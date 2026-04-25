namespace AutoRetainer.UI.NeoUI;
public class MainSettings : NeoUIEntry
{
    public override string Path => "常规";

    public override NuiBuilder Builder { get; init; } = new NuiBuilder()
        .Section("延迟")
        .Widget(100f, "时间不同步补偿", (x) => ImGuiEx.SliderInt(x, ref C.UnsyncCompensation.ValidateRange(-60, 0), -10, 0), "从探险结束时间中额外扣除的秒数，用于缓解游戏和电脑时间不同步导致的问题。")
        .Widget(100f, "额外交互延迟（帧）", (x) => ImGuiEx.SliderInt(x, ref C.ExtraFrameDelay.ValidateRange(-10, 100), 0, 50), "数值越低，插件执行动作越快。低帧率或高延迟时可调高；想让插件更快时可调低。")
        .Widget("额外日志", (x) => ImGui.Checkbox(x, ref C.ExtraDebug), "启用后会输出大量调试日志，可能刷屏并影响性能。插件重载或游戏重启后会自动关闭。")

            .Section("操作模式")
        .Widget("派遣 + 重新派遣", (x) =>
        {
            if(ImGui.RadioButton(x, C.EnableAssigningQuickExploration && !C._dontReassign))
            {
                C.EnableAssigningQuickExploration = true;
                C.DontReassign = false;
            }
        }, "如果启用的雇员当前没有探险，会自动派遣自由探索；已有探险则重新派遣当前探险。")
        .Widget("仅收取", (x) =>
        {
            if(ImGui.RadioButton(x, !C.EnableAssigningQuickExploration && C._dontReassign))
            {
                C.EnableAssigningQuickExploration = false;
                C.DontReassign = true;
            }
        }, "只收取雇员探险奖励，不重新派遣。\n与雇员铃交互时按住 CTRL 可临时使用此模式。")
        .Widget("重新派遣", (x) =>
        {
            if(ImGui.RadioButton("重新派遣", !C.EnableAssigningQuickExploration && !C._dontReassign))
            {
                C.EnableAssigningQuickExploration = false;
                C.DontReassign = false;
            }
        }, "只重新派遣雇员正在执行的探险。")
        .Widget("雇员铃感应", (x) => ImGui.Checkbox(x, ref C.RetainerSense), "当玩家位于雇员铃可交互范围内时，AutoRetainerCN 会自动启用。玩家必须保持静止，否则会取消自动启用。")
        .Widget(200f, "触发时间", (x) => ImGuiEx.SliderIntAsFloat(x, ref C.RetainerSenseThreshold, 1000, 100000));


}
