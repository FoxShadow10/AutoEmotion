using System;

namespace AutoEmotion.Configuration
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
        public Int32 priority = 0;
        public Int64 calculatedPriority = 0;
        public void CopyValue(TriggerData value)
        {
            this.isLocked = value.isLocked;
            this.trigger = value.trigger;
            this.emoteID = value.emoteID;
            this.emoteCommand = value.emoteCommand;
            this.expressionID = value.expressionID;
            this.expressionCommand = value.expressionCommand;
            this.isContains = value.isContains;
            this.isCaseSensitive = value.isCaseSensitive;
            this.priority = value.priority;
            this.calculatedPriority = value.calculatedPriority;
        }
    }
}
