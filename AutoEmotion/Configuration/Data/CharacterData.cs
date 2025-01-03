using ECommons.DalamudServices;
using System.Collections.Generic;

namespace AutoEmotion.Configuration.Data
{
    public class CharacterData
    {
        public string name { get; set; } = string.Empty;
        public uint worldID { get; set; } = 0;
        public string worldName { get; set; } = string.Empty;
        public bool isEnabled = true;
        public List<ReactionData> reactionList = [];

        public string GetKey()
        {
            return name + "@" + worldName;
        }
        public void CopyValue(CharacterData value)
        {
            name = value.name;
            worldID = value.worldID;
            worldName = value.worldName;
            isEnabled = value.isEnabled;
            CopyReactionList(value.reactionList);
        }

        public void CopyReactionList(List<ReactionData> list)
        {
            reactionList = new List<ReactionData>(list.Count);
            foreach (var reaction in list)
            {
                ReactionData value = new();
                value.CopyValue(reaction);
                reactionList.Add(value);
            }
        }
    }
}
