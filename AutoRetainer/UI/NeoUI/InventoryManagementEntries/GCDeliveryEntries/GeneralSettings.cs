using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRetainer.UI.NeoUI.InventoryManagementEntries.GCDeliveryEntries;
public sealed unsafe class GeneralSettings : InventoryManagementBase
{
    public override string Name { get; } = "军队筹备品交纳/常规设置";

    public override NuiBuilder Builder => new NuiBuilder()
        .Section("常规设置")
        .Checkbox("启用筹备品交纳续交", () => ref C.AutoGCContinuation)
        .TextWrapped($"""
            启用筹备品交纳续交后：
            - 插件会自动消耗可用军票，根据已配置的兑换列表购买物品。
            - 如果兑换列表为空，只会购买探险币。
            - 请确保“角色配置”中的“交纳模式”没有设为“禁用”。

            军票花费后：
            - 筹备品交纳会自动继续。
            - 此流程会重复，直到没有可交纳物品或没有剩余军票。
            """)

        .Section("多角色模式筹备品交纳")
        .TextWrapped($"""
        启用后：
        - 多角色模式期间，已启用传送且军衔足够的角色会自动进行筹备品交纳，并按兑换计划购买物品。
        """)
        .Checkbox("启用多角色模式筹备品交纳", () => ref C.FullAutoGCDelivery)
        .Checkbox("仅在工房未锁定时", () => ref C.FullAutoGCDeliveryOnlyWsUnlocked)
        .InputInt(150f, "剩余背包格小于等于此值时触发交纳", () => ref C.FullAutoGCDeliveryInventory, "只计算主背包，不计算兵装库")
        .Checkbox("探险币耗尽时触发", () => ref C.FullAutoGCDeliveryDeliverOnVentureExhaust, "这可能导致每次登录都前往军票兑换。请确保已有购买足够探险币的兑换计划。")
        .Indent()
        .InputInt(150f, "剩余探险币小于等于此值时触发交纳", () => ref C.FullAutoGCDeliveryDeliverOnVentureLessThan)
        .Unindent()
        .Checkbox("可用时使用军票优先支给票", () => ref C.FullAutoGCDeliveryUseBuffItem)
        .Checkbox("可用时使用部队军票加成", () => ref C.FullAutoGCDeliveryUseBuffFCAction)
        .Checkbox("交纳后传送回房屋/旅馆", () => ref C.TeleportAfterGCExchange)
        .Indent()
        .Checkbox("仅在多角色模式启用时", () => ref C.TeleportAfterGCExchangeMulti)
        .Unindent()
        ;
}
