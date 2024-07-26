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


namespace AutoEmotion;

public partial class AutoEmotionPlugin : IDalamudPlugin
{
    public AutoEmotionConfig config { get; set; }
    private AutoEmotionConfig configGUI { get; set; }
    private bool isOpenConfig;
    private const string commandName = "/aemote";
    private const string commandActivate = "/aet";
    public Queue<string> messageQueue { get; init; }
    public Stopwatch timer { get; init; }

    public AutoEmotionPlugin(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this);
        InitializeConfig();

        Svc.Commands.AddHandler(commandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the plugin configuration window."
        });
        Svc.Commands.AddHandler(commandActivate, new CommandInfo(OnActiveCommand)
        {
            HelpMessage = "Enable/disable the plugin."
        });

        messageQueue = new Queue<string>();
        timer = new Stopwatch();
        Svc.Chat.ChatMessage += ChatMessage;
        Svc.Framework.Update += FrameworkUpdate;

        Svc.PluginInterface.UiBuilder.Draw += DrawConfigUI;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
    }

    private void InitializeConfig()
    {
        config = Svc.PluginInterface.GetPluginConfig() as AutoEmotionConfig ?? new AutoEmotionConfig();
        config.Initialize(Svc.PluginInterface);
        configGUI = Svc.PluginInterface.GetPluginConfig() as AutoEmotionConfig ?? new AutoEmotionConfig();
        configGUI.Initialize(Svc.PluginInterface);
    }

    private void FrameworkUpdate(IFramework framework)
    {
        if (config != null)
        {
            try
            {
                if (messageQueue.Count > 0)
                {
                    if (!timer.IsRunning)
                    {
                        timer.Start();
                    }
                    else
                    {
                        if (timer.ElapsedMilliseconds > 500)
                        {
                            try
                            {
                                Chat.Instance.ExecuteCommand(messageQueue.Dequeue());
                            }
                            catch (Exception e)
                            {
                                Svc.Log.Error($"{e},{e.Message}");
                            }
                            timer.Restart();
                        }
                    }
                }
            }
            catch
            {
                Svc.Log.Error($"[Chat Manager]: Failed to process Framework Update!");
            }
        }
    }

    public void Dispose()
    {
        Svc.Chat.ChatMessage -= ChatMessage;
        Svc.Framework.Update -= FrameworkUpdate;
        Svc.PluginInterface.UiBuilder.Draw -= DrawConfigUI;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
        Svc.Commands.RemoveHandler(commandName);
        Svc.Commands.RemoveHandler(commandActivate);
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        OpenConfig();
    }

    private void OnActiveCommand(string command, string args)
    {
        config.isActived = !config.isActived;
        configGUI.isActived = config.isActived;
        config.Save();
        Svc.Toasts.ShowQuest("AutoEmotion " + (config.isActived ? "Enabled." : "Disabled."),
    new QuestToastOptions() { PlaySound = true, DisplayCheckmark = true });
    }

    private void OpenConfig() => isOpenConfig = true;
}
