using System;
using System.Numerics;
using AetherBomber.UI;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;

namespace AetherBomber.Windows
{
    public class TitleWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly TextureManager textureManager;

        public TitleWindow(Plugin plugin, string idSuffix = "") : base("AetherBomber###AetherBomberTitleWindow" + idSuffix)
        {
            this.plugin = plugin;
            this.textureManager = new TextureManager();

            var baseSize = new Vector2(720, 540);
            this.Size = baseSize * ImGuiHelpers.GlobalScale;
            Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar;
        }

        public override void OnOpen()
        {
            plugin.AudioManager.StartBgmPlaylist();
        }

        public override void OnClose()
        {
            if (!plugin.MainWindow.IsOpen)
            {
                plugin.AudioManager.StopBgm();
            }
        }

        public void Dispose()
        {
            textureManager.Dispose();
        }

        public override void Draw()
        {
            var backgroundTexture = this.textureManager.GetBackground(0);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
            if (backgroundTexture != null)
            {
                var windowPos = ImGui.GetWindowPos();
                var windowSize = ImGui.GetWindowSize();
                ImGui.GetWindowDrawList().AddImage(backgroundTexture.Handle, windowPos, windowPos + windowSize);
            }

            var startAction = () =>
            {
                this.IsOpen = false;
                if (this.WindowName.Contains("2"))
                {
                    if (plugin.secondMainWindow != null) plugin.secondMainWindow.IsOpen = true;
                }
                else
                {
                    plugin.MainWindow.IsOpen = true;
                }
            };

            var multiplayerAction = () =>
            {
                if (this.WindowName.Contains("2"))
                {
                    plugin.secondMultiplayerWindow?.Toggle();
                }
                else
                {
                    plugin.MultiplayerWindow.Toggle();
                }
            };

            UIManager.DrawMainMenu(plugin, startAction, multiplayerAction, plugin.ToggleConfigUI, plugin.ToggleAboutUI);


            ImGui.PopStyleColor();
        }
    }
}
