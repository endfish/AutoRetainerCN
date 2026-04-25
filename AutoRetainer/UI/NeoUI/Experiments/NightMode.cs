namespace AutoRetainer.UI.NeoUI.Experiments;

internal class NightMode : ExperimentUIEntry
{
    public override string Name => "夜间模式";
    public override void Draw()
    {
        ImGuiEx.TextWrapped($"夜间模式：\n" +
                $"- 强制启用登录界面等待选项\n" +
                $"- 强制应用内置 FPS 限制器\n" +
                $"- 游戏窗口未聚焦且等待时，游戏会被限制到 0.2 FPS\n" +
                $"- 看起来可能像卡住了；重新激活游戏窗口后，请给它最多 5 秒恢复时间。\n" +
                $"- 默认情况下，夜间模式只处理潜艇/飞空艇\n" +
                $"- 禁用夜间模式后，脱困管理器会启动并重新登录回游戏。");
        if(ImGui.Checkbox("启用夜间模式", ref C.NightMode)) MultiMode.BailoutNightMode();
        ImGui.Checkbox("显示夜间模式复选框", ref C.ShowNightMode);
        ImGui.Checkbox("夜间模式处理雇员", ref C.NightModeRetainers);
        ImGui.Checkbox("夜间模式处理潜艇/飞空艇", ref C.NightModeDeployables);
        ImGui.Checkbox("保持夜间模式状态", ref C.NightModePersistent);
        ImGui.Checkbox("关机命令改为启用夜间模式而不是关闭游戏", ref C.ShutdownMakesNightMode);
    }
}
