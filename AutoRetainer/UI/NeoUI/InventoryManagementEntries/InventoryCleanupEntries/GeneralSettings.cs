using AutoRetainer.Internal.InventoryManagement;
using ECommons.GameHelpers;
using TerraFX.Interop.Windows;

namespace AutoRetainer.UI.NeoUI.InventoryManagementEntries.InventoryCleanupEntries;
public class GeneralSettings : InventoryManagementBase
{
    public override string Name { get; } = "库存清理/常规设置";

    private GeneralSettings()
    {
        Builder = InventoryCleanupCommon.CreateCleanupHeaderBuilder()
            .Section(Name)
            .Checkbox($"自动开启探险宝箱", () => ref InventoryCleanupCommon.SelectedPlan.IMEnableCofferAutoOpen, "仅多角色模式。登出前会开启所有宝箱，除非背包空间过低。")
            .Indent()
            .InputInt(100f, "单次最多开启数量", () => ref InventoryCleanupCommon.SelectedPlan.MaxCoffersAtOnce)
            .Unindent()
            .Checkbox($"启用向雇员出售物品", () => ref InventoryCleanupCommon.SelectedPlan.IMEnableAutoVendor, "AutoRetainerCN 检查并重新派遣雇员探险时，会按照库存清理计划出售物品。")
            .Checkbox($"启用向房屋 NPC 出售物品", () => ref InventoryCleanupCommon.SelectedPlan.IMEnableNpcSell, "AutoRetainerCN 进入房屋后，会按照库存清理计划出售物品。支持出售物品的房屋商人必须放在房屋入口附近（不是工房入口），确保进屋后能立刻交互。")
            .Indent()
            .Checkbox($"如果雇员可用则忽略 NPC", () => ref InventoryCleanupCommon.SelectedPlan.IMSkipVendorIfRetainer)
            .Widget("立即出售", (x) =>
            {
                if(ImGuiEx.Button(x, Player.Interactable && InventoryCleanupCommon.SelectedPlan.IMEnableNpcSell && NpcSaleManager.GetValidNPC() != null && !IsOccupied() && !P.TaskManager.IsBusy))
                {
                    NpcSaleManager.EnqueueIfItemsPresent(true);
                }
            })
            .Unindent()
            .Checkbox($"自动精选物品", () => ref InventoryCleanupCommon.SelectedPlan.IMEnableItemDesynthesis)
            .Indent()
            .Widget("兵装库：", t =>
            {
                ImGuiEx.TextV(t);
                ImGui.SameLine();
                ImGuiEx.RadioButtonBool("精选", "跳过", ref InventoryCleanupCommon.SelectedPlan.IMEnableItemDesynthesisFromArmory, true);
            })
            .Unindent()
            .Checkbox($"启用右键菜单集成", () => ref InventoryCleanupCommon.SelectedPlan.IMEnableContextMenu)
            .Checkbox($"允许出售/丢弃兵装库中的物品", () => ref InventoryCleanupCommon.SelectedPlan.AllowSellFromArmory)
            .Checkbox($"演示模式", () => ref InventoryCleanupCommon.SelectedPlan.IMDry, "不会实际出售/丢弃物品，而是在聊天中输出本应处理的内容")
            ;
    }
}
