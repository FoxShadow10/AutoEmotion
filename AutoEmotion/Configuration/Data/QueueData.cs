using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using System;

namespace AutoEmotion.Configuration.Data
{
    public class QueueData
    {
        public string emote { get; set; } = string.Empty;
        public string expression { get; set; } = string.Empty;
        public int delay { get; set; } = 0;
        public int minDelay { get; } = 525;
        public bool targetBack { get; set; } = false;
        public float rotation { get; set; } = 0f;
        public IPlayerCharacter? playerCharacter { get; set; } = null;
        private static readonly Random Random = new();
        public void Clear()
        { 
            emote = string.Empty; 
            expression = string.Empty; 
            playerCharacter = null;
        }
        public void TargetBack()
        {
            if (targetBack == false) return;
            if (playerCharacter == null) return;
            if (playerCharacter.IsTargetable == true)
            {
                Svc.Targets.Target = playerCharacter;
            }
        }
        public string GetEmote(bool isLogHide)
        {
            if (isLogHide) return emote + " motion";
            return emote;
        }
        public string GetExpression(bool isLogHide)
        {
            if (isLogHide) return expression + " motion";
            return expression;
        }
        public void SetDelay(int Delay)
        {
            delay = Delay >= minDelay ? Delay : minDelay;
            delay += Random.Next(1, 26);
        }
    }
}
