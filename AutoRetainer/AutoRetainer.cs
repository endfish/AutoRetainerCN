using AutoRetainer.Internal;
using AutoRetainer.Internal.InventoryManagement;
using AutoRetainer.Modules.Statistics;
using AutoRetainer.Modules.Voyage;
using AutoRetainer.Scheduler.Handlers;
using AutoRetainer.Scheduler.Tasks;
using AutoRetainer.Services;
using AutoRetainer.UI.MainWindow;
using AutoRetainer.UI.Overlays;
using AutoRetainer.UI.Windows;
using AutoRetainerAPI;
using AutoRetainerAPI.Configuration;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.Configuration;
using ECommons.Events;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.EzSharedDataManager;
using ECommons.GameHelpers;
using ECommons.Reflection;
using ECommons.Singletons;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using NotificationMasterAPI;
using PunishLib;
using System.Diagnostics;
using Action = System.Action;
using LoginOverlay = AutoRetainer.UI.Overlays.LoginOverlay;

namespace AutoRetainer;

public unsafe class AutoRetainer : IDalamudPlugin
{
    public string Name => "AutoRetainerCN";
    internal static AutoRetainer P;
    internal static Config C => P.config;
    private Config config;
    internal WindowSystem WindowSystem;
    internal AutoRetainerWindow AutoRetainerWindow;
    internal bool IsInteractionAutomatic = false;
    internal QuickSellItems quickSellItems;
    internal TaskManager TaskManager;
    internal TaskManager ODMTaskManager;
    internal Memory Memory;
    internal bool WasEnabled = false;
    internal bool IsCloseActionAutomatic = false;
    internal long LastMovementAt;
    internal Vector3 LastPosition;
    internal bool IsNextToBell;
    internal bool ConditionWasEnabled = false;
    internal VenturePlanner VenturePlanner;
    internal VentureBrowser VentureBrowser;
    internal LogWindow LogWindow;
    internal AutoRetainerApi API;
    internal LoginOverlay LoginOverlay;
    internal MarketCooldownOverlay MarketCooldownOverlay;
    internal SubmarineUnlockPlanUI SubmarineUnlockPlanUI;
    internal SubmarinePointPlanUI SubmarinePointPlanUI;

    internal long Time => C.UseServerTime ? CSFramework.GetServerTime() : DateTimeOffset.Now.ToUnixTimeSeconds();

    internal RetainerListOverlay RetainerListOverlay;
    internal uint LastVentureID = 0;
    internal uint ListUpdateFrame = 0;

    internal bool LogOpcodes = false;
    internal int LastLoadedItems = 0;
    internal NotificationMasterApi NotificationMasterApi;
    internal long[] TimeLaunched;
    internal ContextMenuManager ContextMenuManager;
    public bool ReadOnly = false;

    internal static OfflineCharacterData Data => Utils.GetCurrentCharacterData();

    public AutoRetainer(IDalamudPluginInterface pi)
    {
        //PluginLoader.CheckAndLoad(pi, "https://love.puni.sh/plugins/AutoRetainer/blacklist.txt", delegate
        {
            P = this;
            ECommonsMain.Init(pi, this, Module.DalamudReflector);
#if CUSTOMCS
            PluginLog.Warning($"Using custom FFXIVClientStructs");
            var gameVersion = DalamudReflector.TryGetDalamudStartInfo(out var ver) ? ver.GameVersion.ToString() : "unknown";
            InteropGenerator.Runtime.Resolver.GetInstance.Setup(Svc.SigScanner.SearchBase, gameVersion, new(Svc.PluginInterface.ConfigDirectory.FullName + "/cs.json"));
            FFXIVClientStructs.Interop.Generated.Addresses.Register();
            InteropGenerator.Runtime.Resolver.GetInstance.Resolve();
#endif
            PunishLibMain.Init(pi, Name, PunishOption.DefaultKoFi); // Default button
            var cnt = FFXIVInstanceMonitor.GetFFXIVCNT();
            PluginLog.Information($"FFXIV instances: {cnt}");
            if(FFXIVInstanceMonitor.AcquireLock() || cnt <= 1)
            {
                new TickScheduler(Load);
            }
            else
            {
                var shouldCreateWindow = !EzConfig.LoadConfiguration<Config>(EzConfig.DefaultSerializationFactory.DefaultConfigFileName).No2ndInstanceNotify;
                if(shouldCreateWindow)
                {
                    new SingletonNotifyWindow();
                }
                else
                {
                    for(var i = 0; i < 100; i++)
                    {
                        PluginLog.Fatal($"AutoRetainerCN 的加载已跳过：检测到同一数据目录下存在第二个游戏实例，且已启用“不提示同目录第二游戏实例”选项。");
                    }
                }
            }
        }
        //);
    }

    internal void SetConfig(Config c)
    {
        config = EzConfig.Set<Config>(c);
    }

    public void Load()
    {
        EzConfig.Migrate<Config>();
        config = EzConfig.Init<Config>();

        //windows
        WindowSystem = new();
        VenturePlanner = new();
        VentureBrowser = new();
        LogWindow = new();
        AutoRetainerWindow = new();
        MarketCooldownOverlay = new();
        new MultiModeOverlay();
        RetainerListOverlay = new();
        LoginOverlay = new LoginOverlay();
        SubmarineUnlockPlanUI = new();
        SubmarinePointPlanUI = new();

        TaskManager = new(new(abortOnTimeout: true, timeLimitMS: 20000, showDebug: true));
        Memory = new();
        Svc.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi += () =>
        {
            AutoRetainerWindow.IsOpen = true;
        };
        Svc.PluginInterface.UiBuilder.OpenConfigUi += () =>
        {
            S.NeoWindow.IsOpen = true;
        };
        Svc.ClientState.Logout += Logout;
        Svc.Condition.ConditionChange += ConditionChange;
        EzCmd.Add("/autoretainer", CommandHandler, """
            打开 AutoRetainerCN 主界面
            /ays - /autoretainer 的别名
            /autoretainer e|enable - 启用插件
            /autoretainer d|disable - 禁用插件
            /autoretainer t|toggle - 切换插件启用状态
            /autoretainer m|multi - 切换多角色模式
            /autoretainer relog 角色名@服务器名 - 切换到已配置的目标角色
            /autoretainer b|browser - 打开雇员探险浏览器
            /autoretainer expert - 切换专家设置
            /autoretainer debug - 切换调试菜单和详细输出
            /autoretainer shutdown <小时> [分钟] [秒] - 设置游戏关闭倒计时
            /autoretainer itemsell - 开始向 NPC 或雇员出售物品
            /autoretainer het - 尝试进入附近自己的房屋或公寓
            /autoretainer reset - 重置所有待处理任务
            /autoretainer deliver - 交纳筹备品
            """);
        EzCmd.Add("/autoretainercn", CommandHandler);
        EzCmd.Add("/ays", CommandHandler);
        Svc.Toasts.ErrorToast += Toasts_ErrorToast;
        Svc.Toasts.Toast += Toasts_Toast;
        Svc.Framework.Update += Tick;
        quickSellItems = new();
        StatisticsManager.Init();
        AutoGCHandin.Init();
        IPC.Init();
        VoyageMain.Init();

        MultiMode.Init();
        NotificationMasterApi = new(Svc.PluginInterface);
        ODMTaskManager = new(new(timeLimitMS: 60 * 1000, abortOnTimeout: true, showDebug: true));

        Safety.Check();

        API = new();
        ApiTest.Init();
        FPSManager.UnlockChillFrames();
        Utils.ResetEscIgnoreByWindows();
        Svc.PluginInterface.UiBuilder.Draw += FPSLimiter.FPSLimit;
        AutoCutsceneSkipper.Init(MiniTA.ProcessCutsceneSkip);
        EzSharedData.TryGet("AutoRetainer.Started", out TimeLaunched, CreationMode.CreateAndKeep, [DateTimeOffset.Now.ToUnixTimeMilliseconds()]);
        if(!C.NightModePersistent) C.NightMode = false;
        ContextMenuManager = new();
        PluginLog.Information($"AutoRetainerCN v{P.GetType().Assembly.GetName().Version} is ready.");
        if(!EzSharedData.TryGet<object>("AutoRetainer.WasLoaded", out _))
        {
            if(C.MultiAutoStart || C.AutoLogin != "")
            {
                MultiMode.PerformAutoStart();
            }
            if(C.DisplayOnStart)
            {
                AutoRetainerWindow.IsOpen = true;
            }
        }
        SingletonServiceManager.Initialize(typeof(AutoRetainerServiceManager));


        if(C.MultiOnPluginLoad)
        {
            if(C.MultiModeOnPluginLoadDelay > 0)
            {
                var n = Svc.NotificationManager.AddNotification(new Notification()
                {
                    UserDismissable = false,
                    Minimized = false,
                    HardExpiry = DateTime.Now + TimeSpan.FromSeconds(C.MultiModeOnPluginLoadDelay),
                    InitialDuration = TimeSpan.FromSeconds(C.MultiModeOnPluginLoadDelay),
                    Title = "AutoRetainerCN 启动",
                    Content = $"多角色模式将在 {C.MultiModeOnPluginLoadDelay} 秒后启用。点击此处取消。"
                });
                TaskManager.EnqueueDelay(C.MultiModeOnPluginLoadDelay * 1000);
                TaskManager.Enqueue((Action)(() => MultiMode.Enabled = true));
                n.Click += delegate 
                {
                    TaskManager.Abort(); 
                    n.DismissNow(); 
                };
            }
            else
            {
                MultiMode.Enabled = true;
            }
        }
    }

    private void Toasts_Toast(ref SeString message, ref ToastOptions options, ref bool isHandled)
    {
        if(Svc.Condition[ConditionFlag.OccupiedSummoningBell] && ProperOnLogin.PlayerPresent)
        {
            var text = message.GetText().Cleanup();
            //4330	57	33	0	False	リテイナーベンチャー「<Value>IntegerParameter(2)</Value> <Sheet(Item,IntegerParameter(1),0)/>」を依頼しました。
            //4330	57	33	0	False	Du hast deinen Gehilfen mit der Beschaffung von <SheetDe(Item,1,IntegerParameter(1),IntegerParameter(3),3,1)/> ( <Value>IntegerParameter(2)</Value>) beauftragt.
            //4330	57	33	0	False	Vous avez confié la tâche “<SheetFr(Item,12,IntegerParameter(1),2,1)/> ( <Value>IntegerParameter(2)</Value>)” à votre servant.
            if(text.StartsWithAny("You assign your retainer".Cleanup(), "リテイナーベンチャー".Cleanup(), "Du hast deinen Gehilfen mit".Cleanup(), "Vous avez confié la tâche".Cleanup())
                && Utils.TryGetCurrentRetainer(out var ret)
                && C.OfflineData.TryGetFirst(x => x.CID == Svc.ClientState.LocalContentId, out var offlineData)
                && offlineData.RetainerData.TryGetFirst(x => x.Name == ret, out var offlineRetainerData))
            {
                offlineRetainerData.VentureBeginsAt = P.Time;
                DebugLog($"Recorded venture start time = {offlineRetainerData.VentureBeginsAt}");
            }
            //4578	57	33	0	False	Gil earned from market sales has been entrusted to your retainer.<If(Equal(IntegerParameter(1),1))>
            //The amount earned exceeded your retainer's gil limit. Excess gil has been discarded.<Else/></If>
            if(text.StartsWith(Svc.Data.GetExcelSheet<LogMessage>().GetRow(4578).Text.GetText(true).Cleanup()))
            {
                TaskWithdrawGil.forceCheck = true;
                DebugLog($"Forcing to check for gil");
            }
        }
    }

    private void CommandHandler(string command, string arguments)
    {
        if(arguments.EqualsIgnoreCase("debug"))
        {
            config.Verbose = !config.Verbose;
            DuoLog.Information($"调试模式已{(config.Verbose ? "启用" : "禁用")}");
            S.NeoWindow.Reload();
        }
        else if(arguments.EqualsIgnoreCaseAny("e", "enable"))
        {
            SchedulerMain.EnablePlugin(PluginEnableReason.Auto);
        }
        else if(arguments.EqualsIgnoreCaseAny("d", "disable"))
        {
            SchedulerMain.DisablePlugin();
        }
        else if(arguments.EqualsIgnoreCaseAny("t", "toggle"))
        {
            Svc.Commands.ProcessCommand(SchedulerMain.PluginEnabled ? "/ays d" : "/ays e");
        }
        else if(arguments.EqualsIgnoreCaseAny("m", "multi"))
        {
            MultiMode.Enabled = !MultiMode.Enabled;
            MultiMode.OnMultiModeEnabled();
        }
        else if(arguments.StartsWithAny(StringComparison.OrdinalIgnoreCase, "m ", "multi "))
        {
            var arg2 = arguments.Split(" ")[1];
            if(arg2.EqualsIgnoreCaseAny("d", "disable"))
            {
                if(MultiMode.Enabled) MultiMode.Enabled = false;
            }
            else if(arg2.EqualsIgnoreCaseAny("e", "enable"))
            {
                if(!MultiMode.Enabled)
                {
                    MultiMode.Enabled = true;
                    MultiMode.OnMultiModeEnabled();
                }
            }
        }
        else if(arguments.EqualsIgnoreCaseAny("n", "night"))
        {
            C.NightMode = !C.NightMode;
            DuoLog.Information($"夜间模式已{(C.NightMode ? "启用" : "禁用")}");
            if(C.NightMode)
            {
                if(!MultiMode.Enabled)
                {
                    MultiMode.Enabled = true;
                    MultiMode.OnMultiModeEnabled();
                }
            }
        }
        else if(arguments.StartsWithAny(StringComparison.OrdinalIgnoreCase, "n ", "night "))
        {
            var arg2 = arguments.Split(" ")[1];
            if(arg2.EqualsIgnoreCaseAny("d", "disable"))
            {
                C.NightMode = false;
            }
            else if(arg2.EqualsIgnoreCaseAny("e", "enable"))
            {
                C.NightMode = true;
                if(!MultiMode.Enabled)
                {
                    MultiMode.Enabled = true;
                    MultiMode.OnMultiModeEnabled();
                }
            }
            else if(arg2.EqualsIgnoreCaseAny("s", "set"))
            {
                C.NightMode = true;
            }
            DuoLog.Information($"夜间模式已{(C.NightMode ? "启用" : "禁用")}");
        }
        else if(arguments.EqualsIgnoreCaseAny("s", "settings"))
        {
            S.NeoWindow.IsOpen = true;
        }
        else if(arguments.EqualsIgnoreCaseAny("b", "browser"))
        {
            VentureBrowser.IsOpen = !VentureBrowser.IsOpen;
        }
        else if(arguments.EqualsIgnoreCaseAny("l", "log"))
        {
            LogWindow.IsOpen = !LogWindow.IsOpen;
        }
        else if(arguments.StartsWith("relog "))
        {
            var target = C.OfflineData.Where(x => $"{x.Name}@{x.World}" == arguments[6..]).FirstOrDefault();
            if(target != null)
            {
                MultiMode.Relog(target, out _, RelogReason.Command);
            }
            else
            {
                Notify.Error($"未找到目标角色");
            }
        }
        else if(arguments.EqualsIgnoreCase("het"))
        {
            TaskNeoHET.Enqueue(() => DuoLog.Error("未能找到合适的房屋"));
        }
        else if(arguments.EqualsIgnoreCase("wet"))
        {
            if(TaskNeoHET.GetWorkshopEntrance() != null)
            {
                TaskNeoHET.TryEnterWorkshop(() => DuoLog.Error("未能进入部队工房"));
            }
            else
            {
                TaskNeoHET.Enqueue(() => DuoLog.Error("未能找到合适的房屋"), true);
            }
        }
        else if(arguments.EqualsIgnoreCaseAny("itemsell"))
        {
            if((!IsOccupied() || TryGetAddonByName<AtkUnitBase>("Shop", out _)) && !P.TaskManager.IsBusy)
            {
                TaskVendorItems.EnqueueFromCommand();
            }
            else
            {
                DuoLog.Error($"未找到可用的房屋 NPC 或雇员铃，或 AutoRetainerCN 正忙，或出售功能已禁用");
            }
        }
        else if(arguments.StartsWith("shutdown"))
        {
            var str = arguments.Split((char[])[' ', ',', ':', '-', '/', '.'], StringSplitOptions.RemoveEmptyEntries);
            if(str.Length <= 1)
            {
                Shutdown.ShutdownAt = 0;
                Shutdown.ForceShutdownAt = 0;
                Svc.Chat.Print("已清除关闭倒计时");
            }
            else
            {
                try
                {
                    var time = new TimeSpan();
                    time = time.Add(TimeSpan.FromHours(int.Parse(str[1])));
                    if(str.Length > 2) time = time.Add(TimeSpan.FromMinutes(int.Parse(str[2])));
                    if(str.Length > 3) time = time.Add(TimeSpan.FromSeconds(int.Parse(str[3])));
                    if(time.TotalSeconds < 10)
                    {
                        DuoLog.Error("倒计时不能少于 10 秒");
                    }
                    else
                    {
                        Svc.Chat.Print($"将在 {time} 后关闭游戏");
                        Shutdown.ShutdownAt = Environment.TickCount64 + (long)time.TotalMilliseconds;
                        Shutdown.ForceShutdownAt = Environment.TickCount64 + (long)time.TotalMilliseconds + 10 * 60 * 1000;
                    }
                }
                catch(Exception e)
                {
                    DuoLog.Error($"{e.Message}");
                    PluginLog.Error($"{e.StackTrace}");
                }
            }
        }
        else if(arguments.StartsWith("modifySoftVendorList"))
        {
            var s = C.DefaultIMSettings;
            if(s != null && int.TryParse(arguments.Split(" ")[1], out var num))
            {
                if(num > 0)
                {
                    var id = (uint)num;
                    if(!s.IMAutoVendorSoft.Contains(id))
                    {
                        s.IMAutoVendorSoft.Add(id);
                        PluginLog.Warning($"External addition to soft vendor list: {ExcelItemHelper.GetName(id)}");
                    }
                }
                else if(num < 0)
                {
                    var id = (uint)-num;
                    if(s.IMAutoVendorSoft.Contains(id))
                    {
                        s.IMAutoVendorSoft.Remove(id);
                        PluginLog.Warning($"External removal from soft vendor list: {ExcelItemHelper.GetName(id)}");
                    }
                }
            }
        }
        else if(arguments.EqualsIgnoreCase("reset"))
        {
            P.TaskManager.Abort();
            SchedulerMain.CharacterPostProcessLocked = false;
            Notify.Success("重置完成");
        }
        else if(arguments.EqualsIgnoreCase("deliver"))
        {
            if(!P.TaskManager.IsBusy)
            {
                TaskDeliverItems.Enqueue();
            }
        }
        else if(arguments.EqualsIgnoreCase("discard"))
        {
            if(!P.TaskManager.IsBusy)
            {
                TaskRecursiveItemDiscard.EnqueueIfNeeded();
            }
        }
        else if(arguments.StartsWith("set"))
        {
            try
            {
                var field = arguments.Split(" ")[1];
                var value = arguments.Split(" ")[2];
                DuoLog.Information($"Attempting to set {field}={value}");
                if(C.GetFoP(field).GetType() == typeof(bool))
                {
                    C.SetFoP(field, bool.Parse(value));
                    DuoLog.Information($"Set bool {field}={value}");
                }
                else if(C.GetFoP(field).GetType() == typeof(int))
                {
                    C.SetFoP(field, int.Parse(value));
                    DuoLog.Information($"Set int {field}={value}");
                }
                else if(C.GetFoP(field).GetType() == typeof(uint))
                {
                    C.SetFoP(field, uint.Parse(value));
                    DuoLog.Information($"Set uint {field}={value}");
                }
                else if(C.GetFoP(field).GetType() == typeof(float))
                {
                    C.SetFoP(field, float.Parse(value));
                    DuoLog.Information($"Set float {field}={value}");
                }
                else if(C.GetFoP(field).GetType() == typeof(double))
                {
                    C.SetFoP(field, double.Parse(value));
                    DuoLog.Information($"Set double {field}={value}");
                }
                else if(C.GetFoP(field).GetType() == typeof(nint))
                {
                    C.SetFoP(field, nint.Parse(value));
                    DuoLog.Information($"Set nint {field}={value}");
                }
                else if(C.GetFoP(field).GetType() == typeof(long))
                {
                    C.SetFoP(field, long.Parse(value));
                    DuoLog.Information($"Set long {field}={value}");
                }
                else if(C.GetFoP(field).GetType() == typeof(ulong))
                {
                    C.SetFoP(field, ulong.Parse(value));
                    DuoLog.Information($"Set ulong {field}={value}");
                }
                else if(C.GetFoP(field).GetType() == typeof(string))
                {
                    C.SetFoP(field, value);
                    DuoLog.Information($"Set string {field}={value}");
                }
                else if(C.GetFoP(field).GetType().IsEnum)
                {
                    C.SetFoP(field, int.Parse(value));
                    DuoLog.Information($"Set enum {field}={value}");
                }
            }
            catch(Exception e)
            {
                e.LogDuo();
            }
        }
        else
        {
            AutoRetainerWindow.IsOpen = !AutoRetainerWindow.IsOpen;
        }
    }

    private void Tick(object _)
    {
        if(!IPC.Suppressed)
        {
            if(SchedulerMain.PluginEnabled && Svc.ClientState.LocalPlayer != null)
            {
                SchedulerMain.Tick();
                if(!C.SelectedRetainers.ContainsKey(Svc.ClientState.LocalContentId))
                {
                    C.SelectedRetainers[Svc.ClientState.LocalContentId] = [];
                }
            }
        }
        MiniTA.Tick();
        OfflineDataManager.Tick();
        AutoGCHandin.Tick();
        MultiMode.Tick();
        NotificationHandler.Tick();
        NewYesAlreadyManager.Tick();
        Artisan.ArtisanTick();
        FPSManager.Tick();
        PriorityManager.Tick();
        TextAdvanceManager.Tick();
        Shutdown.Tick();
        BailoutManager.Tick();
        if(Svc.Condition[ConditionFlag.OccupiedSummoningBell] && Utils.TryGetCurrentRetainer(out var name) && Utils.TryGetRetainerByName(name, out var retainer))
        {
            if(!retainer.VentureID.EqualsAny(0u, LastVentureID))
            {
                LastVentureID = retainer.VentureID;
                DebugLog($"Retainer {retainer.Name} current venture={LastVentureID}");
            }
        }
        else
        {
            if(LastVentureID != 0)
            {
                LastVentureID = 0;
                DebugLog($"Last venture ID reset");
            }
        }
        //if(C.RetryItemSearch) RetryItemSearch.Tick();
        if(SchedulerMain.PluginEnabled || MultiMode.Enabled || TaskManager.IsBusy)
        {
            if(EzThrottler.Throttle("CheckHTweaks"))
            {
                Utils.EnsureEnhancedLoginIsOff();
            }
            if(Svc.ClientState.TerritoryType == Prisons.Mordion_Gaol)
            {
                Process.GetCurrentProcess().Kill();
            }
            if(Svc.Condition[ConditionFlag.OccupiedSummoningBell])
            {
                if(!ConditionWasEnabled)
                {
                    ConditionWasEnabled = true;
                    DebugLog($"ConditionWasEnabled = true");
                }
            }
        }
        IsNextToBell = false;
        if(C.RetainerSense && Svc.ClientState.LocalPlayer != null && Svc.ClientState.LocalPlayer.HomeWorld.RowId == Svc.ClientState.LocalPlayer.CurrentWorld.RowId)
        {
            if(!IPC.Suppressed && !IsOccupied() && !C.OldRetainerSense && !TaskManager.IsBusy && !Utils.MultiModeOrArtisan && !Svc.Condition[ConditionFlag.InCombat] && !Svc.Condition[ConditionFlag.BoundByDuty] && Utils.IsAnyRetainersCompletedVenture())
            {
                var bell = Utils.GetReachableRetainerBell(true);
                if(bell == null || LastPosition != Svc.ClientState.LocalPlayer.Position)
                {
                    LastPosition = Svc.ClientState.LocalPlayer.Position;
                    LastMovementAt = Environment.TickCount64;
                }
                if(bell != null)
                {
                    IsNextToBell = true;
                }
                if(Environment.TickCount64 - LastMovementAt > C.RetainerSenseThreshold)
                {
                    if(bell != null)
                    {
                        IsNextToBell = true;
                        if(EzThrottler.Throttle("RetainerSense", 30000))
                        {
                            TaskInteractWithNearestBell.Enqueue();
                            TaskManager.Enqueue(() => { SchedulerMain.EnablePlugin(PluginEnableReason.Auto); return true; });
                        }
                    }
                }
            }
        }
        if(Utils.IsBusy && TryGetAddonByName<AtkUnitBase>("Trade", out var trade))
        {
            Callback.Fire(trade, true, -1);
        }
    }

    private void Toasts_ErrorToast(ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        if(!Svc.ClientState.IsLoggedIn)
        {
            //5800	60	8	0	False	Unable to execute command. Character is currently visiting the <Highlight>StringParameter(1)</Highlight> data center.
            //5800	60	8	0	False	他のデータセンター<Highlight>StringParameter(1)</Highlight>へ遊びに行っているため操作できません。
            //5800	60	8	0	False	Der Vorgang kann nicht ausgeführt werden, da der Charakter gerade das Datenzentrum <Highlight>StringParameter(1)</Highlight> bereist.
            //5800	60	8	0	False	Impossible d'exécuter cette commande. Le personnage se trouve dans un autre centre de traitement de données (<Highlight>StringParameter(1)</Highlight>).
            if(message.ToString().StartsWithAny(Lang.UnableToVisitWorld))
            {

                MultiMode.Enabled = false;
            }
        }
    }

    public void Dispose()
    {
        //if (PluginLoader.IsLoaded)
        {
            Safe(() => FFXIVInstanceMonitor.ReleaseLock());
            Safe(() => quickSellItems.Disable());
            Safe(() => quickSellItems.Dispose());
            Safe(() => Svc.PluginInterface.UiBuilder.Draw -= FPSLimiter.FPSLimit);
            Safe(() => Svc.PluginInterface.UiBuilder.Draw -= WindowSystem.Draw);
            Safe(() => Svc.ClientState.Logout -= Logout);
            Safe(() => Svc.Condition.ConditionChange -= ConditionChange);
            Safe(() => Svc.Framework.Update -= Tick);
            Safe(() => Svc.Toasts.ErrorToast -= Toasts_ErrorToast);
            Safe(() => Svc.Toasts.Toast -= Toasts_Toast);
            Safe(() => NewYesAlreadyManager.Unlock());
            Safe(() => TextAdvanceManager.UnlockTA());
            Safe(() => StatisticsManager.Shutdown());
            Safe(() => Memory.Dispose());
            Safe(() => IPC.Shutdown());
            Safe(() => API.Dispose());
            Safe(() => FPSManager.ForceRestore());
            Safe(() => PriorityManager.RestorePriority());
            Safe(() => VoyageMain.Shutdown());
            Safe(() => ContextMenuManager.Dispose());
            PunishLibMain.Dispose();
            ECommonsMain.Dispose();
        }
        //PluginLoader.Dispose();
    }

    private void AddVenture(string name, uint ventureId)
    {
        if(API.Ready && API.GetOfflineCharacterData(Player.CID).RetainerData.TryGetFirst(x => x.Name == name, out var rdata))
        {
            var adata = API.GetAdditionalRetainerData(Player.CID, rdata.Name);
            if(adata.VenturePlan.List.TryGetFirst(x => x.ID == ventureId, out var v))
            {
                v.Num += 1;
            }
            else
            {
                adata.VenturePlan.List.Add(new(ventureId, 1));
            }
            API.WriteAdditionalRetainerData(Player.CID, rdata.Name, adata);
        }
    }

    private IEnumerable<string> ListRetainers()
    {
        if(API.Ready)
        {
            foreach(var x in API.GetOfflineCharacterData(Player.CID).RetainerData)
            {
                yield return x.Name;
            }
        }
    }

    internal HashSet<string> GetSelectedRetainers(ulong cid)
    {
        if(!config.SelectedRetainers.ContainsKey(cid))
        {
            config.SelectedRetainers.Add(cid, []);
        }
        return config.SelectedRetainers[cid];
    }

    internal static string LastLogMsg = string.Empty;
    internal static void DebugLog(string message)
    {
        //if (LastLogMsg != message)
        {
            PluginLog.Debug(message);
        }
    }

    public bool SkipNextEnable = false;

    private void ConditionChange(ConditionFlag flag, bool value)
    {
        if(flag == ConditionFlag.LoggingOut && value)
        {
            if(Player.Available)
            {
                PluginLog.Verbose($"Writing logout offline data...");
                OfflineDataManager.WriteOfflineData(true, true);
            }
        }
        if(flag == ConditionFlag.OccupiedSummoningBell)
        {
            OfflineDataManager.WriteOfflineData(true, true);
            if(!value)
            {
                ConditionWasEnabled = false;
                DebugLog("ConditionWasEnabled = false;");
            }
            if(!SkipNextEnable)
            {
                if(Svc.Targets.Target.IsRetainerBell())
                {
                    if(value)
                    {
                        if(Utils.MultiModeOrArtisan)
                        {
                            WasEnabled = false;
                            if(IsInteractionAutomatic)
                            {
                                IsInteractionAutomatic = false;
                                SchedulerMain.EnablePlugin(MultiMode.Enabled ? PluginEnableReason.MultiMode : PluginEnableReason.Artisan);
                            }
                        }
                        else
                        {
                            var bellBehavior = Utils.IsAnyRetainersCompletedVenture() ? C.OpenBellBehaviorWithVentures : C.OpenBellBehaviorNoVentures;
                            if(bellBehavior != OpenBellBehavior.Pause_AutoRetainer && IsKeyPressed(C.Suppress) && !CSFramework.Instance()->WindowInactive)
                            {
                                bellBehavior = OpenBellBehavior.Do_nothing;
                                Notify.Info($"已取消打开雇员铃时的自动操作");
                            }
                            if(SchedulerMain.PluginEnabled && bellBehavior == OpenBellBehavior.Pause_AutoRetainer)
                            {
                                WasEnabled = true;
                                SchedulerMain.DisablePlugin();
                            }
                            if(IsInteractionAutomatic)
                            {
                                IsInteractionAutomatic = false;
                                SchedulerMain.EnablePlugin(PluginEnableReason.Auto);
                            }
                            else
                            {
                                if(bellBehavior == OpenBellBehavior.Enable_AutoRetainer)
                                {
                                    SchedulerMain.EnablePlugin(PluginEnableReason.Access);
                                }
                                else if(bellBehavior == OpenBellBehavior.Disable_AutoRetainer)
                                {
                                    SchedulerMain.DisablePlugin();
                                }
                            }
                        }
                    }
                }
                else
                {
                    if(Svc.Targets.Target.IsRetainerBell() || Svc.Targets.PreviousTarget.IsRetainerBell())
                    {
                        if(WasEnabled)
                        {
                            DebugLog($"Enabling plugin because WasEnabled is true");
                            SchedulerMain.EnablePlugin(PluginEnableReason.Auto);
                            WasEnabled = false;
                        }
                        else if(!IsCloseActionAutomatic && C.AutoDisable && !Utils.MultiModeOrArtisan)
                        {
                            DebugLog($"Disabling plugin because AutoDisable is on");
                            SchedulerMain.DisablePlugin();
                        }
                    }
                }
            }
            SkipNextEnable = false;
            IsCloseActionAutomatic = false;
        }
        if(flag == ConditionFlag.Gathering)
        {
            VentureBrowser.Reset();
            OfflineDataManager.WriteOfflineData(true, true);
        }
    }

    private void Logout(int _, int __)
    {
        SchedulerMain.DisablePlugin();

        if(!P.TaskManager.IsBusy)
        {
            MultiMode.LastLogin = 0;
        }

    }
}
