using AutoRetainer.Modules.Voyage;
using Dalamud.Game;
using ECommons.GameHelpers;
using ECommons.Reflection;

namespace AutoRetainer.UI.MainWindow;
public static unsafe class TroubleshootingUI
{
    private static readonly Config EmptyConfig = new();

    public static bool IsPluginInstalled(string name)
    {
        return Svc.PluginInterface.InstalledPlugins.Any(x => x.IsLoaded && (x.InternalName.EqualsIgnoreCase(name) || x.Name.EqualsIgnoreCase(name)));
    }

    public static void Draw()
    {
        ImGuiEx.TextWrapped("此标签页会检查配置中常见、可自行处理的问题，方便你在寻求支持前先排查。");

        if(!Player.Available)
        {
            ImGuiEx.TextWrapped($"未登录时无法进行故障排查。");
            return;
        }

        if(Data == null)
        {
            ImGuiEx.TextWrapped($"当前角色没有可用数据。访问雇员铃、潜艇/飞空艇面板或登出后可创建数据。");
            return;
        }

        if(!Svc.ClientState.ClientLanguage.EqualsAny(ClientLanguage.Japanese, ClientLanguage.German, ClientLanguage.French, ClientLanguage.English))
        {
            Error($"检测到本地代理客户端。AutoRetainerCN 尚未完整验证所有本地代理客户端环境，部分或全部功能可能无法正常工作。");
        }

        if(C.DontLogout)
        {
            Error("DontLogout 调试选项已启用");
        }

        foreach(var x in C.OfflineData)
        {
            if(x.WorkshopEnabled)
            {
                var a = x.OfflineSubmarineData.Select(x => x.Name);
                if(a.Count() > a.Distinct().Count())
                {
                    Error($"角色 {Censor.Character(x.Name, x.World)} 存在重复的潜水艇名称。潜水艇名称必须唯一。");
                }
            }
        }

        if((C.GlobalTeleportOptions.Enabled || C.OfflineData.Any(x => x.TeleportOptionsOverride.Enabled == true)) && !Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "Lifestream" && x.IsLoaded))
        {
            Error("已启用传送，但 Lifestream 插件未安装或未加载。AutoRetainerCN 无法在此配置下工作。请禁用传送，或安装并加载 Lifestream。");
        }

        foreach(var x in C.SubmarineUnlockPlans)
        {
            if(x.EnforcePlan)
            {
                Info($"潜水艇解锁计划 {x.Name.NullWhenEmpty() ?? x.GUID} 已设为强制使用；只要还有内容可解锁，它会覆盖所有潜水艇设置。");
            }
        }

        foreach(var x in C.SubmarineUnlockPlans)
        {
            if(x.EnforceDSSSinglePoint)
            {
                Info($"潜水艇解锁计划 {x.Name.NullWhenEmpty() ?? x.GUID} 已设为在深海站点单点派遣，并会忽略手动设置的解锁行为。");
            }
        }

        try
        {
            if(DalamudReflector.IsOnStaging())
            {
                Error($"检测到非 release 的 Dalamud 分支。这可能导致问题。如可行，请输入 /xlbranch 打开分支切换器，切换到“release”后重启游戏。");
            }
        }
        catch(Exception e)
        {
        }

        if(Player.Available)
        {
            if(Player.CurrentWorld != Player.HomeWorld)
            {
                Error("你正在访问其他世界。AutoRetainerCN 继续处理此角色前，你必须返回原始世界。");
            }
            if(C.Blacklist.Any(x => x.CID == Player.CID))
            {
                Error("当前角色已被 AutoRetainerCN 完全排除，不会以任何方式处理。可前往设置 - 排除项修改。");
            }
            if(Data.ExcludeRetainer)
            {
                Error("当前角色已从雇员列表中排除。可前往设置 - 排除项修改。");
            }
            if(Data.ExcludeWorkshop)
            {
                Error("当前角色已从潜艇/飞空艇列表中排除。可前往设置 - 排除项修改。");
            }
        }

        {
            var list = C.OfflineData.Where(x => x.GetAreTeleportSettingsOverriden());
            if(list.Any())
            {
                Info("部分角色使用了自定义传送选项。悬停查看列表。", list.Select(x => $"{x.Name}@{x.World}").Print("\n"));
            }
        }

        if(C.NoTeleportHetWhenNextToBell)
        {
            Warning("角色在雇员铃附近时，传送或进入房屋/公寓已禁用。请注意房屋拆除倒计时。");
        }



        if(C.AllowSimpleTeleport)
        {
            Warning("已启用简单传送选项。它没有通过 Lifestream 登记房屋可靠。若遇到传送问题，建议禁用此选项并在 Lifestream 中登记房产。");
        }

        if(!C.EnableEntrustManager && C.AdditionalData.Any(x => x.Value.EntrustPlan != Guid.Empty))
        {
            Warning($"委托管理器已全局禁用，但部分雇员仍分配了委托计划。这些委托计划只会被手动处理。");
        }

        if(C.ExtraDebug)
        {
            Info("额外日志选项已启用。它会产生大量日志，只建议在收集调试信息时使用。");
        }

        if(C.UnsyncCompensation > -5)
        {
            Warning("时间不同步补偿设置过高（>-5）。这可能导致问题。");
        }

        if(UIUtils.GetFPSFromMSPT(C.TargetMSPTIdle) < 10)
        {
            Warning("空闲时目标帧率设置过低（<10）。这可能导致问题。");
        }

        if(UIUtils.GetFPSFromMSPT(C.TargetMSPTRunning) < 20)
        {
            Warning("操作时目标帧率设置过低（<20）。这可能导致问题。");
        }

        if(Data?.GetIMSettings().AllowSellFromArmory == true)
        {
            Info("已启用从兵装库出售物品。请确认已将零式装备和绝本武器加入保护列表。");
        }

        {
            var list = C.OfflineData.Where(x => !x.ExcludeRetainer && !x.Enabled && x.RetainerData.Count > 0);
            if(list.Any())
            {
                Warning($"部分拥有雇员的角色未启用雇员多角色模式。悬停查看列表。", list.Print("\n"));
            }
        }
        {
            var list = C.OfflineData.Where(x => !x.ExcludeRetainer && x.Enabled && x.RetainerData.Count > 0 && C.SelectedRetainers.TryGetValue(x.CID, out var rd) && !x.RetainerData.All(r => rd.Contains(r.Name)));
            if(list.Any())
            {
                Warning($"部分角色并未启用所有雇员进行处理。悬停查看列表。", list.Print("\n"));
            }
        }
        {
            var list = C.OfflineData.Where(x => !x.ExcludeWorkshop && !x.WorkshopEnabled && (x.OfflineSubmarineData.Count + x.OfflineAirshipData.Count) > 0);
            if(list.Any())
            {
                Warning($"部分已登记潜艇/飞空艇的角色未启用潜艇/飞空艇多角色模式。悬停查看列表。", list.Print("\n"));
            }
        }

        {
            var list = C.OfflineData.Where(x => !x.ExcludeWorkshop && x.WorkshopEnabled && x.GetEnabledVesselsData(Internal.VoyageType.Airship).Count + x.GetEnabledVesselsData(Internal.VoyageType.Submersible).Count < Math.Min(x.OfflineAirshipData.Count + x.OfflineSubmarineData.Count, 4));
            if(list.Any())
            {
                Warning($"部分角色并未启用所有潜艇/飞空艇进行处理。悬停查看列表。", list.Print("\n"));
            }
        }

        if(C.MultiModeType != AutoRetainerAPI.Configuration.MultiModeType.Everything)
        {
            Warning($"你的多角色模式类型设为 {C.MultiModeType}。这会限制 AutoRetainerCN 执行的功能。");
        }

        if(C.OfflineData.Any(x => x.MultiWaitForAllDeployables))
        {
            Info("部分角色启用了“等待所有待返回潜艇/飞空艇”选项。这表示 AutoRetainerCN 会等这些角色的全部潜艇/飞空艇返回后再处理。悬停查看完整列表。", C.OfflineData.Where(x => x.MultiWaitForAllDeployables).Select(x => $"{x.Name}@{x.World}").Print("\n"));
        }

        if(C.MultiModeWorkshopConfiguration.MultiWaitForAll)
        {
            Info("全局选项“等待探险完成”已启用。这表示即使某些角色的单独选项已禁用，AutoRetainerCN 仍会等待所有角色的潜艇/飞空艇返回后再处理。");
        }

        if(C.MultiModeWorkshopConfiguration.WaitForAllLoggedIn)
        {
            Info("潜艇/飞空艇已启用“已登录时仍等待”选项。这表示即使你已经登录，AutoRetainerCN 仍会等待该角色所有潜艇/飞空艇完成后再处理。");
        }

        if(C.DisableRetainerVesselReturn > 0)
        {
            if(C.DisableRetainerVesselReturn > 10)
            {
                Warning("“雇员探险处理截止时间”设置得异常高。当潜艇/飞空艇即将可用时，重新派遣雇员可能出现明显延迟。");
            }
            else
            {
                Info("已启用“雇员探险处理截止时间”选项。当潜艇/飞空艇即将可用时，重新派遣雇员可能出现延迟。");
            }
        }

        if(C.MultiModeRetainerConfiguration.MultiWaitForAll)
        {
            Info("已启用“等待探险完成”选项。这表示 AutoRetainerCN 会等待角色所有雇员探险完成后，再登录处理它们。");
        }

        if(C.MultiModeRetainerConfiguration.WaitForAllLoggedIn)
        {
            Info("雇员已启用“已登录时仍等待”选项。这表示即使你已经登录，AutoRetainerCN 仍会等待该角色所有雇员探险完成后再处理。");
        }

        {
            var manualList = new List<string>();
            var deletedList = new List<string>();
            foreach(var x in C.OfflineData)
            {
                foreach(var ret in x.RetainerData)
                {
                    var planId = Utils.GetAdditionalData(x.CID, ret.Name).EntrustPlan;
                    var plan = C.EntrustPlans.FirstOrDefault(s => s.Guid == planId);
                    if(plan != null && plan.ManualPlan) manualList.Add($"{Censor.Character(x.Name)} - {Censor.Retainer(ret.Name)}");
                    if(plan == null && planId != Guid.Empty) deletedList.Add($"{Censor.Character(x.Name)} - {Censor.Retainer(ret.Name)}");
                }
            }
            if(manualList.Count > 0)
            {
                Info("部分雇员设置了手动委托计划。这些计划不会在重新派遣雇员探险后自动处理，只能通过叠加层按钮手动处理。悬停查看列表。", manualList.Print("\n"));
            }
            if(deletedList.Count > 0)
            {
                Warning("部分雇员原本使用的委托计划已被删除。使用已删除委托计划的雇员不会委托任何物品。悬停查看列表。", deletedList.Print("\n"));
            }
        }

        if(C.No2ndInstanceNotify)
        {
            Info("你启用了“不警告同目录第二个游戏实例”选项。使用同一 Dalamud 目录运行第二个游戏实例时，AutoRetainerCN 会自动跳过加载。");
        }

        if(Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "SimpleTweaksPlugin" && x.IsLoaded))
        {
            Info("检测到 Simple Tweaks 插件。与雇员或潜水艇相关的调整可能影响 AutoRetainerCN 功能。请确认这些调整不会干扰 AutoRetainerCN。");
        }

        if(Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "PandorasBox" && x.IsLoaded))
        {
            Info("检测到 Pandora's Box 插件。AutoRetainerCN 启用期间自动使用技能可能影响其功能。请确认 Pandora's Box 不会在 AutoRetainerCN 活动时自动使用技能。");
        }

        if(Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "Automaton" && x.IsLoaded))
        {
            Info("检测到 Automaton 插件。AutoRetainerCN 启用期间自动使用技能或自动输入数字可能影响其功能。请确认 Automaton 不会在 AutoRetainerCN 活动时自动使用技能或输入。");
        }

        if(Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "RotationSolver" && x.IsLoaded))
        {
            Info("检测到 RotationSolver 插件。AutoRetainerCN 启用期间自动使用技能可能影响其功能。请确认 RotationSolver 不会在 AutoRetainerCN 活动时自动使用技能。");
        }

        if(Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName.StartsWith("BossMod") && x.IsLoaded))
        {
            Info("检测到 BossMod 插件。AutoRetainerCN 启用期间自动使用技能可能影响其功能。请确认 BossMod 不会在 AutoRetainerCN 活动时自动使用技能。");
        }

        ImGui.Separator();
        ImGuiEx.TextWrapped("专家设置会改变开发者预期行为。请确认你的问题不是由错误配置的专家设置导致。");
        CheckExpertSetting("访问雇员铃且没有可用探险时的行为", nameof(C.OpenBellBehaviorNoVentures));
        CheckExpertSetting("访问雇员铃且存在可用探险时的行为", nameof(C.OpenBellBehaviorWithVentures));
        CheckExpertSetting("访问雇员铃后的任务完成行为", nameof(C.TaskCompletedBehaviorAccess));
        CheckExpertSetting("手动启用后的任务完成行为", nameof(C.TaskCompletedBehaviorManual));
        CheckExpertSetting("若 5 分钟内有雇员完成探险，则停留在雇员菜单", nameof(C.Stay5));
        CheckExpertSetting("关闭雇员列表时自动禁用插件", nameof(C.AutoDisable));
        CheckExpertSetting("不显示插件状态图标", nameof(C.HideOverlayIcons));
        CheckExpertSetting("显示多角色模式类型选择器", nameof(C.DisplayMMType));
        CheckExpertSetting("在工房显示潜艇/飞空艇复选框", nameof(C.ShowDeployables));
        CheckExpertSetting("启用脱困模块", nameof(C.EnableBailout));
        CheckExpertSetting("AutoRetainerCN 尝试脱困前的超时时间（秒）", nameof(C.BailoutTimeout));
        CheckExpertSetting("禁用排序和折叠/展开", nameof(C.NoCurrentCharaOnTop));
        CheckExpertSetting("在插件 UI 栏显示多角色模式复选框", nameof(C.MultiModeUIBar));
        CheckExpertSetting("雇员菜单延迟（秒）", nameof(C.RetainerMenuDelay));
        CheckExpertSetting("不检查探险计划器错误", nameof(C.NoErrorCheckPlanner2));
        CheckExpertSetting("启用多角色模式时尝试进入附近房屋", nameof(C.MultiHETOnEnable));
        CheckExpertSetting("Artisan 集成", nameof(C.ArtisanIntegration));
        CheckExpertSetting("使用服务器时间而不是电脑时间", nameof(C.UseServerTime));
    }

    private static void Error(string message, string tooltip = null)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGuiEx.Text(EColor.RedBright, "\uf057");
        ImGui.PopFont();
        if(tooltip != null) ImGuiEx.Tooltip(tooltip);
        ImGui.SameLine();
        ImGuiEx.TextWrapped(EColor.RedBright, message);
        if(tooltip != null) ImGuiEx.Tooltip(tooltip);
    }

    private static void Warning(string message, string tooltip = null)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGuiEx.Text(EColor.OrangeBright, "\uf071");
        ImGui.PopFont();
        if(tooltip != null) ImGuiEx.Tooltip(tooltip);
        ImGui.SameLine();
        ImGuiEx.TextWrapped(EColor.OrangeBright, message);
        if(tooltip != null) ImGuiEx.Tooltip(tooltip);
    }

    private static void Info(string message, string tooltip = null)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGuiEx.Text(EColor.YellowBright, "\uf05a");
        ImGui.PopFont();
        if(tooltip != null) ImGuiEx.Tooltip(tooltip);
        ImGui.SameLine();
        ImGuiEx.TextWrapped(EColor.YellowBright, message);
        if(tooltip != null) ImGuiEx.Tooltip(tooltip);
    }

    private static void CheckExpertSetting(string setting, string nameOfSetting)
    {
        var original = EmptyConfig.GetFoP(nameOfSetting);
        var current = C.GetFoP(nameOfSetting);
        if(!original.Equals(current))
        {
            Info($"专家设置“{setting}”与默认值不同", $"默认值为“{original}”，当前值为“{current}”。");
        }
    }
}
