using ECommons.Throttlers;

namespace AutoRetainer.UI.NeoUI.MultiModeEntries;
public class MultiModeDeployables : NeoUIEntry
{
    public override string Path => "多角色模式/潜艇与飞空艇";

    public override NuiBuilder Builder { get; init; } = new NuiBuilder()
        .Section("多角色模式 - 潜艇/飞空艇")
        .Checkbox("等待远航完成", () => ref C.MultiModeWorkshopConfiguration.MultiWaitForAll, """启用后，AutoRetainerCN 会等待所有潜艇/飞空艇返回后再登录该角色。如果你因其他原因已登录该角色，它仍会重新派遣已完成的潜艇，除非全局设置“已登录时也等待”也启用。""")
        .Indent()
        .Checkbox("已登录时也等待", () => ref C.MultiModeWorkshopConfiguration.WaitForAllLoggedIn, """改变“等待远航完成”（全局和单角色）的行为，使 AutoRetainerCN 在已登录时也不单独重新派遣已完成潜艇，而是等待全部潜艇返回后再行动。""")
        .InputInt(120f, "最长等待（分钟）", () => ref C.MultiModeWorkshopConfiguration.MaxMinutesOfWaiting.ValidateRange(0, 9999), 10, 60, """如果等待其他潜艇/飞空艇返回会超过此分钟数，AutoRetainerCN 会忽略“等待远航完成”和“已登录时也等待”设置。""")
        .Unindent()
        .DragInt(60f, "提前切换阈值（秒）", () => ref C.MultiModeWorkshopConfiguration.AdvanceTimer.ValidateRange(0, 300), 0.1f, 0, 300, "在该角色潜艇可以重新派遣前，AutoRetainerCN 应提前多少秒登录。")
        .DragInt(120f, "雇员探险处理截止（分钟）", () => ref C.DisableRetainerVesselReturn.ValidateRange(0, 60), "若大于 0，AutoRetainerCN 会在任意角色计划重新派遣潜艇前的这段时间内停止处理雇员。会综合考虑前面的全部设置。")
        .Checkbox("远航派遣后立即出售无条件出售列表物品（需要雇员）", () => ref C.VendorItemAfterVoyage)
        .Checkbox("进入工房时定期检查部队箱金币", () => ref C.FCChestGilCheck, "进入工房时定期检查部队箱，以保持金币统计更新。")
        .Indent()
        .SliderInt(150f, "检查频率（小时）", () => ref C.FCChestGilCheckCd, 0, 24 * 5)
        .Widget("重置冷却", (x) =>
        {
            if(ImGuiEx.Button(x, C.FCChestGilCheckTimes.Count > 0)) C.FCChestGilCheckTimes.Clear();
        })
        .Unindent()
        .Checkbox("处理完所有潜艇/飞空艇后关闭游戏", () => ref C.ShutdownOnSubExhaustion)
        .Indent()
        .SliderFloat(150f, "若有潜艇/飞空艇会在这些小时内返回，则不关闭游戏", () => ref C.HoursForShutdown, 0f, 10f)
        .Widget(() =>
        {
            ImGuiEx.HelpMarker($"""
                当前：{(Utils.CanShutdownForSubs() ? "可以关闭" : "不能关闭")}
                距离强制关闭剩余：{EzThrottler.GetRemainingTime("ForceShutdownForSubs")}
                """);
        })
        .Unindent()
        .TextWrapped("进入工房后自动购买青磷水桶：")
        .Indent()
        .Widget(() =>
        {
            if(Data != null)
            {
                ImGui.Checkbox($"在 {Data.NameWithWorldCensored} 上启用", ref Data.AutoFuelPurchase);
            }
            ImGuiEx.TextWrapped($"若要为其他角色启用/禁用燃料购买，请前往“功能、排除、顺序”页面。");
        })
        .InputInt(150f, "剩余多少水桶时触发购买", () => ref C.AutoFuelPurchaseLow.ValidateRange(100, 99999))
        .InputInt(150f, "购买到背包中达到此数量", () => ref C.AutoFuelPurchaseMax)
        .Checkbox("仅在工房已解锁时购买", () => ref C.AutoFuelPurchaseOnlyWsUnlocked)
        .Unindent()
        .Checkbox("潜艇/飞空艇完成后退出游戏", () => ref C.ExitOnSubCompletion, "重要：启用后，多角色模式会被设为仅处理潜艇/飞空艇，不处理雇员。")
        .Indent()
        .InputInt(150f, "等待潜艇返回的最长时间（分钟）", () => ref C.ExitOnSubCompletionTime)
        .Unindent()
        ;
}
