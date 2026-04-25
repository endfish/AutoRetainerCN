using AutoRetainerAPI.Configuration;

namespace AutoRetainer.UI.NeoUI.InventoryManagementEntries.InventoryCleanupEntries;
public class ProtectionList : InventoryManagementBase
{
    public override string Name { get; } = "库存清理/保护列表";
    private InventoryManagementCommon InventoryManagementCommon = new();
    private ProtectionList()
    {
        DisplayPriority = -1;
        Builder = InventoryCleanupCommon.CreateCleanupHeaderBuilder()
            .Section(Name)
            .TextWrapped("AutoRetainerCN 不会出售、精选、丢弃或交纳这些物品，即使它们被加入了其他处理列表。")
            .Widget(() => InventoryManagementCommon.DrawListNew(
                itemId => InventoryCleanupCommon.SelectedPlan.AddItemToList(IMListKind.Protect, itemId, out _),
                itemId => InventoryCleanupCommon.SelectedPlan.IMProtectList.Remove(itemId), InventoryCleanupCommon.SelectedPlan.IMProtectList))
            .Separator()
            .Widget(() =>
            {
                InventoryManagementCommon.ImportBlacklistFromArDiscard();
            });
    }

}
