using System;

namespace AutoEmotion.Configuration.Data
{
    public class ReactionData
    {
        public bool isLocked = false;
        public uint receivedEmoteID = 0;
        public uint responseEmoteID = 0;
        public string responseEmoteCommand = string.Empty;
        public uint responseExpressionID = 0;
        public string responseExpressionCommand = string.Empty;
        public int responseDelay = 0;
        public bool targetBack = false;
        public int priority = 0;
        public long calculatedPriority = 0;
        public float yalms = 0f;
        public void CopyValue(ReactionData value)
        {
            isLocked = value.isLocked;
            receivedEmoteID = value.receivedEmoteID;
            responseEmoteID = value.responseEmoteID;
            responseEmoteCommand = value.responseEmoteCommand;
            responseExpressionID = value.responseExpressionID;
            responseExpressionCommand = value.responseExpressionCommand;
            responseDelay = value.responseDelay;
            targetBack = value.targetBack;
            priority = value.priority;
            calculatedPriority = value.calculatedPriority;
            yalms = value.yalms;
        }
    }
}
