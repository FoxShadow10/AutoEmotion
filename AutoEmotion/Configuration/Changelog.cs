using System.Collections.Generic;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Common.Math;
using Dalamud.Interface.Utility.Raii;

namespace AutoEmotion.Configuration
{
    internal class Changelog
    {
        public static Dictionary<string, string[]> GetChangelog()
        {
            return new Dictionary<string, string[]>()
                {
                    {
                        "v2.1.2",
                        [
                            "Changed the log level from Information to Debug."
                        ]
                    },
                    {
                        "v2.1.1",
                        [
                            "API update to 13"
                        ]
                    },
                    {
                        "v2.1.0",
                        [
                            "New setting to define the maximum distance (in yalms) for automatic emote responses.",
                            "Added the button Revert Change to restore all previously saved changes."
                        ]
                    },
                    {
                        "v2.0.2",
                        [
                            "Migrated to Dalamud.NET.Sdk"
                        ]
                    },
                    {
                        "v2.0.1",
                        [
                            "API update to 12"
                        ]
                    },
                    {
                        "v2.0.0",
                        [
                            "Added configurable emote reaction",
                            "Plugin can now be toggled off automatically during AFK or Busy",
                            "Option to resume looping emote (ie. /hum, /shakedrink) after reacting with an emote",
                            "Dynamic delay based on message sent for reactions",
                            "Plugin display on HUD (can be toggled off)",
                            "Emote reaction has a whitelist and blacklist",
                            "Added new chat commands and aliases to manage the plugin"
                        ]
                    },
                    {
                        "v1.1.3",
                        [
                            "Update API to 11."
                        ]
                    },
                    {
                        "v1.1.0",
                        [
                            "Added the option to show/ignore the emote log with the emotes executed by AutoEmotion."
                        ]
                    },
                    {
                        "v1.0.0",
                        [
                            "Initial release."
                        ]
                    },
                };
        }

        public static void DrawChangelog()
        {
            var changelog = GetChangelog();

            foreach (var (version, info) in changelog)
            {
                if (ImGui.CollapsingHeader(version, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    using (ImRaii.PushColor(ImGuiCol.Text, new Vector4(0.68f, 0.68f, 0.68f, 1.0f)))
                    {
                        foreach (var text in info)
                        {
                            ImGui.BulletText(text);
                        }
                    }
                    ImGui.Spacing();
                }
            }
        }
    }
}
