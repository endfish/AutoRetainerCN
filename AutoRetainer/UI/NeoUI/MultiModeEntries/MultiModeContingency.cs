using AutoRetainerAPI.Configuration;
using System.Collections.Frozen;

namespace AutoRetainer.UI.NeoUI.MultiModeEntries;
public class MultiModeContingency : NeoUIEntry
{
    private static readonly FrozenDictionary<WorkshopFailAction, string> WorkshopFailActionNames = new Dictionary<WorkshopFailAction, string>()
    {
        [WorkshopFailAction.StopPlugin] = "停止所有插件操作",
        [WorkshopFailAction.ExcludeVessel] = "从操作中排除该潜艇/飞空艇",
        [WorkshopFailAction.ExcludeChar] = "从多角色轮转中排除该角色",
    }.ToFrozenDictionary();

    public override string Path => "多角色模式/应急策略";

    public override NuiBuilder Builder { get; init; } = new NuiBuilder()
        .Section("应急策略")
        .TextWrapped("这里可以为常见失败状态或潜在操作错误设置备用处理方式。")
        .EnumComboFullWidth(null, "青磷水桶耗尽", () => ref C.FailureNoFuel, (x) => x != WorkshopFailAction.ExcludeVessel, WorkshopFailActionNames, "派遣新远航时青磷水桶不足，将执行所选备用操作。")
        .EnumComboFullWidth(null, "无法修理潜艇/飞空艇", () => ref C.FailureNoRepair, null, WorkshopFailActionNames, "修理潜艇/飞空艇时魔导机械修理材料不足，将执行所选备用操作。")
        .EnumComboFullWidth(null, "背包已满", () => ref C.FailureNoInventory, (x) => x != WorkshopFailAction.ExcludeVessel, WorkshopFailActionNames, "队长背包空间不足以接收远航奖励时，将执行所选备用操作。")
        .EnumComboFullWidth(null, "严重操作失败", () => ref C.FailureGeneric, (x) => x != WorkshopFailAction.ExcludeVessel, WorkshopFailActionNames, "发生未知或杂项错误时，将执行所选备用操作。")
        .Widget("被 GM 关进监狱", (x) =>
        {
            ImGui.BeginDisabled();
            ImGuiEx.SetNextItemFullWidth();
            if(ImGui.BeginCombo("##jailsel", "终止游戏")) { ImGui.EndCombo(); }
            ImGui.EndDisabled();
        }, "插件运行时如果被 GM 关进监狱，将执行所选备用操作。祝好运。");
}
