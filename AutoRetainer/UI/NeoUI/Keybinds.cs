namespace AutoRetainer.UI.NeoUI;
public class Keybinds : NeoUIEntry
{
    public override string Path => "快捷键";

    public override NuiBuilder Builder { get; init; } = new NuiBuilder()
        .Section("访问雇员铃/工房面板快捷键")
        .Widget("使用雇员铃/工房面板时，临时阻止 AutoRetainerCN 自动启用", (x) =>
        {
            UIUtils.DrawKeybind(x, ref C.Suppress);
        })
        .Widget("临时使用“仅收取”模式，阻止本轮重新派遣探险；或临时将潜艇/飞空艇模式设为仅收取", (x) =>
        {
            UIUtils.DrawKeybind(x, ref C.TempCollectB);
        })

        .Section("雇员快捷操作")
        .Widget("出售物品", (x) => UIUtils.QRA(x, ref C.SellKey))
        .Widget("委托保管物品", (x) => UIUtils.QRA(x, ref C.EntrustKey))
        .Widget("取回物品", (x) => UIUtils.QRA(x, ref C.RetrieveKey))
        .Widget("上架出售", (x) => UIUtils.QRA(x, ref C.SellMarketKey));
}
