using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using System;
using System.Linq;

namespace AutoEmotion.Hooks
{
    public class EmoteReaderHooks : IDisposable
    {
        public delegate void EmoteDelegate(IPlayerCharacter playerCharacter, ushort emoteId);

        public event EmoteDelegate? OnEmote;

        public delegate void OnEmoteFuncDelegate(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2);
        private readonly Hook<OnEmoteFuncDelegate> hookEmote;

        public EmoteReaderHooks()
        {
            hookEmote = Svc.Hook.HookFromSignature<OnEmoteFuncDelegate>("E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24", OnEmoteDetour);
            hookEmote.Enable();
        }

        public void Dispose()
        {
            hookEmote?.Dispose();
        }

        void OnEmoteDetour(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2)
        {
            // unk - some field of event framework singleton? doesn't matter here anyway
            //PluginServices.PluginLog.Info($"Emote >> unk:{unk:X}, instigatorAddr:{instigatorAddr:X}, emoteId:{emoteId}, targetId:{targetId:X}, unk2:{unk2:X}");

            if (Svc.ClientState.LocalPlayer != null)
            {
                if (targetId == Svc.ClientState.LocalPlayer.GameObjectId)
                {
                    var instigatorOb = Svc.Objects.FirstOrDefault(x => (ulong)x.Address == instigatorAddr);
                    if (instigatorOb != null && instigatorOb is IPlayerCharacter playerCharacter)
                    {
                        OnEmote?.Invoke(playerCharacter, emoteId);
                    }
                }
            }
            hookEmote.Original(unk, instigatorAddr, emoteId, targetId, unk2);
        }
    }
}
