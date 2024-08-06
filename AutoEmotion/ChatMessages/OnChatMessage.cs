using AutoEmotion.Configuration;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutoEmotion
{
    public partial class AutoEmotionPlugin : IDalamudPlugin
    {
        List<char> toRemove = new() {
            '','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','','',
        };

        private string AppendBackslashToNonAlphanumeric(string input)
        {
            // Regular expression to match non-alphanumeric characters
            Regex regex = new Regex(@"[^a-zA-Z0-9]");

            // Replace non-alphanumeric characters with themselves followed by a backslash
            string result = regex.Replace(input, match => "\\" + match.Value);

            return result;
        }

        private void ChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (isHandled) return;
            if (!config.isActived) return;
            if (config.triggerLists.Count <= 0)
            {
                Svc.Log.Information("No triggers set; nothing will happen.");
                return;
            };
            if (!config.allowedChannels.Contains(type)) return;

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
                    if (result.Item2.Length > 0)
                    {
                        Svc.Log.Debug($"Queued expression: {result.Item2}");
                        messageQueue.Enqueue(result.Item2);
                    }
                    if (result.Item1.Length > 0)
                    {
                        Svc.Log.Debug($"Queued emote: {result.Item1}");
                        messageQueue.Enqueue(result.Item1);
                    }
                }
                catch (Exception e)
                {
                    Svc.Log.Error("Error while queuing message: {}", e.Message);
                }
            }
        }

        private (string, string) messageParser(SeString messageString)
        {
            var input = messageString.TextValue;
            var stopwatch = Stopwatch.StartNew();
            var solution = (string.Empty, string.Empty);
            Svc.Log.Debug($"Received input: '{input}'");
            var keywords = config.triggerLists.Keys.ToList();
            HashSet<List<TriggerData>> candidates = [];
            var lowercaseInput = input.ToLower();
            var lowercaseSplitInputs = lowercaseInput.Split(' ');
            var splitInputs = input.Split(' ');
            (Int64, string) bestEmote = (long.MaxValue, string.Empty);
            (Int64, string) bestExpression = (long.MaxValue, string.Empty);
            foreach (var keyword in keywords)
            {
                // select all the possible candidates for full sentence
                if (lowercaseInput.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                {
                    candidates.Add(config.triggerLists.GetValueOrDefault(keyword, []));
                }
                // select all the possible candidates for each word
                if (lowercaseSplitInputs.Contains(keyword.ToLower()))
                {
                    candidates.Add(config.triggerLists.GetValueOrDefault(keyword, []));
                }
            }
            Svc.Log.Debug($"Candidates size: {candidates.Count}");
            foreach (var candidate in candidates)
            {
                for (var i = 0; i < candidate.Count; i++)
                {
                    // evaluate for emote
                    if (candidate[i].calculatedPriority < bestEmote.Item1 || candidate[i].calculatedPriority < bestExpression.Item1)
                    {
                        Svc.Log.Debug($"[{i}] Evaluating candidate: '{candidate[i].trigger}'");
                        // evaluate for case sensitive
                        if (candidate[i].isCaseSensitive)
                        {
                            // contains allowed
                            if (candidate[i].isContains)
                            {
                                var matches = input.Contains(candidate[i].trigger);
                                assignResult(matches, candidate[i], ref bestEmote, ref bestExpression);
                            }
                            else // contains not allowed
                            {
                                var matches = input == candidate[i].trigger;
                                assignResult(matches, candidate[i], ref bestEmote, ref bestExpression);
                                foreach (var caseSensitiveSplit in splitInputs)
                                {
                                    matches = caseSensitiveSplit == candidate[i].trigger;
                                    assignResult(matches, candidate[i], ref bestEmote, ref bestExpression);
                                }
                            }
                        }
                        // evaluate for not case sensitive options
                        else
                        {
                            if (candidate[i].isContains)
                            {
                                var matches = lowercaseInput.Contains(candidate[i].trigger, StringComparison.CurrentCultureIgnoreCase);
                                assignResult(matches, candidate[i], ref bestEmote, ref bestExpression);
                            }
                            else
                            {
                                var matches = lowercaseInput.Equals(candidate[i].trigger, StringComparison.CurrentCultureIgnoreCase);
                                assignResult(matches, candidate[i], ref bestEmote, ref bestExpression);
                                foreach (var split in lowercaseSplitInputs)
                                {
                                    matches = split.Equals(candidate[i].trigger, StringComparison.CurrentCultureIgnoreCase);
                                    assignResult(matches, candidate[i], ref bestEmote, ref bestExpression);
                                }
                            }

                        }
                    }
                }
            }
            solution.Item1 = config.isLogHide & !string.IsNullOrEmpty(bestEmote.Item2) ? (bestEmote.Item2 + " motion") : bestEmote.Item2;
            solution.Item2 = config.isLogHide & !string.IsNullOrEmpty(bestExpression.Item2) ? (bestExpression.Item2 + " motion") : bestExpression.Item2;
            stopwatch.Stop();
            Svc.Log.Debug($"Done; elapsed: {stopwatch.ElapsedMilliseconds}ms");
            return solution;
        }

        private static void assignResult(
            bool match,
            TriggerData candidate,
            ref (long, string) emoteTuple,
            ref (long, string) expressionTuple)
        {
            if (!match) return;
            emoteTuple = (candidate.emoteCommand.Length > 0 && candidate.calculatedPriority < emoteTuple.Item1) ?
                                    (candidate.calculatedPriority, candidate.emoteCommand) :
                                    emoteTuple;
            expressionTuple = (candidate.expressionCommand.Length > 0 && candidate.calculatedPriority < expressionTuple.Item1) ?
                (candidate.calculatedPriority, candidate.expressionCommand) :
                expressionTuple;
        }
    }
}
