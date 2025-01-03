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
        private bool CanExecute()
        {
            if (!config.isActived) return false;
            if (!config.isReactionActive) return false;
            if (!CharacterUtility.IsCharacterAvailable(config)) return false;
            return true;
        }
        private void OnEmote(IPlayerCharacter playerCharacter, ushort emoteId)
        {
            if (!CanExecute()) return;
            var playerName = playerCharacter.Name.TextValue;
            Svc.Log.Debug($"OnEmote > Player {playerName} - Emote {emoteId}");
#if DEBUG
            if (true)
#else
            if (playerName != ECommons.GameHelpers.Player.Name)
#endif
            {
                var world = Svc.Data.GetExcelSheet<World>()?.GetRow(playerCharacter.HomeWorld.RowId);
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
                if (reactionCache.RecordAction(key, emoteId))
                {
                    foreach (var reaction in reactions)
                    {
                        try
                        {
                            var queueData = new QueueData();
                            queueData.emote = reaction.responseEmoteCommand;
                            queueData.expression = reaction.responseExpressionCommand;
                            queueData.SetDelay(reaction.responseDelay);
                            queueData.targetBack = reaction.targetBack;
                            queueData.playerCharacter = playerCharacter;
                            messageQueue.Enqueue(queueData);
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
