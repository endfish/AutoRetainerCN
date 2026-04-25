namespace AutoRetainer.UI.NeoUI.MultiModeEntries;
public class MultiModeCommon : NeoUIEntry
{
    public override string Path => "多角色模式/通用设置";

    public override NuiBuilder Builder { get; init; } = new NuiBuilder()
        .Section("通用设置")
        .Checkbox($"在登录画面等待", () => ref C.MultiWaitOnLoginScreen, "如果没有可处理探险的角色，会登出并等待任意角色可用。启用此选项和多角色模式时会禁用标题画面动画。")
        .Checkbox($"手动登录时禁用多角色模式", () => ref C.MultiDisableOnRelog, "通过 AutoRetainerCN 界面或命令切换角色后，自动禁用多角色模式。")
        .Checkbox($"手动登录时不重置偏好角色", () => ref C.MultiNoPreferredReset, "通过 AutoRetainerCN 界面或命令切换角色后，不重置偏好角色。")
        .Checkbox("允许进入共享房屋", () => ref C.SharedHET)
        .Checkbox("即使多角色模式禁用，登录时也尝试进入房屋", () => ref C.HETWhenDisabled)
        .Checkbox("已在雇员铃旁时，不为雇员流程传送或进房", () => ref C.NoTeleportHetWhenNextToBell)

        .Section("游戏启动")
        .Checkbox($"游戏启动时启用多角色模式", () => ref C.MultiAutoStart)
        .Checkbox($"插件启动时启用多角色模式", () => ref C.MultiOnPluginLoad)
        .Indent()
        .SliderInt(150f, "延迟（秒）", () => ref C.MultiModeOnPluginLoadDelay, 0, 20)
        .Unindent()
        .Widget("游戏启动时自动登录", (x) =>
        {
            ImGui.SetNextItemWidth(150f);
            var names = C.OfflineData.Where(s => !s.Name.IsNullOrEmpty()).Select(s => $"{s.Name}@{s.World}");
            var dict = names.ToDictionary(s => s, s => Censor.Character(s));
            dict.Add("", "禁用");
            dict.Add("~", "上次登录的角色");
            ImGuiEx.Combo(x, ref C.AutoLogin, ["", "~", .. names], names: dict);
        })
        .SliderInt(150f, "延迟", () => ref C.AutoLoginDelay.ValidateRange(0, 60), 0, 20, "设置适当延迟，让插件在登录前完成加载，也给自己留出取消登录的时间。")

        .Section("库存警告")
        .InputInt(100f, $"雇员列表：剩余背包格警告", () => ref C.UIWarningRetSlotNum.ValidateRange(2, 1000))
        .InputInt(100f, $"雇员列表：剩余探险币警告", () => ref C.UIWarningRetVentureNum.ValidateRange(2, 1000))
        .InputInt(100f, $"潜艇/飞空艇列表：剩余背包格警告", () => ref C.UIWarningDepSlotNum.ValidateRange(2, 1000))
        .InputInt(100f, $"潜艇/飞空艇列表：剩余青磷水桶警告", () => ref C.UIWarningDepTanksNum.ValidateRange(20, 1000))
        .InputInt(100f, $"潜艇/飞空艇列表：剩余修理材料警告", () => ref C.UIWarningDepRepairNum.ValidateRange(5, 1000))

        .Section("传送")
        .Widget(() => ImGuiEx.Text("需要 Lifestream 插件"))
        .Widget(() => ImGuiEx.PluginAvailabilityIndicator([new("Lifestream", new Version("2.2.1.1"))]))
        .TextWrapped("要让此选项生效，需要在 Lifestream 中为每个角色注册房屋，或启用简单传送。")
        .TextWrapped("你也可以在角色配置菜单中为每个角色单独自定义这些设置。")
        .Widget(() =>
        {
            if(Data != null && Data.GetAreTeleportSettingsOverriden())
            {
                ImGuiEx.TextWrapped(ImGuiColors.DalamudRed, "当前角色已自定义传送选项。");
            }
        })
        .Checkbox("启用", () => ref C.GlobalTeleportOptions.Enabled)
        .Indent()
        .Checkbox("为雇员流程传送...", () => ref C.GlobalTeleportOptions.Retainers)
        .Indent()
        .Checkbox("...到个人房屋", () => ref C.GlobalTeleportOptions.RetainersPrivate)
        .Checkbox("...到共享房屋", () => ref C.GlobalTeleportOptions.RetainersShared)
        .Checkbox("...到部队房屋", () => ref C.GlobalTeleportOptions.RetainersFC)
        .Checkbox("...到公寓", () => ref C.GlobalTeleportOptions.RetainersApartment)
        .TextWrapped("如果以上全部禁用或失败，将传送到旅馆。")
        .Unindent()
        .Checkbox("潜艇/飞空艇流程传送到部队房屋", () => ref C.GlobalTeleportOptions.Deployables)
        .Checkbox("启用简单传送", () => ref C.AllowSimpleTeleport)
        .Unindent()
        .Widget(() => ImGuiEx.HelpMarker("""
            允许在不向 Lifestream 注册房屋的情况下传送到房屋。注意：传送功能仍然需要 Lifestream 插件。

            警告：此选项不如在 Lifestream 中注册房屋可靠，请仅在必要时使用。
            """, EColor.RedBright, FontAwesomeIcon.ExclamationTriangle.ToIconString()))

        .Section("脱困模块")
        .Checkbox("连接错误时自动关闭并重试登录", () => ref C.ResolveConnectionErrors, "断线后 AutoRetainerCN 会尝试重新登录。如果会话已过期，则不会尝试登录。")
        .Widget(() => ImGuiEx.PluginAvailabilityIndicator([new("NoKillPlugin")]));
}
