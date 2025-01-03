using AutoEmotion.Configuration.Data;
using AutoEmotion.Utility;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using Dalamud.Utility;
using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AutoEmotion
{
    public partial class AutoEmotionPlugin : IDalamudPlugin
    {
        List<char> toRemove = new() {
            '','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','',
        };

        private enum QueueType { Emote, Expression, Both }

        private string AppendBackslashToNonAlphanumeric(string input)
        {
            // Regular expression to match non-alphanumeric characters
            Regex regex = new Regex(@"[^a-zA-Z0-9]");

            // Replace non-alphanumeric characters with themselves followed by a backslash
            string result = regex.Replace(input, match => "\\" + match.Value);

            return result;
        }
        private bool CanExecute(XivChatType type)
        {
            if (!config.isActived) return false;
            if (!config.isChatActive) return false;
            if (config.triggerLists.Count <= 0)
            {
                Svc.Log.Information("No triggers set; nothing will happen.");
                return false;
            };
            if (!config.allowedChannels.Contains(type)) return false;
            if (!CharacterUtility.IsCharacterAvailable(config)) return false;
            return true;
        }
        private void ChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (isHandled) return;
            if (!CanExecute(type)) return;

            var senderSanitized = sender.TextValue;

            if ((type == XivChatType.Party) || (type == XivChatType.Alliance))
            {
                foreach (var c in toRemove) { senderSanitized = senderSanitized.Replace(c.ToString(), string.Empty); }
            }

            if ((senderSanitized == ECommons.GameHelpers.Player.Name) || (type == XivChatType.TellOutgoing))
            {
                var result = messageParser(message);
                try
                {
                    if (result.emote.command.IsNullOrEmpty() && result.expression.command.IsNullOrEmpty()) return;
                    if (result.emote.delay == result.expression.delay)
                    {
                        Enqueue(QueueType.Both, result);
                    }
                    else if (result.expression.delay > result.emote.delay)
                    {
                        if (!result.emote.command.IsNullOrEmpty())
                        {
                            Enqueue(QueueType.Emote, result);
                        }
                        if (!result.expression.command.IsNullOrEmpty())
                        {
                            Enqueue(QueueType.Expression, result);
                        }
                    }
                    else
                    {
                        if (!result.expression.command.IsNullOrEmpty())
                        {
                            Enqueue(QueueType.Expression, result);
                        }
                        if (!result.emote.command.IsNullOrEmpty())
                        {
                            Enqueue(QueueType.Emote, result);
                        }
                    }
                }
                catch (Exception e)
                {
                    Svc.Log.Error("Error while queuing message: {}", e.Message);
                }
            }
        }
        private void Enqueue(QueueType queueType, ((string command, int delay) emote, (string command, int delay) expression) tuple)
        {
            QueueData queueData = new QueueData();
            switch (queueType)
            {
                case QueueType.Emote:
                    {
                        queueData.emote = tuple.emote.command;
                        queueData.SetDelay(tuple.emote.delay);
                        messageQueue.Enqueue(queueData);
                        Svc.Log.Debug($"Enqueue Emote {queueData.emote} with delay {queueData.delay}ms");
                        break;
                    }
                case QueueType.Expression:
                    {
                        queueData.expression = tuple.expression.command;
                        queueData.SetDelay(tuple.expression.delay);
                        messageQueue.Enqueue(queueData);
                        Svc.Log.Debug($"Enqueue expression {queueData.expression} with delay {queueData.delay}ms");
                        break;
                    }
                default:
                    {
                        queueData.emote = tuple.emote.command;
                        queueData.expression = tuple.expression.command;
                        queueData.SetDelay(tuple.emote.delay);
                        messageQueue.Enqueue(queueData);
                        Svc.Log.Debug($"Enqueue Emote {queueData.emote} and Expression {queueData.expression} with delay {queueData.delay}ms");
                        break;
                    }
            }
        }

        public ((string command, int delay) emote, (string command, int delay) expression) messageParser(SeString messageString)
        {
            var stopwatch = Stopwatch.StartNew();
            var input = messageString.TextValue;
            var lowerInput = input.ToLower();
            string[] words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var solution = ((string.Empty, 0), (string.Empty, 0));
            HashSet<List<TriggerData>> candidates = [];
            TriggerData bestEmote = new TriggerData() { calculatedPriority = long.MaxValue };
            TriggerData bestExpression = new TriggerData() { calculatedPriority = long.MaxValue };
            Svc.Log.Debug($"Received input: '{input}'");

            foreach (var keyword in config.triggerLists)
            {
                if (CompareStrings(lowerInput, keyword.Key.ToLower(), true, false))
                {
                    candidates.Add(config.triggerLists.GetValueOrDefault(keyword.Key, []));
                }
            }
            Svc.Log.Debug($"Candidates size: {candidates.Count}");
            foreach (var candidate in candidates)
            {
                foreach (var data in candidate)
                {
                    if (data.calculatedPriority > bestEmote.calculatedPriority || data.calculatedPriority > bestExpression.calculatedPriority) { continue; }
                    Svc.Log.Debug($"Evaluating candidate: '{data.trigger}'");

                    bool isMatch = false;

                    if (data.isContains || data.trigger.Contains(" "))
                    {
                        // Check if the search text is contained anywhere in the text
                        isMatch = CompareStrings(input, data.trigger, data.isCaseSensitive, false);
                    }
                    else
                    {
                        foreach (string word in words)
                        {
                            if (CompareStrings(word, data.trigger, data.isCaseSensitive))
                            {
                                isMatch = true;
                                break;
                            }
                        }
                    }

                    if (isMatch)
                    {
                        if (!string.IsNullOrEmpty(data.emoteCommand) && data.calculatedPriority < bestEmote.calculatedPriority) bestEmote = data;
                        if (!string.IsNullOrEmpty(data.expressionCommand) && data.calculatedPriority < bestExpression.calculatedPriority) bestExpression = data;
                    }
                }
            }
            if (!bestEmote.trigger.IsNullOrEmpty())
            {
                solution.Item1.Item1 = bestEmote.emoteCommand;
                solution.Item1.Item2 = calculateDelay(lowerInput, bestEmote.trigger.ToLower(), config.charDelay);
            }
            if (!bestExpression.trigger.IsNullOrEmpty())
            {
                solution.Item2.Item1 = bestExpression.expressionCommand;
                solution.Item2.Item2 = calculateDelay(lowerInput, bestExpression.trigger.ToLower(), config.charDelay);
            }
            stopwatch.Stop();
            Svc.Log.Debug($"Done; elapsed: {stopwatch.ElapsedMilliseconds}ms");
            return solution;
        }

        private bool CompareStrings(string text, string searchText, bool caseSensitive, bool exactMatch = true)
        {
            if (!caseSensitive)
            {
                text = text.ToLower();
                searchText = searchText.ToLower();
            }

            return exactMatch
                ? text == searchText
                : text.Contains(searchText);
        }

        private static int calculateDelay(string mainstring, string substring, int charDelay)
        {
            if (charDelay > 0)
            {
                return (mainstring.IndexOf(substring) + 1) * charDelay;
            }
            return 0;
        }
    }
}
