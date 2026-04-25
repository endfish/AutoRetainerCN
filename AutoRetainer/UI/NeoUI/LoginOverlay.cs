namespace AutoRetainer.UI.NeoUI;
public class LoginOverlay : NeoUIEntry
{
    public override string Path => "登录叠加层";

    public override NuiBuilder Builder { get; init; } = new NuiBuilder()
            .Section("登录叠加层")
            .Checkbox("显示登录叠加层", () => ref C.LoginOverlay)
            .Widget("登录叠加层缩放倍率", (x) =>
            {
                ImGuiEx.SetNextItemWidthScaled(150f);
                if(ImGuiEx.SliderFloat(x, ref C.LoginOverlayScale.ValidateRange(0.1f, 5f), 0.2f, 2f)) P.LoginOverlay.bWidth = 0;
            })
            .Widget($"登录按钮内边距", (x) =>
            {
                ImGuiEx.SetNextItemWidthScaled(150f);
                if(ImGuiEx.SliderFloat(x, ref C.LoginOverlayBPadding.ValidateRange(0.5f, 5f), 1f, 1.5f)) P.LoginOverlay.bWidth = 0;
            })
        .Checkbox("搜索时显示隐藏角色", () => ref C.LoginOverlayAllSearch)
        .SliderInt(150f, "列数", () => ref C.NumLoginOverlayCols.ValidateRange(1, 10), 1, 10)
        .SliderFloat(150f, "叠加层高度（%）", () => ref C.LoginOverlayPercent.ValidateRange(20f, 100f), 20f, 100f);
}
