// AetherBomber/Plugin.cs
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using AetherBomber.Windows;
using AetherBomber.Audio;
using Dalamud.Game.ClientState.Conditions;
using AetherBomber.Networking;
using AetherBomber.Game;
using System.Collections.Concurrent;
using System;
using Dalamud.Game.ClientState.GamePad;

namespace AetherBomber;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IPartyList? PartyList { get; private set; } = null!;
    [PluginService] internal static IGamepadState? GamepadState { get; private set; } = null!;

    private const string CommandName = "/aetherbomber";
    private const string SecondCommandName = "/abomb2";

    public Configuration Configuration { get; init; }
    public AudioManager AudioManager { get; init; }
    public readonly WindowSystem WindowSystem = new("AetherBomber");
    private ConfigWindow ConfigWindow { get; init; }
    public MainWindow MainWindow { get; init; }
    public TitleWindow TitleWindow { get; init; }

    private AboutWindow AboutWindow { get; init; }
    public MultiplayerWindow MultiplayerWindow { get; init; }

    public TitleWindow? secondTitleWindow;
    public MainWindow? secondMainWindow;
    public MultiplayerWindow? secondMultiplayerWindow;

    private bool wasDead = false;
    public MultiplayerGameSession? MultiplayerSession { get; private set; }

    // This queue holds actions that need to be run on the main UI thread.
    public readonly ConcurrentQueue<Action> MainThreadActions = new();

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);
        AudioManager = new AudioManager(this.Configuration);

        ConfigWindow = new ConfigWindow(this, this.AudioManager);
        MainWindow = new MainWindow(this, this.AudioManager);
        TitleWindow = new TitleWindow(this);
        AboutWindow = new AboutWindow();
        MultiplayerWindow = new MultiplayerWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(TitleWindow);
        WindowSystem.AddWindow(AboutWindow);
        WindowSystem.AddWindow(MultiplayerWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the AetherBomber game window."
        });
        CommandManager.AddHandler(SecondCommandName, new CommandInfo(OnSecondWindowCommand)
        {
            HelpMessage = "Opens a second AetherBomber window for testing."
        });


        ClientState.TerritoryChanged += OnTerritoryChanged;
        Condition.ConditionChange += OnConditionChanged;

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
    }

    public void Dispose()
    {
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        Condition.ConditionChange -= OnConditionChanged;

        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(SecondCommandName);

        this.WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        MainWindow.Dispose();
        TitleWindow.Dispose();
        AboutWindow.Dispose();
        MultiplayerWindow.Dispose();
        this.secondMainWindow?.Dispose();
        this.secondTitleWindow?.Dispose();
        this.secondMultiplayerWindow?.Dispose();
        this.AudioManager.Dispose();
    }

    private void OnCommand(string command, string args) => TitleWindow.Toggle();

    public void ToggleTitleUI(string idSuffix = "")
    {
        if (idSuffix == "2")
        {
            secondTitleWindow?.Toggle();
        }
        else
        {
            TitleWindow.Toggle();
        }
    }

    private void OnSecondWindowCommand(string command, string args)
    {
        if (this.secondTitleWindow == null)
        {
            this.secondTitleWindow = new TitleWindow(this, "2");
            this.secondMainWindow = new MainWindow(this, this.AudioManager, "2");
            this.secondMultiplayerWindow = new MultiplayerWindow(this, "2");
            this.WindowSystem.AddWindow(this.secondTitleWindow);
            this.WindowSystem.AddWindow(this.secondMainWindow);
            this.WindowSystem.AddWindow(this.secondMultiplayerWindow);
        }
        this.secondTitleWindow.Toggle();
    }

    private void DrawUI()
    {
        while (this.MainThreadActions.TryDequeue(out var action))
        {
            action.Invoke();
        }
        this.WindowSystem.Draw();
    }

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleAboutUI() => AboutWindow.Toggle();

    public void StartMultiplayerGame()
    {
        if (MultiplayerSession == null) return;

        // Close lobby windows
        MultiplayerWindow.IsOpen = false;
        if (secondMultiplayerWindow != null)
        {
            secondMultiplayerWindow.IsOpen = false;
        }

        // Open game windows and start multiplayer sessions
        MainWindow.IsOpen = true;
        MainWindow.StartMultiplayerGame(1, MultiplayerSession.Players.Count);

        if (secondMainWindow != null)
        {
            secondMainWindow.IsOpen = true;
            secondMainWindow.StartMultiplayerGame(2, MultiplayerSession.Players.Count);
        }
    }

    public void OnClientConnected(string passphrase, MultiplayerWindow clientWindow)
    {
        Plugin.Log.Debug($"[Plugin] OnClientConnected called by window '{clientWindow.WindowName}'.");
        if (this.MultiplayerSession == null)
        {
            Plugin.Log.Debug("[Plugin] MultiplayerSession is null. Creating new session.");
            this.MultiplayerSession = new MultiplayerGameSession(passphrase);
        }

        Plugin.Log.Debug($"[Plugin] Players before add: {this.MultiplayerSession.Players.Count}");
        this.MultiplayerSession.AddPlayer($"Player {this.MultiplayerSession.Players.Count + 1}");
        Plugin.Log.Debug($"[Plugin] Players after add: {this.MultiplayerSession.Players.Count}");

        clientWindow.SetConnectionStatus("Connected", false);

        // Also update the other client's lobby screen if it exists
        if (clientWindow == MultiplayerWindow)
        {
            Plugin.Log.Debug("[Plugin] Notifying second client window to update.");
            secondMultiplayerWindow?.UpdateLobby();
        }
        else
        {
            Plugin.Log.Debug("[Plugin] Notifying first client window to update.");
            MultiplayerWindow.UpdateLobby();
        }
    }

    public void OnClientDisconnected(MultiplayerWindow clientWindow)
    {
        // For simplicity, if one client disconnects, we end the session for both in local testing.
        MultiplayerSession = null;
        MultiplayerWindow.SetConnectionStatus("Disconnected", true);
        secondMultiplayerWindow?.SetConnectionStatus("Disconnected", true);
    }

    public void OnStartGameReceived()
    {
        MainThreadActions.Enqueue(StartMultiplayerGame);
    }

    private void OnTerritoryChanged(ushort territoryTypeId)
    {
        if (MainWindow.IsOpen) { MainWindow.IsOpen = false; }
        if (TitleWindow.IsOpen) { TitleWindow.IsOpen = false; }
        if (secondMainWindow != null && secondMainWindow.IsOpen) { secondMainWindow.IsOpen = false; }
        if (secondTitleWindow != null && secondTitleWindow.IsOpen) { secondTitleWindow.IsOpen = false; }
    }

    private void OnConditionChanged(ConditionFlag flag, bool value)
    {
        if (flag == ConditionFlag.InCombat && !value)
        {
            bool isDead = ClientState.LocalPlayer?.CurrentHp == 0;
            if (isDead && !wasDead && Configuration.OpenOnDeath) { TitleWindow.IsOpen = true; }
            wasDead = isDead;
        }

        if (flag == ConditionFlag.InDutyQueue && value && Configuration.OpenInQueue) { TitleWindow.IsOpen = true; }
        if (flag == ConditionFlag.UsingPartyFinder && value && Configuration.OpenInPartyFinder) { TitleWindow.IsOpen = true; }
        if (flag == ConditionFlag.Crafting && value && Configuration.OpenDuringCrafting) { TitleWindow.IsOpen = true; }
    }
}
