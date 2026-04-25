namespace AutoRetainer.UI.NeoUI.MultiModeEntries;
public class MultiModeFPSLimiter : NeoUIEntry
{
    public override string Path => "多角色模式/FPS 限制";

    public override NuiBuilder Builder { get; init; } = new NuiBuilder()
        .Section("FPS 限制")
        .TextWrapped("FPS 限制只会在多角色模式启用时生效")
        .Widget("空闲时目标帧率", (x) =>
        {
            ImGui.SetNextItemWidth(100f);
            UIUtils.SliderIntFrameTimeAsFPS(x, ref C.TargetMSPTIdle, C.ExtraFPSLockRange ? 1 : 10);
        })
        .Widget("操作时目标帧率", (x) =>
        {
            ImGui.SetNextItemWidth(100f);
            UIUtils.SliderIntFrameTimeAsFPS("操作时目标帧率", ref C.TargetMSPTRunning, C.ExtraFPSLockRange ? 1 : 20);
        })
        .Checkbox("游戏窗口激活时解除 FPS 锁定", () => ref C.NoFPSLockWhenActive)
        .Checkbox($"允许极低 FPS 限制值", () => ref C.ExtraFPSLockRange, "启用此选项后，如果多角色模式出现任何错误，将不提供支持")
        .Checkbox($"仅在设置关闭倒计时时启用限制器", () => ref C.FpsLockOnlyShutdownTimer);
}
