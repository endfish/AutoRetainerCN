namespace AutoRetainer.UI.NeoUI;
public class MiscTab : NeoUIEntry
{
    public override string Path => "杂项";

    public override NuiBuilder Builder { get; init; } = new NuiBuilder()
        .Section("统计")
        .Checkbox($"记录探险统计", () => ref C.RecordStats)

        .Section("自动筹备品交纳")
        .Checkbox("交纳完成时发送托盘通知（需要 NotificationMaster）", () => ref C.GCHandinNotify)

        .Section("性能")

        .If(() => Utils.IsBusy)
        .Widget("", (x) => ImGui.BeginDisabled())
        .EndIf()

        .Checkbox($"插件运行时移除最小化 FPS 限制", () => ref C.UnlockFPS)
        .Checkbox($"- 同时移除常规 FPS 限制", () => ref C.UnlockFPSUnlimited)
        .Checkbox($"- 同时暂停 ChillFrames 插件", () => ref C.UnlockFPSChillFrames)
        .Checkbox($"插件运行时提高 FFXIV 进程优先级", () => ref C.ManipulatePriority, "可能导致其他程序变慢")

        .If(() => Utils.IsBusy)
        .Widget("", (x) => ImGui.EndDisabled())
        .EndIf();
}
