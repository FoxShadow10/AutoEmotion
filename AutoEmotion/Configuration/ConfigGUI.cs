using Dalamud.Interface.Utility;
using Dalamud.Plugin;
using Dalamud.Bindings.ImGui;
using Num = System.Numerics;
using FFXIVClientStructs.FFXIV.Common.Math;
using AutoEmotion.Utility;
using System;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using AutoEmotion.Configuration;

namespace AutoEmotion
{
    public partial class AutoEmotionPlugin : IDalamudPlugin
    {
        private unsafe void DrawConfigUI()
        {
            chkSize = 23f * ImGuiHelpers.GlobalScale;
            if (isOpenConfig)
            {
                //ImGui.SetNextWindowSizeConstraints(new Num.Vector2900 * ImGuiHelpers.GlobalScale, 910 * ImGuiHelpers.GlobalScale), new Num.Vector2(900 * ImGuiHelpers.GlobalScale, float.MaxValue));
#if DEBUG
                ImGui.Begin("[DEBUG] AutoEmotion Configuration Window##AutoEmotionConfigWin", ref isOpenConfig, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
#else
                ImGui.Begin("AutoEmotion Configuration Window##AutoEmotionConfigWin", ref isOpenConfig, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
#endif
                if (ImGui.BeginChildFrame(ImGui.GetID("MainChildFrame"), new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - (50 * ImGuiHelpers.GlobalScale)), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground))
                {
                    ImGui.BeginTabBar("##TabBar");

                    if (ImGui.BeginTabItem("Settings##TabSettings"))
                    {
                        ImGui.BeginChild("##TabSettingsChild", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));

                        DrawTextSeparator("GeneralSettings", "General");
                        ImGui.Checkbox("Enable plugin##isActived", ref configGUI.isActived);
                        ImGui.SameLine();
                        ImGuiEx.InfoMarker($"Can be toogled using command:{Environment.NewLine}/aet");

                        ImGui.Checkbox("Enable Chat Reaction##isChatActive", ref configGUI.isChatActive);
                        ImGui.SameLine();
                        ImGuiEx.InfoMarker($"Can be toogled using commands:{Environment.NewLine}/aet c{Environment.NewLine}/aet chat");

                        ImGui.Checkbox("Enable Emote Reaction##isReactionActive", ref configGUI.isReactionActive);
                        ImGui.SameLine();
                        ImGuiEx.InfoMarker($"Can be toogled using commands:{Environment.NewLine}/aet r{Environment.NewLine}/aet reaction");

                        ImGui.Checkbox("Enable while in AFK or Busy state##executeReactionWhileBusyAfk", ref configGUI.executeReactionWhileBusyAfk);

                        ImGui.Checkbox("Hide Emote Log##isLogHide", ref configGUI.isChatLogHidden);
                        ImGui.SameLine();
                        ImGuiEx.InfoMarker("When enabled the emotes used by AutoEmotion will not display the log message.");

                        ImGui.NewLine();
                        DrawTextSeparator("AdvancedSettings", "Advanced");
                        ImGui.Checkbox("Resume Interrupted Loop Emote##resumeEmoteLoop", ref configGUI.resumeEmoteLoop);
                        ImGui.SameLine();
                        ImGuiEx.InfoMarker("Enable this option to automatically resume the emote loop if it gets interrupted by the plugin.");

                        DrawDragInt("Delay before resuming loop emote (ms)##resumeEmoteDelay", ref configGUI.resumeEmoteDelay, 100, 100 * ImGuiHelpers.GlobalScale, 0, int.MaxValue);
                        ImGui.SameLine();
                        ImGuiEx.InfoMarker("Delay before resuming loop emote after the end of the executed emote.");

                        ImGui.Checkbox("Remove target when resuming loop emote##resumeEmoteDetarget", ref configGUI.resumeEmoteDetarget);

                        ImGui.Checkbox("Restore direction when resuming loop emote##resumeEmoteRestoreRotation", ref configGUI.resumeEmoteRestoreRotation);

                        DrawDragInt("Emote/Player threshold before lock##maxReactionsCache", ref configGUI.maxReactionsCache, 1, 100 * ImGuiHelpers.GlobalScale, 1, 100);
                        ImGui.SameLine();
                        ImGuiEx.InfoMarker($"When receiving multiple triggers of the same emote from the same target, only react to the specified amount.{Environment.NewLine}This stop the plugin from queuing too many reactions to the same event and prevents emote spamming from triggers.");

                        DrawDragInt("Time before lock expires (s)##timeoutReactionsCache", ref configGUI.timeoutReactionsCache, 1, 100 * ImGuiHelpers.GlobalScale, 1, int.MaxValue);
#if DEBUG
                        ImGui.Text($"Cache {reactionCache.GetTotalCacheKeys()}");
#endif

                        DrawDragInt("Time for each letter before triggering emote (ms)##charDelay", ref configGUI.charDelay, 5, 100 * ImGuiHelpers.GlobalScale, 0, int.MaxValue);
                        ImGui.SameLine();
                        ImGuiEx.InfoMarker($"This value represents the delay in milliseconds for each character within the message.{Environment.NewLine}The total delay for the emote will be calculated based on the position of the keyword within the message.{Environment.NewLine}The delay increases proportionally to the distance of the keyword from the start of the message.{Environment.NewLine}Minimum total delay is 500ms.");


                        ImGui.NewLine();
                        DrawTextSeparator("OtherUISettings", "Other/UI");
                        ImGui.Checkbox("Use Plus/Minus buttons for numeric inputs.##showPlusMinus", ref configGUI.showPlusMinus);

                        ImGui.Checkbox("Show Plugin Status Window##isActiveWinShow", ref configGUI.isStatusWinOpen);
                        ImGui.SameLine();
                        ImGuiEx.InfoMarker("Enable this option to display a status window that shows whether the plugin is active and which functions are currently enabled.");

                        ImGui.Checkbox("Lock Plugin Status Window##isActiveWinLock", ref configGUI.isStatusWinLocked);

                        ImGui.EndChild();

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Chat Reaction##TabKeywords"))
                    {
                        ImGui.BeginChild("##TabKeywordsChild", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));

                        ImGui.Text("Enabled channels:");
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Select chat channels to enable emote execution control.");
                        }

                        DrawChannelList();

                        ImGui.NewLine();
                        ImGui.Separator();

                        ImGui.Text("Keywords list:");

                        var text = "Clear Keywords list";
                        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGuiHelpers.GetButtonSize(text).X);
                        if (ImGui.Button(text))
                        {
                            if (ImGui.GetIO().KeyShift)
                            {
                                count--;
                                if (count == 0)
                                {
                                    configGUI.triggerLists.Clear();
                                    configGUI.triggerOrder.Clear();
                                    count = 3;
                                }
                            }
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip($"Hold SHIFT and CLICK {count} times to clear the whole list.");
                        }
                        else
                        {
                            count = 3;
                        }

                        DrawTriggerList();

                        ImGui.EndChild();

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Emote Reaction##TabEmoteReaction"))
                    {
                        ImGui.BeginChild("##BWLists", new Vector2(200, ImGui.GetContentRegionAvail().Y));
                        ImGui.BeginChild("##BoxWhitelist", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y / 2), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                        DrawWhiteList();
                        ImGui.EndChild();

                        ImGui.BeginChild("##BoxBlacklist", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                        DrawBlackList();
                        ImGui.EndChild();
                        ImGui.EndChild();

                        ImGui.SameLine();
                        DrawReactionList();

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Changelog##Changelog"))
                    {
                        ImGui.BeginChild("##ChangelogChild", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));
                        Changelog.DrawChangelog();
                        ImGui.EndChild();

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                    ImGui.EndChildFrame();
                }

                ImGui.GetWindowDrawList().AddLine(ImGui.GetCursorScreenPos(), ImGui.GetCursorScreenPos() + ImGui.GetContentRegionAvail() * Num.Vector2.UnitX, ImGui.GetColorU32(ImGuiCol.TabActive));
                ImGui.NewLine();

                if (ImGui.Button("Save"))
                {
                    SaveConfig();
                }
                ImGui.SameLine();
                if (ImGui.Button("Save and Close"))
                {
                    SaveConfig();
                    isOpenConfig = false;
                }
                ImGui.SameLine();
                if (ImGui.Button("Revert Change"))
                {
                    LoadConfig();
                    InitializeGui();
                }
                ImGui.SameLine();
                KoFiButton.DrawRight();

                ImGui.End();
            }
        }

        private void SaveConfig()
        {
            config = configGUI;
            config.Save();
            InitializeConfig();
            if (Svc.ClientState.IsLoggedIn)
            {
                isOpenStatusWin = config.isStatusWinOpen;
            }
            else
            {
                isOpenStatusWin = false;
            }
        }
    }
}
