using System;

namespace AutoEmotion.Configuration.Data
{
    public class TriggerData
    {
        public bool isLocked;
        public string trigger = string.Empty;
        public uint emoteID = 0;
        public string emoteCommand = string.Empty;
        public uint expressionID = 0;
        public string expressionCommand = string.Empty;
        public bool isContains;
        public bool isCaseSensitive;
        public int priority = 0;
        public long calculatedPriority = 0;
        public void CopyValue(TriggerData value)
        {
            isLocked = value.isLocked;
            trigger = value.trigger;
            emoteID = value.emoteID;
            emoteCommand = value.emoteCommand;
            expressionID = value.expressionID;
            expressionCommand = value.expressionCommand;
            isContains = value.isContains;
            isCaseSensitive = value.isCaseSensitive;
            priority = value.priority;
            calculatedPriority = value.calculatedPriority;
        }
    }
}
