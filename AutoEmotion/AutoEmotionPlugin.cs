using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Automation;
using Dalamud.Game.Gui.Toast;
using AutoEmotion.Hooks;
using AutoEmotion.Configuration.Data;
using AutoEmotion.Utility;
using Dalamud.Game.ClientState.Conditions;
using AutoEmotion.ChatMessages;
using System.Linq;

namespace AutoEmotion;

public partial class AutoEmotionPlugin : IDalamudPlugin
{
    public AutoEmotionConfig config { get; set; }
    private AutoEmotionConfig configGUI { get; set; }
    private bool isOpenConfig;
    private bool isOpenStatusWin;
    private const string CommandName = "/aemote";
    private const string CommandActivate = "/aet";
    private const string Commands = "/ae";

    public Queue<QueueData> messageQueue { get; init; }
    public Queue<QueueData> messageQueueResume { get; init; }
    private QueueData messageProcess = new();
    private QueueData messageProcessResume = new();
    public ReactionCache reactionCache = new();
    public Stopwatch timer { get; init; }
    public Stopwatch timerResume { get; init; }
    public EmoteReaderHooks EmoteReaderHooks { get; init; }
    public AutoEmotionPlugin(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this);

        configGUI = Svc.PluginInterface.GetPluginConfig() as AutoEmotionConfig ?? new AutoEmotionConfig();
        configGUI.Initialize(Svc.PluginInterface);
        InitializeConfig();
        InitializeGui();
        AddHandler();

        messageQueue = new Queue<QueueData>();
        messageQueueResume = new Queue<QueueData>();
        timer = new Stopwatch();
        timerResume = new Stopwatch();

        EmoteReaderHooks = new EmoteReaderHooks();
        EmoteReaderHooks.OnEmote += OnEmote;

        Svc.Chat.ChatMessage += ChatMessage;
        Svc.Framework.Update += FrameworkUpdate;

        Svc.PluginInterface.UiBuilder.Draw += DrawConfigUI;
        Svc.PluginInterface.UiBuilder.Draw += DrawStatusWin;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        Svc.PluginInterface.UiBuilder.OpenMainUi += OpenConfig;

        Svc.ClientState.Login += ClientState_Login;
        Svc.ClientState.Logout += ClientState_Logout;
    }

    private void InitializeConfig()
    {
        config = Svc.PluginInterface.GetPluginConfig() as AutoEmotionConfig ?? new AutoEmotionConfig();
        config.Initialize(Svc.PluginInterface);
        reactionCache = new ReactionCache(config.maxReactionsCache, config.timeoutReactionsCache);
    }

    private void FrameworkUpdate(IFramework framework)
    {
        if (config != null)
        {
            try
            {
                if (messageQueue.Any() || timer.IsRunning)
                {
                    if (!timer.IsRunning)
                    {
                        messageProcess = messageQueue.Dequeue();
                        timer.Start();
                    }
                    if (timer.ElapsedMilliseconds > messageProcess.delay)
                    {
                        if (!string.IsNullOrEmpty(messageProcess.expression))
                        {
                            messageProcess.TargetBack();
                            var expression = messageProcess.GetExpression(config.isChatLogHidden);
                            Chat.Instance.ExecuteCommand(expression);
                            Svc.Log.Debug($"Expressione: {expression}");
                            messageProcess.expression = string.Empty;
                            messageProcess.SetDelay(messageProcess.delay + messageProcess.minDelay);
                            return;
                        }
                        if (!string.IsNullOrEmpty(messageProcess.emote))
                        {
                            if (config.resumeEmoteLoop)
                            {
                                if (Svc.Condition.Any(ConditionFlag.Emoting))
                                {
                                    var currentEmote = CharacterUtility.GetCurrentEmote(true);
                                    if (currentEmote != null)
                                    {
                                        if (currentEmote.EmoteCommand != messageProcess.emote)
                                        {
                                            var queueData = new QueueData() { emote = currentEmote.EmoteCommand };
                                            queueData.SetDelay(config.resumeEmoteDelay);
                                            if (Svc.ClientState.LocalPlayer != null)
                                            {
                                                queueData.rotation = Svc.ClientState.LocalPlayer.Rotation;
                                            }
                                            messageQueueResume.Enqueue(queueData);
                                            Svc.Log.Debug($"Enqueue resume Emote: {queueData.emote} with delay {queueData.delay}ms");
                                        }
                                    }
                                }
                            }
                            messageProcess.TargetBack();
                            var emote = messageProcess.GetEmote(config.isChatLogHidden);
                            Chat.Instance.ExecuteCommand(emote);
                            Svc.Log.Debug($"Emote: {emote}");
                            messageProcess.emote = string.Empty;
                            return;
                        }
                        timer.Reset();
                    }
                }
                else if (timerResume.IsRunning)
                {
                    var currentEmote = CharacterUtility.GetCurrentEmote();
                    if (currentEmote != null && currentEmote.EmoteID == 0)
                    {
                        if (!string.IsNullOrEmpty(messageProcessResume.emote))
                        {
                            if (config.resumeEmoteDetarget) Svc.Targets.Target = null;
                            var emote = messageProcessResume.GetEmote(config.isChatLogHidden);
                            Chat.Instance.ExecuteCommand(emote);
                            if (config.resumeEmoteRestoreRotation)
                            {
                                CharacterUtility.SetRotation(messageProcessResume.rotation);
                            }
                            Svc.Log.Debug($"Resume Emote: {emote}");
                        }
                        timerResume.Reset();
                    }
                }
                else if (messageQueueResume.Any())
                {
                    messageProcessResume = messageQueueResume.Dequeue();
                    timerResume.Start();
                }
            }
            catch (Exception e)
            {
                Svc.Log.Error($"[Chat Manager]: Failed to process Framework Update! ${e}: ${e.Message}");
            }
            reactionCache.CleanExpiredActions();
        }
    }

    public void Dispose()
    {
        Svc.Chat.ChatMessage -= ChatMessage;
        Svc.Framework.Update -= FrameworkUpdate;
        Svc.PluginInterface.UiBuilder.Draw -= DrawConfigUI;
        Svc.PluginInterface.UiBuilder.Draw -= DrawStatusWin;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
        Svc.ClientState.Login -= ClientState_Login;
        Svc.ClientState.Logout -= ClientState_Logout;
        EmoteReaderHooks.OnEmote -= OnEmote;
        EmoteReaderHooks.Dispose();
        RemoveHandler();
        ECommonsMain.Dispose();
    }

    private void AddHandler()
    {
        Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the plugin configuration window."
        });
        Svc.Commands.AddHandler(CommandActivate, new CommandInfo(OnActiveCommand)
        {
            HelpMessage = """
                Enable/disable the plugin.
                /aet c | chat → Enable/disable the chat reactions.
                /aet r | reaction → Enable/disable the emote reactions.
                """
        });
        Svc.Commands.AddHandler(Commands, new CommandInfo(OnCommands)
        {
            HelpMessage = """
                Opens the plugin configuration window.
                /ae a | add | white → Adds the targeted character to the whitelist.
                /ae b | block | black → Adds the targeted character to the blacklist.
                /ae c | clear → Clear all emotes currently queued.
                """
        });
    }

    private void RemoveHandler()
    {
        Svc.Commands.RemoveHandler(CommandName);
        Svc.Commands.RemoveHandler(CommandActivate);
        Svc.Commands.RemoveHandler(Commands);
    }

    private void OnCommand(string command, string args)
    {
        OpenConfig();
    }

    private void OnActiveCommand(string command, string args)
    {
        if (args.EqualsIgnoreCaseAny("c", "chat"))
        {
            config.isChatActive = !config.isChatActive;
            configGUI.isChatActive = config.isChatActive;
            Svc.Toasts.ShowQuest("AutoEmotion Chat Reaction " + (config.isChatActive ? "Enabled." : "Disabled."),
            new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
        }
        else if (args.EqualsIgnoreCaseAny("r", "reaction"))
        {
            config.isReactionActive = !config.isReactionActive;
            configGUI.isReactionActive = config.isReactionActive;
            Svc.Toasts.ShowQuest("AutoEmotion Emote Reaction " + (config.isReactionActive ? "Enabled." : "Disabled."),
            new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
        }
        else
        {
            ClearQueue();
            config.isActived = !config.isActived;
            configGUI.isActived = config.isActived;
            Svc.Toasts.ShowQuest("AutoEmotion " + (config.isActived ? "Enabled." : "Disabled."),
            new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
        }
        config.Save();
    }

    private void OnCommands(string command, string args)
    {
        if (args.EqualsIgnoreCaseAny("a", "add", "white"))
        {
            config.TryToAddBWList(true, true);
            configGUI.TryToAddBWList(true);
            config.Save();
        }
        else if (args.EqualsIgnoreCaseAny("b", "block", "black"))
        {
            config.TryToAddBWList(false, true);
            configGUI.TryToAddBWList(false);
            config.Save();
        }
        else if (args.EqualsIgnoreCaseAny("c", "clear"))
        {
            ClearQueue();
            Svc.Toasts.ShowQuest("Emote queue cleared successfully.",
            new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
        }
        else
        {
            OpenConfig();
        }
    }

    private void ClientState_Login()
    {
        isOpenStatusWin = config.isStatusWinOpen;
    }

    private void ClientState_Logout(int type, int code)
    {
        isOpenStatusWin = false;
    }

    private void OpenConfig() => isOpenConfig = true;

    private void ClearQueue()
    {
        messageQueue.Clear();
        messageProcess.Clear();
        messageQueueResume.Clear();
        messageProcessResume.Clear();
    }
}
