using AutoEmotion.Configuration;
using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace AutoEmotion;

[Serializable]
public class AutoEmotionConfig : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool isActived = true;
    public bool isLogHide = false;
    public Dictionary<string, List<TriggerData>> triggerLists = new Dictionary<string, List<TriggerData>>(StringComparer.OrdinalIgnoreCase) { };
    public List<string> triggerOrder = new List<string> { };
    public List<XivChatType> allowedChannels { get; set; } = new() { XivChatType.Say };

    public readonly bool[] visibleChannels =
        {
            false,//None
            false,//None
            false,//None
            false,//None
            true,//Say
            true,//Shout
            true,//TellOutgoing
            false,//TellIncoming
            true,//Party
            true,//Alliance
            true,//Ls1
            true,//Ls2
            true,//Ls3
            true,//Ls4
            true,//Ls5
            true,//Ls6
            true,//Ls7
            true,//Ls8
            true,//FreeCompany
            true,//NoviceNetwork
            true,//CustomEmote
            false,//StandardEmote
            true,//Yell
            true,//CrossParty
            false,//PvPTeam
            true,//CrossLinkShell1
            true,//Echo
            false,//None
            false,//None
            false,//None
            false,//None
            false,//None
            false,//None
            false,//None
            true,//CrossLinkShell2
            true,//CrossLinkShell3
            true,//CrossLinkShell4
            true,//CrossLinkShell5
            true,//CrossLinkShell6
            true,//CrossLinkShell7
            true//CrossLinkShell8
        };

    [NonSerialized]
    private IDalamudPluginInterface? _pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
    }

    public void Save()
    {
        CalculatePriorities();
        _pluginInterface!.SavePluginConfig(this);
    }

    private void CalculatePriorities()
    {
        foreach (var list in triggerLists)
        {
            foreach (var trigger in list.Value)
            {
                trigger.calculatedPriority = (trigger.priority * triggerOrder.Count) + triggerOrder.IndexOf(trigger.trigger);
            }
        }
    }
}
