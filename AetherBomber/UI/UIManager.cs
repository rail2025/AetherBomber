// AetherBomber/UI/UIManager.cs
using System;
using System.Numerics;
using AetherBomber.Windows;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;

namespace AetherBomber.UI;

public static class UIManager
{
    private static void DrawTextWithOutline(ImDrawListPtr drawList, string text, Vector2 pos, uint color, uint outlineColor, float size = 1f)
    {
        var fontSize = ImGui.GetFontSize() * size;
        var outlineOffset = new Vector2(1, 1);

        drawList.AddText(ImGui.GetFont(), fontSize, pos - outlineOffset, outlineColor, text);
        drawList.AddText(ImGui.GetFont(), fontSize, pos + new Vector2(outlineOffset.X, -outlineOffset.Y), outlineColor, text);
        drawList.AddText(ImGui.GetFont(), fontSize, pos + new Vector2(-outlineOffset.X, outlineOffset.Y), outlineColor, text);
        drawList.AddText(ImGui.GetFont(), fontSize, pos + outlineOffset, outlineColor, text);
        drawList.AddText(ImGui.GetFont(), fontSize, pos, color, text);
    }

    public static void DrawMainMenu(Plugin plugin, Action startGame, Action openSettings, Action openAbout)
    {
        var drawList = ImGui.GetWindowDrawList();
        var windowPos = ImGui.GetWindowPos();
        var title = "AetherBomber";
        var titleFontSize = 3.5f;
        var titleSize = ImGui.CalcTextSize(title) * titleFontSize;
        var titlePos = new Vector2(windowPos.X + (MainWindow.ScaledWindowSize.X - titleSize.X) * 0.5f, windowPos.Y + MainWindow.ScaledWindowSize.Y * 0.2f);

        DrawTextWithOutline(drawList, title, titlePos, 0xFFFFFFFF, 0xFF000000, titleFontSize);

        var buttonSize = new Vector2(140, 40) * ImGuiHelpers.GlobalScale;
        var startY = MainWindow.ScaledWindowSize.Y * 0.45f;
        uint buttonTextColor = 0xFFFFFFFF;
        uint buttonOutlineColor = 0xFF000000;

        void DrawButtonWithOutline(string label, string id, Vector2 position, Vector2 size, Action onClick)
        {
            ImGui.SetCursorPos(position);
            if (ImGui.Button($"##{id}", size))
            {
                onClick();
            }
            var textSize = ImGui.CalcTextSize(label) * 1.2f;
            var textPos = windowPos + position + new Vector2((size.X - textSize.X) * 0.5f, (size.Y - textSize.Y) * 0.5f);
            DrawTextWithOutline(drawList, label, textPos, buttonTextColor, buttonOutlineColor, 1.2f);
        }

        float currentY = startY;
        var buttonSpacing = 50f * ImGuiHelpers.GlobalScale;
        var buttonX = (MainWindow.ScaledWindowSize.X - buttonSize.X) * 0.5f;

        DrawButtonWithOutline("Start Game", "Start", new Vector2(buttonX, currentY), buttonSize, startGame);
        currentY += buttonSpacing;

        DrawButtonWithOutline("Multiplayer", "Multiplayer", new Vector2(buttonX, currentY), buttonSize, plugin.ToggleMultiplayerUI);
        currentY += buttonSpacing;

        DrawButtonWithOutline("Settings", "Settings", new Vector2(buttonX, currentY), buttonSize, plugin.ToggleConfigUI);
        currentY += buttonSpacing;

        DrawButtonWithOutline("About", "About", new Vector2(buttonX, currentY), buttonSize, openAbout);
    }
}
