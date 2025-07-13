using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;

namespace AutoEmotion.Utility
{
    public class CharacterUtility
    {
        public enum OnlineStatus
        {
            Busy = 12,
            AwayFromKeyboard = 17,
        }

        public static bool IsCharacterAvailable(AutoEmotionConfig config)
        {
            //if (GenericHelpers.IsOccupied()) return false;
            if (Svc.Condition[ConditionFlag.InCombat]
               || Svc.Condition[ConditionFlag.Occupied]
               || Svc.Condition[ConditionFlag.Occupied30]
               || Svc.Condition[ConditionFlag.Occupied33]
               || Svc.Condition[ConditionFlag.Occupied38]
               || Svc.Condition[ConditionFlag.Occupied39]
               || Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
               || Svc.Condition[ConditionFlag.OccupiedInEvent]
               || Svc.Condition[ConditionFlag.OccupiedInQuestEvent]
               || Svc.Condition[ConditionFlag.OccupiedSummoningBell]
               || Svc.Condition[ConditionFlag.WatchingCutscene]
               || Svc.Condition[ConditionFlag.WatchingCutscene78]
               || Svc.Condition[ConditionFlag.BetweenAreas]
               || Svc.Condition[ConditionFlag.BetweenAreas51]
               //|| Svc.Condition[ConditionFlag.InThatPosition]
               || Svc.Condition[ConditionFlag.TradeOpen]
               || Svc.Condition[ConditionFlag.Crafting]
               || Svc.Condition[ConditionFlag.ExecutingCraftingAction]
               || Svc.Condition[ConditionFlag.ExecutingGatheringAction]
               || Svc.Condition[ConditionFlag.PreparingToCraft]
               || Svc.Condition[ConditionFlag.Unconscious]
               || Svc.Condition[ConditionFlag.MeldingMateria]
               || Svc.Condition[ConditionFlag.Gathering]
               || Svc.Condition[ConditionFlag.OperatingSiegeMachine]
               || Svc.Condition[ConditionFlag.CarryingItem]
               || Svc.Condition[ConditionFlag.CarryingObject]
               || Svc.Condition[ConditionFlag.BeingMoved]
               || Svc.Condition[ConditionFlag.Mounted2]
               || Svc.Condition[ConditionFlag.Mounting]
               || Svc.Condition[ConditionFlag.Mounting71]
               || Svc.Condition[ConditionFlag.ParticipatingInCustomMatch]
               || Svc.Condition[ConditionFlag.PlayingLordOfVerminion]
               || Svc.Condition[ConditionFlag.ChocoboRacing]
               || Svc.Condition[ConditionFlag.PlayingMiniGame]
               || Svc.Condition[ConditionFlag.Performing]
               || Svc.Condition[ConditionFlag.Fishing]
               || Svc.Condition[ConditionFlag.Transformed]
               || Svc.Condition[ConditionFlag.UsingHousingFunctions]
               || Svc.ClientState.LocalPlayer?.IsTargetable != true
                ) return false;
            if (Svc.ClientState.IsGPosing) return false;
            if (Svc.ClientState.IsPvP) return false;
            if (!config.executeReactionWhileBusyAfk)
            {
                if (Svc.ClientState.LocalPlayer != null && Svc.ClientState.LocalPlayer.OnlineStatus.IsValid)
                {
                    var playerStatus = (OnlineStatus)Svc.ClientState.LocalPlayer.OnlineStatus.RowId;
                    if (playerStatus == OnlineStatus.Busy || playerStatus == OnlineStatus.AwayFromKeyboard) return false;
                }
            }
            return true;
        }

        public static unsafe EmoteIdentifier? GetCurrentEmote(bool loopEmote = false)
        {
            if (Svc.ClientState.LocalPlayer == null) return null;
            var playerAddress = (GameObject*)Svc.ClientState.LocalPlayer.Address;
            var player = (Character*)playerAddress;
            if (player != null)
            {
                return EmoteIdentifier.Get(player, loopEmote);
            }
            return null;
        }

        public static unsafe void SetRotation(float p)
        {
            if (Svc.ClientState.LocalPlayer != null && Svc.ClientState.LocalPlayer.IsValid())
            {
                var playerAddress = (GameObject*)Svc.ClientState.LocalPlayer.Address;
                playerAddress->SetRotation(p);
            }
        }

        public static float CalculateDistanceFromCharacter(System.Numerics.Vector3 targetPos)
        {
            if (Svc.ClientState.LocalPlayer != null && Svc.ClientState.LocalPlayer.IsValid())
            {
                var charPos = Svc.ClientState.LocalPlayer.Position;
                float distance = (float)Math.Sqrt(Math.Pow((targetPos.X - charPos.X), 2) + Math.Pow((targetPos.Z - charPos.Z), 2));
                return (float)Math.Round(distance, 3);
            }
            else
            {
                return 0;
            }             
        }
    }
}
