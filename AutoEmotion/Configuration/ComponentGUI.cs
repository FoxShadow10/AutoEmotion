using Dalamud.Interface.Utility;
using Dalamud.Interface;
using ECommons.DalamudServices;
using Dalamud.Bindings.ImGui;
using System;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Common.Math;
using Num = System.Numerics;
using AutoEmotion.Configuration.Data;
using ECommons.ImGuiMethods;
using AutoEmotion.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;

namespace AutoEmotion
{
    public partial class AutoEmotionPlugin : IDalamudPlugin
    {
        private TriggerData newTriggerData = new();

        private CharacterData? selectedCharacter = null;
        private string searchCharacter = string.Empty;
        private CharacterData? selectedCharacterB = null;
        private string searchCharacterB = string.Empty;
        private CharacterData copyCharacterReaction = new();

        private string searchInput = string.Empty;
        private string oldTrigger = string.Empty;
        private string newTrigger = string.Empty;

        private ReactionData newReactionData = new();

        private int beginDrag = -1;
        private int endDrag = -1;
        private Vector2 endDragPosition = new();

        private string stringRefIgnore = string.Empty;
        private int count = 3;

        private enum TableHeaderAlign { Left, Center, Right }

        private float chkSize { get; set; }
        private void InitializeGui()
        {
            configGUI.whiteList.TryGetValue(AutoEmotionConfig.EveryoneKey, out selectedCharacter);
        }

        private static void TableHeaderRow(string ID, string[] infoMarker, params TableHeaderAlign[] aligns)
        {
            ImGui.TableNextRow();
            for (var i = 0; i < ImGui.TableGetColumnCount(); i++)
            {
                ImGui.TableNextColumn();
                var label = ImGui.TableGetColumnName(i);
                ImGui.PushID($"{ID}Header_{i}");
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
                if (infoMarker[i].Length > 0)
                {
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X - (19f * ImGuiHelpers.GlobalScale));
                    ImGuiEx.InfoMarker(infoMarker[i], null, null, false);
                }
            }
        }

        public static void DrawTextSeparator(string ID, string Label)
        {
            var space = ImGuiHelpers.ScaledVector2(50f, 0f);
            var blank = new Num.Vector2(15f, 0f);
            var textSize = ImGui.CalcTextSize(Label);
            var pos = ImGui.GetCursorScreenPos() + new Num.Vector2(0, textSize.Y / 2f + 1f);
            ImGui.PushID(ID);
            ImGui.GetWindowDrawList().AddLine(
                pos,
                pos + space,
                ImGui.GetColorU32(ImGuiCol.Separator));
            ImGui.SetCursorPosX(space.X + blank.X);
            ImGui.Text(Label);
            ImGui.GetWindowDrawList().AddLine(
                pos + blank * 2 + space + textSize * Num.Vector2.UnitX,
                pos + ImGui.GetContentRegionAvail() * Num.Vector2.UnitX,
                ImGui.GetColorU32(ImGuiCol.Separator));
            ImGui.PopID();
        }

        public void DrawDragInt(string label, ref int v, int v_speed, float item_width = 120, int v_min = 0, int v_max = 0)
        {
            string[] text = label.Split(["##"], StringSplitOptions.None);
            float buttonSize = 0f;
            if (configGUI.showPlusMinus == true)
            {
                buttonSize = ((chkSize * 2f) + 1f);
            }
            ImGui.SetNextItemWidth(item_width - buttonSize);
            ImGui.DragInt($"##{text[1]}", ref v, v_speed, v_min, v_max);
            if (configGUI.showPlusMinus == true)
            {
                using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One * ImGuiHelpers.GlobalScale))
                {
                    using (ImRaii.PushFont(UiBuilder.IconFont))
                    {
                        ImGui.PushID($"buttons_{text[1]}");
                        ImGui.SameLine();
                        if (ImGui.Button($"{(char)FontAwesomeIcon.Plus}", new Vector2(chkSize)))
                        {
                            v = (v_min == 0 && v_max == 0) ? v + v_speed : Math.Min(v_max, v + v_speed);
                        }
                        ImGui.SameLine();
                        if (ImGui.Button($"{(char)FontAwesomeIcon.Minus}", new Vector2(chkSize)))
                        {
                            v = (v_min == 0 && v_max == 0) ? v - v_speed : Math.Max(v_min, v - v_speed);
                        }
                        ImGui.PopID();
                    }
                }
            }
            if (!string.IsNullOrEmpty(text[0]))
            {
                ImGui.SameLine();
                ImGui.Text(text[0]);
            }
        }

        public void DrawDragFloat(string label, ref float v, float v_speed, float item_width = 120, float v_min = 0, float v_max = 0)
        {
            string[] text = label.Split(["##"], StringSplitOptions.None);
            float buttonSize = 0f;
            if (configGUI.showPlusMinus == true)
            {
                buttonSize = ((chkSize * 2f) + 1f);
            }
            ImGui.SetNextItemWidth(item_width - buttonSize);
            ImGui.DragFloat($"##{text[1]}", ref v, v_speed, v_min, v_max);
            if (configGUI.showPlusMinus == true)
            {
                using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One * ImGuiHelpers.GlobalScale))
                {
                    using (ImRaii.PushFont(UiBuilder.IconFont))
                    {
                        ImGui.PushID($"buttons_{text[1]}");
                        ImGui.SameLine();
                        if (ImGui.Button($"{(char)FontAwesomeIcon.Plus}", new Vector2(chkSize)))
                        {
                            v = (v_min == 0 && v_max == 0) ? v + v_speed : Math.Min(v_max, v + v_speed);
                        }
                        ImGui.SameLine();
                        if (ImGui.Button($"{(char)FontAwesomeIcon.Minus}", new Vector2(chkSize)))
                        {
                            v = (v_min == 0 && v_max == 0) ? v - v_speed : Math.Max(v_min, v - v_speed);
                        }
                        ImGui.PopID();
                    }
                }
            }
            if (!string.IsNullOrEmpty(text[0]))
            {
                ImGui.SameLine();
                ImGui.Text(text[0]);
            }
        }

        public static void SetNextItemIndentation(int numberIndentation)
        {
            var indentation = ImGuiHelpers.ScaledVector2(27f * numberIndentation, 0f);
            ImGui.SetCursorPosX(indentation.X);
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
                    var icon = Svc.Texture.GetFromGameIcon(previewIcon).GetWrapOrDefault();
                    if (icon != null) drawlist.AddImage(icon.Handle, pos, pos + new Num.Vector2(size.Y));
                    var textSize = ImGui.CalcTextSize(previewText);
                    drawlist.AddText(pos + new Num.Vector2(size.Y + ImGui.GetStyle().FramePadding.X, size.Y / 2f - textSize.Y / 2f), ImGui.GetColorU32(ImGuiCol.Text), previewText);
                }
                ImGui.EndChildFrame();
            }
            return ImGui.IsItemClicked();
        }

        private void DrawComboEmote(string ID, uint previewID, bool isExpression, ref uint outEmoteID, ref string outEmoteCommand)
        {
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One * ImGuiHelpers.GlobalScale))
            {
                if (ThreadLoadImageHandler.TryGetIconTextureWrap(EmoteIdentifier.FetchIcon(previewID), false, out var iconPicture))
                {
                    ImGui.Image(iconPicture.Handle, new(chkSize));
                }
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

                IReadOnlyList<EmoteIdentifier> listEmote;
                if (isExpression)
                {
                    listEmote = EmoteIdentifier.ListExpression;
                }
                else
                {
                    listEmote = EmoteIdentifier.ListEmote;
                }
                if (ImGui.BeginCombo($"##{ID}", EmoteIdentifier.FetchName(previewID), ImGuiComboFlags.HeightLargest))
                {
                    if (ImGui.IsWindowAppearing())
                    {
                        searchInput = string.Empty;
                        ImGui.SetKeyboardFocusHere();
                    }

                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    ImGui.InputTextWithHint($"##{ID}SearchInput", "Search...", ref searchInput, 40);
                    if (ImGui.BeginChild($"##{ID}SearchScroll", new Vector2(ImGui.GetContentRegionAvail().X, 300)))
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
                        ImGui.EndChild();
                    }
                    ImGui.EndCombo();
                }
            }
        }

        private static bool MouseWithin(Vector2 min, Vector2 max)
        {
            var mousePos = ImGui.GetMousePos();
            return mousePos.X >= min.X && mousePos.Y <= max.X && mousePos.Y >= min.Y && mousePos.Y <= max.Y;
        }

        private void DrawChannelList()
        {
            var i = 0;
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
        }

        public void DrawTriggerList()
        {
            if (ImGui.BeginChild("TriggerChild", new Vector2(ImGui.GetContentRegionAvail().X, Math.Max(ImGui.GetContentRegionAvail().Y, 200f * ImGuiHelpers.GlobalScale)), false))
            {
                var keywordWidth = Math.Max(100f, ImGui.GetContentRegionAvail().X - ((chkSize + 150f * 2 + 80f + 80f + 245f) * ImGuiHelpers.GlobalScale));
                if (ImGui.BeginTable("HeaderTriggerTable", 7))
                {
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, chkSize * 3f + 3f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Keyword", ImGuiTableColumnFlags.WidthFixed, keywordWidth);
                    ImGui.TableSetupColumn("Emote", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150f) * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Expression", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150f) * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Contained", ImGuiTableColumnFlags.WidthFixed, 80f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Case Sensitive", ImGuiTableColumnFlags.WidthFixed, 80f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Priority", ImGuiTableColumnFlags.WidthFixed, ImGui.GetContentRegionAvail().X);
                    string[] infoMarkers = {
                             ""
                            ,""
                            ,""
                            ,""
                            ,""
                            ,""
                            ,"The keyword with the lowest priority will take precedence if multiple applicable triggers are found in a chat message."
                    };
                    TableHeaderRow("TriggerTable", infoMarkers, TableHeaderAlign.Left, TableHeaderAlign.Left, TableHeaderAlign.Left, TableHeaderAlign.Left, TableHeaderAlign.Center, TableHeaderAlign.Center, TableHeaderAlign.Center);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushID("##newTrigger");
                    using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One * ImGuiHelpers.GlobalScale))
                    {
                        ImGui.Dummy(new Vector2((chkSize * 2f) + 1f, 0f));
                        ImGui.SameLine();
                        using (ImRaii.PushFont(UiBuilder.IconFont))
                        {
                            if (ImGui.Button($"{(char)FontAwesomeIcon.Plus}##add", new Vector2(chkSize)))
                            {
                                AddNewTrigger();
                            }
                        }
                    }

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    ImGui.InputText("##newTrigger", ref newTriggerData.trigger, 100);

                    ImGui.TableNextColumn();
                    DrawComboEmote("newEmote", newTriggerData.emoteID, false, ref newTriggerData.emoteID, ref newTriggerData.emoteCommand);

                    ImGui.TableNextColumn();
                    DrawComboEmote("newExpression", newTriggerData.expressionID, true, ref newTriggerData.expressionID, ref newTriggerData.expressionCommand);

                    ImGui.TableNextColumn();
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2f - chkSize / 2f);
                    ImGui.Checkbox("##newIsContains", ref newTriggerData.isContains);

                    ImGui.TableNextColumn();
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2f - chkSize / 2f);
                    ImGui.Checkbox("##newIsCaseSensitive", ref newTriggerData.isCaseSensitive);

                    ImGui.TableNextColumn();
                    DrawDragInt("##newPriority", ref newTriggerData.priority, 10, ImGui.GetContentRegionAvail().X);

                    ImGui.EndTable();

                    if (ImGui.BeginTable("RowsTriggerTable", 7, ImGuiTableFlags.ScrollY))
                    {
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, chkSize * 3f + 3f * ImGuiHelpers.GlobalScale); //Buttons
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, keywordWidth); //Keyword
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150f) * ImGuiHelpers.GlobalScale); //Emote
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150f) * ImGuiHelpers.GlobalScale); //Expression
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 80f * ImGuiHelpers.GlobalScale); //Contained
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 80f * ImGuiHelpers.GlobalScale); //Case Sensitive
                        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, ImGui.GetContentRegionAvail().X); //Priority

                        string deleteTrigger = string.Empty;
                        bool isModified = false;
                        var i = 0;
                        var windowMax = ImGui.GetWindowPos() + ImGui.GetWindowSize();
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

                                    using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One * ImGuiHelpers.GlobalScale))
                                    {
                                        using (ImRaii.PushFont(UiBuilder.IconFont))
                                        {

                                            ImGui.Button($"{(char)FontAwesomeIcon.ArrowsUpDown}", new Vector2(chkSize));
                                            if (beginDrag == -1 && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                                            {
                                                beginDrag = i;
                                                endDrag = i;
                                                endDragPosition = ImGui.GetItemRectMin();
                                                //Svc.Log.Debug($"Begin {beginDrag} {endDragPosition}");
                                            }

                                            if (beginDrag >= 0 && MouseWithin(ImGui.GetItemRectMin(), new Vector2(windowMax.X, ImGui.GetItemRectMax().Y)))
                                            {
                                                endDrag = i;
                                                endDragPosition = ImGui.GetItemRectMin();
                                            }

                                            ImGui.SameLine();
                                            if (ImGui.Button($"{(char)(triggerData.isLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen)}", new Vector2(chkSize)))
                                            {
                                                if (triggerData.isLocked == false || ImGui.GetIO().KeyShift)
                                                    triggerData.isLocked = !triggerData.isLocked;
                                            }
                                        }
                                        if (ImGui.IsItemHovered())
                                        {
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
                                        }
                                        using (ImRaii.PushFont(UiBuilder.IconFont))
                                        {
                                            if (triggerData.isLocked) ImGui.BeginDisabled(triggerData.isLocked);

                                            ImGui.SameLine();
                                            if (ImGui.Button($"{(char)FontAwesomeIcon.Trash}##delete", new Vector2(chkSize)) && ImGui.GetIO().KeyShift)
                                            {
                                                deleteTrigger = triggerData.trigger;
                                            }
                                        }
                                        if (ImGui.IsItemHovered() && !ImGui.GetIO().KeyShift)
                                        {
                                            ImGui.SetTooltip("Hold SHIFT to delete.");
                                        }
                                    }

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
                                    DrawComboEmote("emote", triggerData.emoteID, false, ref triggerData.emoteID, ref triggerData.emoteCommand);

                                    ImGui.TableNextColumn();
                                    DrawComboEmote("expression", triggerData.expressionID, true, ref triggerData.expressionID, ref triggerData.expressionCommand);

                                    ImGui.TableNextColumn();
                                    ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2f - chkSize / 2f - 4f);
                                    ImGui.Checkbox("##isContains", ref triggerData.isContains);

                                    ImGui.TableNextColumn();
                                    ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2f - chkSize / 2f - 4f);
                                    ImGui.Checkbox("##isCaseSensitive", ref triggerData.isCaseSensitive);

                                    ImGui.TableNextColumn();
                                    DrawDragInt("##Priority", ref triggerData.priority, 10, ImGui.GetContentRegionAvail().X);

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
                                TriggerData? toRemove = value.Find(v => v.trigger == deleteTrigger);
                                if (toRemove != null)
                                {
                                    value.Remove(toRemove);
                                    if (value.Count == 0) configGUI.triggerLists.Remove(deleteTrigger);

                                    int index = configGUI.triggerOrder.IndexOf(deleteTrigger);
                                    if (index != -1)
                                    {
                                        configGUI.triggerOrder.RemoveAt(index);
                                    }
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
                                dl.AddLine(endDragPosition, endDragPosition + new Vector2(ImGui.GetWindowContentRegionMax().X, 0), ImGui.GetColorU32(ImGuiCol.DragDropTarget), 2 * ImGuiHelpers.GlobalScale);
                            }
                        }
                    }
                }
            }
            ImGui.EndChild();
        }

        private void AddNewTrigger()
        {
            newTriggerData.trigger = newTriggerData.trigger.Trim();
            if (!string.IsNullOrEmpty(newTriggerData.trigger))
            {
                if (configGUI.triggerLists.TryGetValue(newTriggerData.trigger, out var value))
                {
                    if (value.Exists(v => v.trigger == newTriggerData.trigger))
                    {
                        //Notify.Error($"Trigger '{newTriggerData.trigger}' already exists.");
                        Svc.Toasts.ShowError($"Trigger '{newTriggerData.trigger}' already exists.");
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

                if (newTrigger.Equals(oldTrigger, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (triggerArray.Any(v => v.trigger == newTrigger))
                    {
                        isExists = true;
                        Svc.Log.Debug("Matched more than once for {0}", newTrigger);
                    }
                }
                else
                {
                    if (triggerArray.Exists(v => v.trigger == newTrigger))
                    {
                        isExists = true;
                        Svc.Log.Debug("Trigger {0} exists", newTrigger);
                    }
                }

                if (isExists)
                {
                    //Notify.Error($"Trigger '{newTrigger}' already exists.");
                    Svc.Toasts.ShowError($"Trigger '{newTrigger}' already exists.");
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

        public void DrawWhiteList()
        {
            ImGui.Text("Whitelist:");
            ImGui.SameLine();
            ImGuiEx.InfoMarker($"Reactions configured for characters in the whitelist will take priority over Everyone reactions.{Environment.NewLine}If no specific reaction is set for a whitelisted character, the Everyone reaction will be used instead.{Environment.NewLine}You can quickly add a person to the whitelist using the following commands:{Environment.NewLine}/ae a{Environment.NewLine}/ae add{Environment.NewLine}/ae white");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##SearchWhiteList", "Search...", ref searchCharacter, 255);
            ImGui.Separator();

            ImGui.BeginChildFrame(ImGui.GetID("whitelistChildFrame"), new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - chkSize - (9f * ImGuiHelpers.GlobalScale)), ImGuiWindowFlags.NoBackground);
            foreach (var player in configGUI.whiteList)
            {
                if (!string.IsNullOrEmpty(searchCharacter))
                {
                    if (!player.Key.Contains(searchCharacter, StringComparison.CurrentCultureIgnoreCase)) continue;
                }
                uint isEnabledColorTrue;
                uint isEnabledColorFalse;
                unsafe
                {
                    Num.Vector4* color = ImGui.GetStyleColorVec4(ImGuiCol.TabActive);
                    isEnabledColorFalse = ImGui.ColorConvertFloat4ToU32(*color);
                    color = ImGui.GetStyleColorVec4(ImGuiCol.Text);
                    isEnabledColorTrue = ImGui.ColorConvertFloat4ToU32(*color);
                }
                using (ImRaii.PushColor(ImGuiCol.Text, player.Value.isEnabled ? isEnabledColorTrue : isEnabledColorFalse))
                {
                    ImGui.Checkbox($"##{player.Key}Enabled", ref player.Value.isEnabled);
                    ImGui.SameLine();
                    if (ImGui.Selectable($"{player.Key}##w{player.Key}", selectedCharacter == player.Value))
                    {
                        selectedCharacter = player.Value;
                    }
                }
            }
            ImGui.EndChildFrame();

            ImGui.Separator();

            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One * ImGuiHelpers.GlobalScale))
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    if (ImGui.Button($"{(char)FontAwesomeIcon.PersonCirclePlus}##addWhitelistTarget", new Vector2(chkSize)))
                    {
                        configGUI.TryToAddBWList(true);
                    }
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Add targeted player to the whitelist.");
                }
                ImGui.SameLine();
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    if (ImGui.Button($"{(char)FontAwesomeIcon.Trash}##removeWhitelist", new Vector2(chkSize)) && ImGui.GetIO().KeyShift)
                    {
                        if (selectedCharacter != null)
                        {
                            if (selectedCharacter.GetKey() != AutoEmotionConfig.EveryoneKey)
                            {
                                configGUI.whiteList.Remove(selectedCharacter.GetKey());
                                configGUI.whiteList.TryGetValue(AutoEmotionConfig.EveryoneKey, out selectedCharacter);
                            }
                        }
                    }
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"Hold SHIFT to remove the selected player from the white list.{Environment.NewLine}{AutoEmotionConfig.EveryoneKey} can not be removed.");
                }
            }
        }

        public void DrawBlackList()
        {
            ImGui.Text("Blacklist:");
            ImGui.SameLine();
            ImGuiEx.InfoMarker($"No reactions will be executed for characters added to the blacklist.{Environment.NewLine}You can quickly add a person to the blacklist using the following commands:{Environment.NewLine}/ae b{Environment.NewLine}/ae block{Environment.NewLine}/ae black");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("##SearchWhiteList", "Search...", ref searchCharacterB, 255);
            ImGui.Separator();

            ImGui.BeginChildFrame(ImGui.GetID("blacklistChildFrame"), new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y - chkSize - (9f * ImGuiHelpers.GlobalScale)), ImGuiWindowFlags.NoBackground);
            foreach (var player in configGUI.blackList)
            {
                if (!string.IsNullOrEmpty(searchCharacterB))
                {
                    if (!player.Key.Contains(searchCharacterB, StringComparison.CurrentCultureIgnoreCase)) continue;
                }
                if (ImGui.Selectable($"{player.Key}##b{player.Key}", selectedCharacterB == player.Value))
                {
                    selectedCharacterB = player.Value;
                }
            }
            ImGui.EndChildFrame();

            ImGui.Separator();

            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One * ImGuiHelpers.GlobalScale))
            {
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    if (ImGui.Button($"{(char)FontAwesomeIcon.PersonCirclePlus}##addBlacklistTarget", new Vector2(chkSize)))
                    {
                        configGUI.TryToAddBWList(false);
                    }
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Add targeted player to the blacklist.");
                }
                ImGui.SameLine();
                using (ImRaii.PushFont(UiBuilder.IconFont))
                {
                    if (ImGui.Button($"{(char)FontAwesomeIcon.Trash}##removeBlacklist", new Vector2(chkSize)) && ImGui.GetIO().KeyShift)
                    {
                        if (selectedCharacterB != null)
                        {
                            configGUI.blackList.Remove(selectedCharacterB.GetKey());
                            selectedCharacterB = null;
                        }
                    }
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Hold SHIFT to remove the selected player from the black list.");
                }
            }
        }

        public void DrawReactionList()
        {
            if (ImGui.BeginChild("ReactionChildFrame", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y), false))
            {
                if (selectedCharacter == null) return;
                var text = copyCharacterReaction.name == string.Empty ? "Copy" : $"Copied {copyCharacterReaction.name} reactions";
                if (ImGui.Button($"{text}##Copy"))
                {
                    copyCharacterReaction.CopyValue(selectedCharacter);
                }
                ImGui.SameLine();
                text = copyCharacterReaction.name == string.Empty ? "Paste" : $"Paste rections into {selectedCharacter.name}";
                if (ImGui.Button($"{text}##Paste"))
                {
                    if (copyCharacterReaction != null)
                        selectedCharacter.CopyReactionList(copyCharacterReaction.reactionList);
                    copyCharacterReaction = new();
                }

                if (ImGui.BeginTable("HeaderReactionTable", 8))
                {
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, chkSize * 3f + 3f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Received Emote", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150f) * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Response Emote", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150f) * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Response Expression", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150f) * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Distance (yalms)", ImGuiTableColumnFlags.WidthFixed, 120f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Response Delay (ms)", ImGuiTableColumnFlags.WidthFixed, 120f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Target Back", ImGuiTableColumnFlags.WidthFixed, 80f * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("Priority", ImGuiTableColumnFlags.WidthFixed, ImGui.GetContentRegionAvail().X);
                    string[] infoMarkers = {
                             ""
                            ,""
                            ,""
                            ,""
                            ,"Sets the distance (in yalms) at which you'll respond to incoming emotes.\r\n0 means unlimited range.\r\nNote: This setting is highly affected by server latency and may not always be accurate."
                            ,""
                            ,""
                            ,"The reaction with the lowest priority will take precedence if multiple applicable reaction are found."
                    };
                    TableHeaderRow("ReactionTable", infoMarkers, TableHeaderAlign.Left, TableHeaderAlign.Left, TableHeaderAlign.Left, TableHeaderAlign.Left,
                        TableHeaderAlign.Left, TableHeaderAlign.Center, TableHeaderAlign.Center, TableHeaderAlign.Center);

                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One * ImGuiHelpers.GlobalScale))
                    {
                        ImGui.Dummy(new Vector2((chkSize * 2f) + 1f, 0f));
                        ImGui.SameLine();
                        using (ImRaii.PushFont(UiBuilder.IconFont))
                        {
                            if (ImGui.Button($"{(char)FontAwesomeIcon.Plus}##addReaction", new Vector2(chkSize)))
                            {
                                AddNewReaction();
                            }
                        }
                    }

                    ImGui.TableNextColumn();
                    DrawComboEmote("newReceived", newReactionData.receivedEmoteID, false, ref newReactionData.receivedEmoteID, ref stringRefIgnore);

                    ImGui.TableNextColumn();
                    DrawComboEmote("newResponseEmote", newReactionData.responseEmoteID, false, ref newReactionData.responseEmoteID, ref newReactionData.responseEmoteCommand);

                    ImGui.TableNextColumn();
                    DrawComboEmote("newResponseExpression", newReactionData.responseExpressionID, true, ref newReactionData.responseExpressionID, ref newReactionData.responseExpressionCommand);

                    ImGui.TableNextColumn();
                    DrawDragFloat("##newYalms", ref newReactionData.yalms, 0.500f, ImGui.GetContentRegionAvail().X, 0, 500);

                    ImGui.TableNextColumn();
                    DrawDragInt("##newDelay", ref newReactionData.responseDelay, 100, ImGui.GetContentRegionAvail().X);

                    ImGui.TableNextColumn();
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2f - chkSize / 2f);
                    ImGui.Checkbox("##newTargetBack", ref newReactionData.targetBack);

                    ImGui.TableNextColumn();
                    DrawDragInt("##newReceivedPriority", ref newReactionData.priority, 10, ImGui.GetContentRegionAvail().X);

                    ImGui.EndTable();
                }

                if (selectedCharacter.reactionList.Count == 0)
                {
                    if (selectedCharacter.GetKey() == AutoEmotionConfig.EveryoneKey)
                        ImGui.Text($"No reactions are configured for {selectedCharacter.name}.{Environment.NewLine}You will not respond to any emotes.");
                    else
                        ImGui.Text($"No reactions are configured for {selectedCharacter.name}.{Environment.NewLine}Reactions set for Everyone will be used if available.");
                }

                if (ImGui.BeginTable("RowsReceivedTable", 8, ImGuiTableFlags.ScrollY))
                {
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, chkSize * 3f + 3f * ImGuiHelpers.GlobalScale);//Buttons
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150f) * ImGuiHelpers.GlobalScale);//Received Emote
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150f) * ImGuiHelpers.GlobalScale);//Response Emote
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, (chkSize + 150f) * ImGuiHelpers.GlobalScale);//Response Expression
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 120f * ImGuiHelpers.GlobalScale);//Distance (yalms)
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 120f * ImGuiHelpers.GlobalScale);//Response Delay (ms)
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 80f * ImGuiHelpers.GlobalScale);//Target Back
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, ImGui.GetContentRegionAvail().X);//Priority

                    int deleteIndex = -1;
                    int i = 0;

                    var windowMax = ImGui.GetWindowPos() + ImGui.GetWindowSize();
                    foreach (var reactionData in selectedCharacter.reactionList)
                    {
                        ImGui.PushID($"reaction_{i}");

                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();

                        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One * ImGuiHelpers.GlobalScale))
                        {
                            using (ImRaii.PushFont(UiBuilder.IconFont))
                            {

                                ImGui.Button($"{(char)FontAwesomeIcon.ArrowsUpDown}", new Vector2(chkSize));
                                if (beginDrag == -1 && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && ImGui.IsMouseDown(ImGuiMouseButton.Left))
                                {
                                    beginDrag = i;
                                    endDrag = i;
                                    endDragPosition = ImGui.GetItemRectMin();
                                    //Svc.Log.Debug($"Begin {beginDrag} {endDragPosition}");
                                }

                                if (beginDrag >= 0 && MouseWithin(ImGui.GetItemRectMin(), new Vector2(windowMax.X, ImGui.GetItemRectMax().Y)))
                                {
                                    endDrag = i;
                                    endDragPosition = ImGui.GetItemRectMin();
                                }

                                ImGui.SameLine();
                                if (ImGui.Button($"{(char)(reactionData.isLocked ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen)}", new Vector2(chkSize)))
                                {
                                    if (reactionData.isLocked == false || ImGui.GetIO().KeyShift)
                                        reactionData.isLocked = !reactionData.isLocked;
                                }
                            }
                            if (ImGui.IsItemHovered())
                            {
                                if (reactionData.isLocked && ImGui.GetIO().KeyShift)
                                {
                                    ImGui.SetTooltip("Unlock Entry.");
                                }
                                else if (reactionData.isLocked)
                                {
                                    ImGui.SetTooltip("Hold SHIFT to unlock.");
                                }
                                else
                                {
                                    ImGui.SetTooltip("Lock Entry.");
                                }
                            }
                            using (ImRaii.PushFont(UiBuilder.IconFont))
                            {

                                if (reactionData.isLocked) ImGui.BeginDisabled(reactionData.isLocked);

                                ImGui.SameLine();
                                if (ImGui.Button($"{(char)FontAwesomeIcon.Trash}##delete", new Vector2(chkSize)) && ImGui.GetIO().KeyShift)
                                {
                                    deleteIndex = i;
                                }
                            }
                            if (ImGui.IsItemHovered() && !ImGui.GetIO().KeyShift)
                            {
                                ImGui.SetTooltip("Hold SHIFT to delete.");
                            }
                        }

                        ImGui.TableNextColumn();
                        DrawComboEmote("rowReceived", reactionData.receivedEmoteID, false, ref reactionData.receivedEmoteID, ref stringRefIgnore);

                        ImGui.TableNextColumn();
                        DrawComboEmote("rowResponseEmote", reactionData.responseEmoteID, false, ref reactionData.responseEmoteID, ref reactionData.responseEmoteCommand);

                        ImGui.TableNextColumn();
                        DrawComboEmote("rowResponseExpression", reactionData.responseExpressionID, true, ref reactionData.responseExpressionID, ref reactionData.responseExpressionCommand);

                        ImGui.TableNextColumn();
                        DrawDragFloat("##rowYalms", ref reactionData.yalms, 0.500f, ImGui.GetContentRegionAvail().X, 0, 500);

                        ImGui.TableNextColumn();
                        DrawDragInt("##rowDelay", ref reactionData.responseDelay, 100, ImGui.GetContentRegionAvail().X);

                        ImGui.TableNextColumn();
                        ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2f - chkSize / 2f - 4f);
                        ImGui.Checkbox("##rowTargetBack", ref reactionData.targetBack);

                        ImGui.TableNextColumn();
                        DrawDragInt("##rowReceivedPriority", ref reactionData.priority, 10, ImGui.GetContentRegionAvail().X);

                        if (reactionData.isLocked) ImGui.EndDisabled();
                        i++;
                    }
                    if (deleteIndex > -1)
                    {
                        selectedCharacter.reactionList.RemoveAt(deleteIndex);
                    }

                    ImGui.PopID();
                    ImGui.EndTable();
                    if (beginDrag >= 0)
                    {
                        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
                        {
                            //Svc.Log.Debug($"End {endDrag}");
                            if (endDrag != beginDrag && selectedCharacter != null)
                            {
                                var move = selectedCharacter.reactionList[beginDrag];
                                selectedCharacter.reactionList.RemoveAt(beginDrag);
                                selectedCharacter.reactionList.Insert(endDrag, move);
                            }
                            beginDrag = -1;
                            endDrag = -1;
                        }
                        else
                        {
                            var dl = ImGui.GetWindowDrawList();
                            dl.AddLine(endDragPosition, endDragPosition + new Vector2(ImGui.GetWindowContentRegionMax().X, 0), ImGui.GetColorU32(ImGuiCol.DragDropTarget), 2f * ImGuiHelpers.GlobalScale);
                        }
                    }
                }
            }
            ImGui.EndChild();
        }

        private void AddNewReaction()
        {
            if (selectedCharacter != null)
            {
                if (newReactionData.receivedEmoteID > 0)
                {
                    selectedCharacter.reactionList.Add(newReactionData);
                    newReactionData = new ReactionData();
                }
            }
        }

        private void DrawStatusWin()
        {
            if (isOpenStatusWin && config.isActived)
            {
                var Flags = ImGuiWindowFlags.NoDecoration |
                            ImGuiWindowFlags.AlwaysAutoResize |
                            ImGuiWindowFlags.NoFocusOnAppearing |
                            ImGuiWindowFlags.NoNav |
                            ImGuiWindowFlags.NoBackground;

                if (config.isStatusWinLocked)
                {
                    Flags |= ImGuiWindowFlags.NoMove;
                }
                ImGui.Begin("StatusWindow", Flags);
                try
                {
                    if (config.isChatActive)
                    {
                        ThreadLoadImageHandler.TryGetIconTextureWrap(40, false, out var chatImg);//45
                        if (chatImg != null)
                        {
                            ImGui.Image(chatImg.Handle, new(chkSize));
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Chat Reactions are enabled.");
                            }
                        }
                    }
                    else
                    {
                        ImGui.Dummy(new Vector2(chkSize));
                    }
                    ImGui.SameLine();
                    if (config.isReactionActive)
                    {
                        ThreadLoadImageHandler.TryGetIconTextureWrap(9, false, out var reactionImg);
                        if (reactionImg != null)
                        {
                            ImGui.Image(reactionImg.Handle, new(chkSize));
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Emote Reactions are enabled.");
                            }
                        }
                    }
                    else
                    {
                        ImGui.Dummy(new Vector2(chkSize));
                    }
                }
                catch { }
                ImGui.End();
            }
        }
    }
}
