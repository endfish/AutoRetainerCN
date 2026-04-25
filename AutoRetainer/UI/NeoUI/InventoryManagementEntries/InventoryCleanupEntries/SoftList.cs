using AutoRetainerAPI.Configuration;

namespace AutoRetainer.UI.NeoUI.InventoryManagementEntries.InventoryCleanupEntries;
public class SoftList : InventoryManagementBase
{
    public override string Name => "库存清理/自由探索出售列表";
    private InventoryManagementCommon InventoryManagementCommon = new();
    private SoftList()
    {
        Builder = InventoryCleanupCommon.CreateCleanupHeaderBuilder()
            .Section(Name)
            .TextWrapped("这些物品如果来自自由探索，将会被出售，除非它们已经与同类物品堆叠在一起。")
            .Widget(() => InventoryManagementCommon.DrawListNew(
                itemId => InventoryCleanupCommon.SelectedPlan.AddItemToList(IMListKind.SoftSell, itemId, out _),
                itemId => InventoryCleanupCommon.SelectedPlan.IMAutoVendorSoft.Remove(itemId), InventoryCleanupCommon.SelectedPlan.IMAutoVendorSoft,
                filter: item => item.PriceLow != 0))
            .Widget(() =>
            {
                InventoryManagementCommon.ImportFromArDiscard(InventoryCleanupCommon.SelectedPlan.IMAutoVendorSoft);
            });
    }
}
