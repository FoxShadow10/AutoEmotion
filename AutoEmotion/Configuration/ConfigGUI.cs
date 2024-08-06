using AutoEmotion.Configuration;
using Dalamud.Game.Text;
using Dalamud.Interface.Utility;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System;
using Num = System.Numerics;
using Dalamud.Interface.Utility.Raii;
using ECommons.ImGuiMethods;
using ECommons.DalamudServices;
using AutoEmotion.Utility;


namespace AutoEmotion
{
    public partial class AutoEmotionPlugin : IDalamudPlugin
    {
        private TriggerData newTriggerData = new TriggerData();

        private float checkboxSize = 23;

        private string searchInput = string.Empty;
        private string oldTrigger = string.Empty;
        private string newTrigger = string.Empty;

        private int beginDrag = -1;
        private int endDrag = -1;
        private Num.Vector2 endDragPosition = new();

        private int count = 3;

        private enum TableHeaderAlign { Left, Center, Right }
        private void TableHeaderRow(params TableHeaderAlign[] aligns)
        {
            ImGui.TableNextRow();
            for (var i = 0; i < ImGui.TableGetColumnCount(); i++)
            {
                ImGui.TableNextColumn();
                var label = ImGui.TableGetColumnName(i);
                ImGui.PushID($"TableHeader_{i}");
                var align = aligns.Length <= i ? TableHeaderAlign.Left : aligns[i];

                switch (align)
                {
                    case TableHeaderAlign.Center:
                        {
                            var textSize = ImGui.CalcTextSize(label);
                            var space = ImGui.GetContentRegionAvail().X;
                            ImGui.TableHeader("");
                            ImGui.SameLine(space / 2f - textSize.X / 2f);
                            ImGui.Text(label);

                            break;
                        }
                    case TableHeaderAlign.Right:
                        {
                            ImGui.TableHeader("");
                            var textSize = ImGui.CalcTextSize(label);
                            var space = ImGui.GetContentRegionAvail().X;
                            ImGui.SameLine(space - textSize.X);
                            ImGui.Text(label);
                            break;
                        }
                    default:
                        ImGui.TableHeader(label);
                        break;
                }
                ImGui.PopID();
            }
        }

        public static bool IconTextFrame(uint previewIcon, string previewText, bool hoverColor = false)
        {
            var pos = ImGui.GetCursorScreenPos();
            var size = new Num.Vector2(ImGui.GetTextLineHeight()) + ImGui.GetStyle().FramePadding * 2;
            var frameSize = new Num.Vector2(ImGui.CalcItemWidth(), ImGui.GetFrameHeight());

            using (ImRaii.PushColor(ImGuiCol.FrameBg, ImGui.GetColorU32(ImGuiCol.FrameBgHovered), hoverColor && ImGui.IsMouseHoveringRect(pos, pos + frameSize)))
            {
                if (ImGui.BeginChildFrame(ImGui.GetID($"iconTextFrame_{previewIcon}_{previewText}"), frameSize))
                {
                    var drawlist = ImGui.GetWindowDrawList();
                    var icon = Svc.Texture.GetFromGameIcon(previewIcon).GetWrapOrEmpty();
                    if (icon != null) drawlist.AddImage(icon.ImGuiHandle, pos, pos + new Num.Vector2(size.Y));
                    var textSize = ImGui.CalcTextSize(previewText);
                    drawlist.AddText(pos + new Num.Vector2(size.Y + ImGui.GetStyle().FramePadding.X, size.Y / 2f - textSize.Y / 2f), ImGui.GetColorU32(ImGuiCol.Text), previewText);
                }
                ImGui.EndChildFrame();
            }
            return ImGui.IsItemClicked();
        }

        private void DrawCombo(string ID, IReadOnlyList<EmoteIdentifier> listEmote, ref uint outEmoteID, ref string outEmoteCommand)
        {
            if (ImGui.BeginCombo($"##{ID}", EmoteIdentifier.FetchName(outEmoteID), ImGuiComboFlags.HeightLargest))
            {
                if (ImGui.IsWindowAppearing())
                {
                    searchInput = string.Empty;
                    ImGui.SetKeyboardFocusHere();
                }

                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputTextWithHint($"##{ID}SearchInput", "Search...", ref searchInput, 40);
                if (ImGui.BeginChild($"##{ID}SearchScroll", new Num.Vector2(ImGui.GetContentRegionAvail().X, 300)))
                {
                    using (ImRaii.PushColor(ImGuiCol.FrameBgHovered, ImGui.GetColorU32(ImGuiCol.ButtonHovered)))
                    {
                        foreach (var emote in listEmote)
                        {
                            if (!string.IsNullOrWhiteSpace(searchInput))
                            {
                                if (!(emote.Name.Contains(searchInput, StringComparison.InvariantCultureIgnoreCase) || (ushort.TryParse(searchInput, out var searchShort) && searchShort == emote.EmoteID))) continue;
                            }

                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (IconTextFrame(emote.Icon, emote.Name, true))
                            {
                                outEmoteID = emote.EmoteID;
                                outEmoteCommand = emote.EmoteCommand;
                                ImGui.CloseCurrentPopup();
                            }
                        }
                    }
                }
                ImGui.EndChild();
                ImGui.EndCombo();
            }
        }

        private bool MouseWithin(Num.Vector2 min, Num.Vector2 max)
        {
            var mousePos = ImGui.GetMousePos();
            return mousePos.X >= min.X && mousePos.Y <= max.X && mousePos.Y >= min.Y && mousePos.Y <= max.Y;
        }

        private void DrawConfigUI()
        {
            if (isOpenConfig)
            {
                ImGui.SetNextWindowSizeConstraints(new Num.Vector2(900 * ImGuiHelpers.GlobalScale, 910 * ImGuiHelpers.GlobalScale), new Num.Vector2(900 * ImGuiHelpers.GlobalScale, float.MaxValue));
                ImGui.Begin("AutoEmotion Configuration Window##AutoEmotionConfigWin", ref isOpenConfig, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
                var windowMax = ImGui.GetWindowPos() + ImGui.GetWindowSize();
                var i = 0;
                var chkSize = checkboxSize * ImGuiHelpers.GlobalScale;

                ImGui.Checkbox("Enable Plugin##isActived", ref configGUI.isActived);
                ImGui.SameLine();
                ImGui.Checkbox("Hide Emote Log##isLogHide", ref configGUI.isLogHide);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("When enabled the emotes used by AutoEmotion will not display the log message.");
                }
                ImGui.Separator();

                //****************************** LIST OF CHANNEL
                ImGui.Text("Enabled channels:");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Select chat channels to enable emote execution control.");
                }

                ImGui.Columns(2);
                foreach (var e in (XivChatType[])Enum.GetValues(typeof(XivChatType)))
                {
                    if (configGUI.visibleChannels[i])
                    {
                        var enabled = configGUI.allowedChannels.Contains(e);
                        if (ImGui.Checkbox($"{e}", ref enabled))
                        {
                            if (enabled) configGUI.allowedChannels.Add(e);
                            else configGUI.allowedChannels.Remove(e);
                        }
                        ImGui.NextColumn();
                    }
                    i++;
                }
                ImGui.Columns(1);
                //****************************** END LIST OF CHANNEL

                ImGui.NewLine();
                ImGui.Separator();

                //****************************** TRIGGERS LIST
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

                if (ImGui.BeginChildFrame(1, new Num.Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - (50 * ImGuiHelpers.GlobalScale)), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground))
                {
                    if (ImGui.BeginTable("HeaderTriggerTable", 7))
                    {
                        // TABLE HEADER
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, chkSize * 3 + 3 * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("Keyword", ImGuiTableColumnFlags.WidthFixed, 150 * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("Emote", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150) * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("Expression", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150) * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("Contained", ImGuiTableColumnFlags.WidthFixed, 80 * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("Case Sensitive", ImGuiTableColumnFlags.WidthFixed, 80 * ImGuiHelpers.GlobalScale);
                        ImGui.TableSetupColumn("Priority", ImGuiTableColumnFlags.WidthFixed, ImGui.GetContentRegionAvail().X);
                        TableHeaderRow(TableHeaderAlign.Left, TableHeaderAlign.Left, TableHeaderAlign.Left, TableHeaderAlign.Left, TableHeaderAlign.Center, TableHeaderAlign.Center, TableHeaderAlign.Center);
                        ImGuiEx.InfoMarker("The keyword with the lowest priority will take precedence if multiple applicable triggers are found in a chat message.");
                        // END TABLE HEADER

                        // NEW LINE ENTRY
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.PushID("##newTrigger");
                        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Num.Vector2.One * ImGuiHelpers.GlobalScale);
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.SetCursorPosX(chkSize * 2 + 7);
                        if (ImGui.Button($"{(char)FontAwesomeIcon.Plus}##add", new Num.Vector2(chkSize)))
                        {
                            AddNewTrigger();
                        }
                        ImGui.PopFont();
                        ImGui.PopStyleVar();

                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        ImGui.InputText("##newTrigger", ref newTriggerData.trigger, 100);

                        ImGui.TableNextColumn();
                        if (ThreadLoadImageHandler.TryGetIconTextureWrap(EmoteIdentifier.FetchIcon(newTriggerData.emoteID), false, out var newEmoteIcon))
                        {
                            ImGui.Image(newEmoteIcon.ImGuiHandle, new(chkSize));
                        }
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        DrawCombo("newEmote", EmoteIdentifier.ListEmote, ref newTriggerData.emoteID, ref newTriggerData.emoteCommand);

                        ImGui.TableNextColumn();
                        if (ThreadLoadImageHandler.TryGetIconTextureWrap(EmoteIdentifier.FetchIcon(newTriggerData.expressionID), false, out var newExpressionIcon))
                        {
                            ImGui.Image(newExpressionIcon.ImGuiHandle, new(chkSize));
                        }
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        DrawCombo("newExpression", EmoteIdentifier.ListExpression, ref newTriggerData.expressionID, ref newTriggerData.expressionCommand);

                        ImGui.TableNextColumn();
                        ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2f - chkSize / 2f);
                        ImGui.Checkbox("##newIsContains", ref newTriggerData.isContains);

                        ImGui.TableNextColumn();
                        ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2f - chkSize / 2f);
                        ImGui.Checkbox("##newIsCaseSensitive", ref newTriggerData.isCaseSensitive);

                        ImGui.TableNextColumn();
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        ImGui.InputInt("##newPriority", ref newTriggerData.priority, 10);
                        // END NEW LINE ENTRY

                        ImGui.EndTable();
                    }
                    if (ImGui.BeginTable("RowsTriggerTable", 7, ImGuiTableFlags.ScrollY))
                    {
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, chkSize * 3 + 3 * ImGuiHelpers.GlobalScale); //Buttons
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 150 * ImGuiHelpers.GlobalScale); //Keyword
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150) * ImGuiHelpers.GlobalScale); //Emote
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150) * ImGuiHelpers.GlobalScale); //Expression
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 80 * ImGuiHelpers.GlobalScale); //Contained
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 80 * ImGuiHelpers.GlobalScale); //Case Sensitive
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, ImGui.GetContentRegionAvail().X); //Priority

                        string deleteTrigger = string.Empty;
                        bool isModified = false;
                        i = 0;
                        foreach (var key in configGUI.triggerOrder)
                        {
                            if (configGUI.triggerLists.TryGetValue(key, out var triggerList))
                            {
                                var triggerData = triggerList.First(t => t.trigger == key);
                                var trigger = key;
                                if (triggerData != null)
                                {
                                    ImGui.PushID($"trigger_{key}");

                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();

                                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Num.Vector2.One * ImGuiHelpers.GlobalScale);
                                    ImGui.PushFont(UiBuilder.IconFont);

                                    ImGui.Button($"{(char)FontAwesomeIcon.ArrowsUpDown}", new Num.Vector2(chkSize));
                                    if (beginDrag == -1 && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                                    {
                                        beginDrag = i;
                                        endDrag = i;
                                        endDragPosition = ImGui.GetItemRectMin();
                                        //Svc.Log.Debug($"Begin {beginDrag} {endDragPosition}");
                                    }

                                    if (beginDrag >= 0 && MouseWithin(ImGui.GetItemRectMin(), new Num.Vector2(windowMax.X, ImGui.GetItemRectMax().Y)))
                                    {
                                        endDrag = i;
                                        endDragPosition = ImGui.GetItemRectMin();
                                    }

                                    ImGui.SameLine();
                                    if (ImGui.Button($"{(char)(triggerData.isLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen)}", new Num.Vector2(chkSize)))
                                    {
                                        if (triggerData.isLocked == false || ImGui.GetIO().KeyShift)
                                            triggerData.isLocked = !triggerData.isLocked;
                                    }

                                    if (ImGui.IsItemHovered())
                                    {
                                        ImGui.PopFont();
                                        if (triggerData.isLocked && ImGui.GetIO().KeyShift)
                                        {
                                            ImGui.SetTooltip("Unlock Entry.");
                                        }
                                        else if (triggerData.isLocked)
                                        {
                                            ImGui.SetTooltip("Hold SHIFT to unlock.");
                                        }
                                        else
                                        {
                                            ImGui.SetTooltip("Lock Entry.");
                                        }

                                        ImGui.PushFont(UiBuilder.IconFont);
                                    }

                                    if (triggerData.isLocked) ImGui.BeginDisabled(triggerData.isLocked);

                                    ImGui.SameLine();
                                    if (ImGui.Button($"{(char)FontAwesomeIcon.Trash}##delete", new Num.Vector2(chkSize)) && ImGui.GetIO().KeyShift)
                                    {
                                        deleteTrigger = triggerData.trigger;
                                    }

                                    if (ImGui.IsItemHovered() && !ImGui.GetIO().KeyShift)
                                    {
                                        ImGui.PopFont();
                                        ImGui.SetTooltip("Hold SHIFT to delete.");
                                        ImGui.PushFont(UiBuilder.IconFont);
                                    }
                                    ImGui.PopFont();
                                    ImGui.PopStyleVar();

                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                                    if (ImGui.InputText("##trigger", ref trigger, 100))
                                    {
                                        newTrigger = trigger;
                                        if (string.IsNullOrEmpty(oldTrigger))
                                        {
                                            oldTrigger = triggerData.trigger;
                                        }
                                    }
                                    if (ImGui.IsItemDeactivatedAfterEdit())
                                    {
                                        isModified = true;
                                    }

                                    ImGui.TableNextColumn();
                                    if (ThreadLoadImageHandler.TryGetIconTextureWrap(EmoteIdentifier.FetchIcon(triggerData.emoteID), false, out var emoteIcon))
                                    {
                                        ImGui.Image(emoteIcon.ImGuiHandle, new(chkSize));
                                    }
                                    ImGui.SameLine();
                                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                                    DrawCombo("emote", EmoteIdentifier.ListEmote, ref triggerData.emoteID, ref triggerData.emoteCommand);

                                    ImGui.TableNextColumn();
                                    if (ThreadLoadImageHandler.TryGetIconTextureWrap(EmoteIdentifier.FetchIcon(triggerData.expressionID), false, out var expressionIcon))
                                    {
                                        ImGui.Image(expressionIcon.ImGuiHandle, new(chkSize));
                                    }
                                    ImGui.SameLine();
                                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                                    DrawCombo("expression", EmoteIdentifier.ListExpression, ref triggerData.expressionID, ref triggerData.expressionCommand);

                                    ImGui.TableNextColumn();
                                    ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2f - chkSize / 2f-4);
                                    ImGui.Checkbox("##isContains", ref triggerData.isContains);

                                    ImGui.TableNextColumn();
                                    ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2f - chkSize / 2f-4);
                                    ImGui.Checkbox("##isCaseSensitive", ref triggerData.isCaseSensitive);

                                    ImGui.TableNextColumn();
                                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                                    ImGui.InputInt("##Priority", ref triggerData.priority, 10);

                                    if (triggerData.isLocked) ImGui.EndDisabled();
                                }
                            }
                            else
                            {
                                Svc.Log.Debug($"Key {key}");
                            }
                            i++;
                        }

                        // TRIGGER MODIFIED
                        if (isModified)
                        {
                            newTrigger = newTrigger.Trim();
                            ModifyTrigger();
                            newTrigger = string.Empty;
                            oldTrigger = string.Empty;
                        }
                        // END TRIGGER MODIFIED

                        // BUTTON DELETE PRESSED
                        if (!string.IsNullOrEmpty(deleteTrigger))
                        {
                            if (configGUI.triggerLists.TryGetValue(deleteTrigger, out var value))
                            {
                                TriggerData toRemove = value.Find(v => v.trigger == deleteTrigger);
                                value.Remove(toRemove);
                                if (value.Count == 0) configGUI.triggerLists.Remove(deleteTrigger);

                                int index = configGUI.triggerOrder.IndexOf(deleteTrigger);
                                if (index != -1)
                                {
                                    configGUI.triggerOrder.RemoveAt(index);
                                }
                            }
                            deleteTrigger = string.Empty;
                        }
                        // END BUTTON DELETE PRESSED

                        ImGui.PopID();
                        ImGui.EndTable();

                        if (beginDrag >= 0)
                        {
                            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                            {
                                //Svc.Log.Debug($"End {endDrag}");
                                if (endDrag != beginDrag)
                                {
                                    var move = configGUI.triggerOrder[beginDrag];
                                    configGUI.triggerOrder.RemoveAt(beginDrag);
                                    configGUI.triggerOrder.Insert(endDrag, move);
                                }

                                beginDrag = -1;
                                endDrag = -1;
                            }
                            else
                            {
                                var dl = ImGui.GetWindowDrawList();
                                dl.AddLine(endDragPosition, endDragPosition + new Num.Vector2(ImGui.GetWindowContentRegionMax().X, 0), ImGui.GetColorU32(ImGuiCol.DragDropTarget), 2 * ImGuiHelpers.GlobalScale);
                            }
                        }

                    }
                    ImGui.EndChildFrame();
                }
                //END TABLE
                //****************************** END TRIGGERS LIST

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
                Utility.KoFiButton.DrawRight();
                /*
                ImGui.Text($"Dictionary Count: {configGUI.triggerLists.Count}");
                var TEST = string.Empty;
                foreach (var chiave in configGUI.triggerLists.Keys)
                {
                    TEST = TEST + ',' + chiave + '(';
                    configGUI.triggerLists.TryGetValue(chiave, out var array);
                    foreach (var c in array)
                    {
                        TEST = TEST + ',' + c.trigger;
                    }
                    TEST = TEST + ')';
                }
                ImGui.SameLine();
                ImGui.Text(TEST);
                */
                ImGui.End();
            }
        }
        //****************************** ADD TRIGGER
        private void AddNewTrigger()
        {
            newTriggerData.trigger = newTriggerData.trigger.Trim();
            if (!string.IsNullOrEmpty(newTriggerData.trigger))
            {
                if (configGUI.triggerLists.TryGetValue(newTriggerData.trigger, out var value))
                {
                    if (value.Exists(v => v.trigger == newTriggerData.trigger))
                    {
                        Notify.Error($"Trigger '{newTriggerData.trigger}' already exists.");
                        return;
                    }
                    else
                    {
                        value.Add(newTriggerData);
                        configGUI.triggerLists[newTriggerData.trigger] = value;
                    }
                }
                else
                {
                    configGUI.triggerLists.Add(newTriggerData.trigger.ToLower(), new List<TriggerData> { newTriggerData });
                }
                configGUI.triggerOrder.Add(newTriggerData.trigger);
                newTriggerData = new TriggerData();
            }
        }
        //****************************** MODIFY TRIGGER
        private void ModifyTrigger()
        {
            if (string.IsNullOrEmpty(newTrigger)) return;

            configGUI.triggerLists.TryGetValue(oldTrigger, out var value);
            if (value == null) return;

            TriggerData editedTriggerData = new TriggerData();
            editedTriggerData.CopyValue(value.First(v => v.trigger == oldTrigger));
            editedTriggerData.trigger = newTrigger;

            if (configGUI.triggerLists.TryGetValue(newTrigger, out var triggerArray))
            {
                bool isExists = false;

                if (newTrigger.ToLower() == oldTrigger.ToLower())
                {
                    if (triggerArray.Count(v => v.trigger == newTrigger) > 0)
                    {
                        isExists = true;
                        Svc.Log.Information("Matched more than once for {0}", newTrigger);
                    }
                }
                else
                {
                    if (triggerArray.Exists(v => v.trigger == newTrigger))
                    {
                        isExists = true;
                        Svc.Log.Information("Trigger {0} exists", newTrigger);
                    }
                }

                if (isExists)
                {
                    Notify.Error($"Trigger '{newTrigger}' already exists.");
                    return;
                }
                else
                {
                    triggerArray.Add(editedTriggerData);
                    configGUI.triggerLists[editedTriggerData.trigger] = triggerArray;
                }
            }
            else
            {
                configGUI.triggerLists.Add(editedTriggerData.trigger.ToLower(), new List<TriggerData> { editedTriggerData });
            }

            editedTriggerData = value.First(v => v.trigger == oldTrigger);
            value.Remove(editedTriggerData);
            if (value.Count == 0)
            {
                configGUI.triggerLists.Remove(oldTrigger);
            }
            int index = configGUI.triggerOrder.IndexOf(oldTrigger);
            if (index != -1)
            {
                configGUI.triggerOrder[index] = newTrigger;
            }
        }
        private void SaveConfig()
        {
            config = configGUI;
            config.Save();
            InitializeConfig();
        }
    }
}
