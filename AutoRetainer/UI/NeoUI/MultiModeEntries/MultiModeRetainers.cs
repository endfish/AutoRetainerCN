namespace AutoRetainer.UI.NeoUI.MultiModeEntries;
public class MultiModeRetainers : NeoUIEntry
{
    public override string Path => "多角色模式/雇员";

    public override NuiBuilder Builder { get; init; } = new NuiBuilder()
        .Section("多角色模式 - 雇员")
        .Checkbox("等待探险完成", () => ref C.MultiModeRetainerConfiguration.MultiWaitForAll, "多角色模式运行时，AutoRetainerCN 会等待所有雇员返回后再切换到下一个角色。")
        .DragInt(60f, "提前切换阈值", () => ref C.MultiModeRetainerConfiguration.AdvanceTimer.ValidateRange(0, 300), 0.1f, 0, 300)
        .SliderInt(100f, "继续操作所需最少背包格", () => ref C.MultiMinInventorySlots.ValidateRange(2, 9999), 2, 30)
        .Checkbox("同步雇员（一次性）", () => ref MultiMode.Synchronize, "AutoRetainerCN 会等待所有已启用雇员完成探险。完成后此设置会自动关闭，并继续处理所有角色。")
        .Checkbox($"强制完整角色轮转", () => ref C.CharEqualize, "推荐 15 个以上角色的用户使用。强制多角色模式按顺序处理所有角色后，再回到循环开头。")
        .Indent()
        .Checkbox("按探险完成时间排序角色", () => ref C.LongestVentureFirst, "探险完成更久的角色会优先检查")
        .Checkbox("按雇员等级和等级上限排序角色", () => ref C.CappedLevelsLast, "先处理可升级雇员的角色；然后处理满级雇员角色；最后处理未满级但受角色职业等级限制的雇员。")
        .Unindent();
}
