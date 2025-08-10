using Dalamud.Interface.Utility;
using ECommons;
using Dalamud.Bindings.ImGui;
using System;

namespace AutoEmotion.Utility;

public static class KoFiButton
{
    public static bool IsOfficialPlugin = false;
    public const string Text = "Support on Ko-fi";
    public static string DonateLink => "https://ko-fi.com/foxshadow10" + (IsOfficialPlugin ? "?official" : "");
    public static void DrawRaw()
    {
        DrawButton();
    }

    const uint ColorNormal = 0xFF5E5BFF;
    const uint ColorHovered = 0xff5e5bff;
    const uint ColorActive = 0xFF5E5BFF;
    const uint ColorText = 0xFFFFFFFF;

    public static void DrawButton()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, ColorNormal);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorHovered);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorActive);
        ImGui.PushStyleColor(ImGuiCol.Text, ColorText);
        if (ImGui.Button(Text))
        {
            GenericHelpers.ShellStart(DonateLink);
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        ImGui.PopStyleColor(4);
    }

    public static void RightTransparentTab()
    {
        var textWidth = ImGui.CalcTextSize(Text).X;
        var spaceWidth = ImGui.CalcTextSize(" ").X;
        ImGui.BeginDisabled();
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0f);
        if (ImGui.BeginTabItem(" ".Repeat((int)MathF.Ceiling(textWidth / spaceWidth)), ImGuiTabItemFlags.Trailing))
        {
            ImGui.EndTabItem();
        }
        ImGui.PopStyleVar();
        ImGui.EndDisabled();
    }

    public static void DrawRight()
    {
        var cur = ImGui.GetCursorPos();
        ImGui.SetCursorPosX(cur.X + ImGui.GetContentRegionAvail().X - ImGuiHelpers.GetButtonSize(Text).X - 20);
        DrawRaw();
        ImGui.SetCursorPos(cur);
    }
}
