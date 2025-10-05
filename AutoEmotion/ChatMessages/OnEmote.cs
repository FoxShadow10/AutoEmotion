using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using ECommons.DalamudServices;
using AutoEmotion.Configuration.Data;
using System;
using System.Linq;
using AutoEmotion.Utility;
using Lumina.Excel.Sheets;

namespace AutoEmotion
{
    public partial class AutoEmotionPlugin : IDalamudPlugin
    {
        private bool isReactionActive()
        {
            if (!config.isActived) return false;
            if (!config.isReactionActive) return false;
            return true;
        }
        private bool CanExecuteReaction()
        {
            if (!CharacterUtility.IsCharacterAvailable(config)) return false;
            return true;
        }
        private void OnEmote(IPlayerCharacter playerCharacter, ushort emoteId)
        {
            if (!isReactionActive()) return;
            if (!CanExecuteReaction()) return;
            if (!playerCharacter.IsValid()) return;

            var playerName = playerCharacter.Name.TextValue;
            var homeWorldID = playerCharacter.HomeWorld.RowId;
            var pos = playerCharacter.Position;

            Svc.Log.Debug($"OnEmote > Player {playerName} - Emote {emoteId} - X: {pos.X} - Z: {pos.Z}");
#if DEBUG
            if (true)
#else
            if (playerName != ECommons.GameHelpers.Player.Name)
#endif
            {
                var world = Svc.Data.GetExcelSheet<World>()?.GetRow(homeWorldID);
                if (world == null) return;

                var key = playerName + "@" + world.Value.Name.ExtractText();
                if (config.blackList.ContainsKey(key)) return;

                IOrderedEnumerable<ReactionData>? reactions = null;
                config.whiteList.TryGetValue(key, out var characterData);
                if (characterData != null && characterData.isEnabled) //Character in whitelist
                {
                    reactions = characterData.reactionList.Where(r => r.receivedEmoteID == emoteId).OrderBy(r => r.priority);
                }
                if (characterData == null || reactions == null || !reactions.Any()) //Character not in whitelist or not reactions for that emote
                {
                    config.whiteList.TryGetValue(AutoEmotionConfig.EveryoneKey, out characterData);
                    if (characterData != null && characterData.isEnabled)
                    {
                        reactions = characterData.reactionList.Where(r => r.receivedEmoteID == emoteId).OrderBy(r => r.priority);
                    }
                }
                if (reactions == null || !reactions.Any()) return;
                if (reactionCache.CanPerformAction(key, emoteId))
                {
                    foreach (var reaction in reactions)
                    {

                        if (reaction.yalms > 0f)
                        {
                            float distance = CharacterUtility.CalculateDistanceFromCharacter(pos);
                            Svc.Log.Debug($"Distance: {distance}");
                            if (distance > reaction.yalms) continue;
                        }

                        try
                        {
                            var queueData = new QueueData();
                            queueData.emote = reaction.responseEmoteCommand;
                            queueData.expression = reaction.responseExpressionCommand;
                            queueData.SetDelay(reaction.responseDelay);
                            queueData.targetBack = reaction.targetBack;
                            if (!playerCharacter.IsValid()) return;
                            queueData.playerCharacter = playerCharacter;
                            messageQueue.Enqueue(queueData);
                            reactionCache.RecordAction(key, emoteId);
                        }
                        catch (Exception e)
                        {
                            Svc.Log.Error("Error while queuing message: {}", e.Message);
                        }
                    }
                }
            }
        }
    }
}
