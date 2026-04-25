using ECommons.Configuration;
using ECommons.Reflection;

namespace AutoRetainer.UI.NeoUI.AdvancedEntries;
public class ExpertTab : NeoUIEntry
{
    public override string Path => "高级/专家设置";

    public override NuiBuilder Builder { get; init; } = new NuiBuilder()
        .Section("行为")
        .EnumComboFullWidth(null, "访问雇员铃且没有可收取探险时：", () => ref C.OpenBellBehaviorNoVentures, null, Lang.OpenBellBehaviorNames)
        .EnumComboFullWidth(null, "访问雇员铃且存在可收取探险时：", () => ref C.OpenBellBehaviorWithVentures, null, Lang.OpenBellBehaviorNames)
        .EnumComboFullWidth(null, "访问雇员铃后的任务完成行为：", () => ref C.TaskCompletedBehaviorAccess, null, Lang.TaskCompletedBehaviorNames)
        .EnumComboFullWidth(null, "手动启用后的任务完成行为：", () => ref C.TaskCompletedBehaviorManual, null, Lang.TaskCompletedBehaviorNames)
        .EnumComboFullWidth(null, "插件运行期间的任务完成行为：", () => ref C.TaskCompletedBehaviorAuto, null, Lang.TaskCompletedBehaviorNames)
        .TextWrapped(ImGuiColors.DalamudGrey, "多角色模式运行时，会强制使用“关闭雇员列表并禁用插件”类行为。")
        .Checkbox("若 5 分钟内还有雇员完成探险，则停留在雇员菜单", () => ref C.Stay5, "多角色模式运行时会强制启用此选项。")
        .Checkbox($"关闭雇员列表时自动禁用插件", () => ref C.AutoDisable, "只在你手动退出菜单时生效；其他情况使用上方设置。")
        .Checkbox($"不显示插件状态图标", () => ref C.HideOverlayIcons)
        .Checkbox($"显示多角色模式类型选择器", () => ref C.DisplayMMType)
        .Checkbox($"在工房中显示潜艇/飞空艇复选框", () => ref C.ShowDeployables)
        .Checkbox("启用脱困模块", () => ref C.EnableBailout)
        .InputInt(150f, "AutoRetainerCN 尝试脱困前的超时时间（秒）", () => ref C.BailoutTimeout)

        .Section("设置")
        .Checkbox($"禁用排序和折叠/展开", () => ref C.NoCurrentCharaOnTop)
        .Checkbox($"在插件 UI 栏显示多角色模式复选框", () => ref C.MultiModeUIBar)
        .SliderIntAsFloat(100f, "雇员菜单延迟（秒）", () => ref C.RetainerMenuDelay.ValidateRange(0, 2000), 0, 2000)
        .Checkbox($"允许探险计时器显示负数", () => ref C.TimerAllowNegative)
        .Checkbox($"不对探险计划器进行错误检查", () => ref C.NoErrorCheckPlanner2)
        .Checkbox("允许手动切换角色后处理", () => ref C.AllowManualPostprocess, "AutoRetainerCN 锁定在后处理阶段时，允许手动调用命令。")
        .Widget("市场冷却叠加层", (x) =>
        {
            if(ImGui.Checkbox(x, ref C.MarketCooldownOverlay))
            {
                if(C.MarketCooldownOverlay)
                {
                    P.Memory.OnReceiveMarketPricePacketHook?.Enable();
                }
                else
                {
                    P.Memory.OnReceiveMarketPricePacketHook?.Disable();
                }
            }
        })

        .Section("集成")
        .Checkbox($"Artisan 集成", () => ref C.ArtisanIntegration, "当雇员探险可收取且附近有雇员铃时，自动启用 AutoRetainerCN 并暂停 Artisan。处理完探险后会重新启用 Artisan 并恢复原本操作。")

        .Section("服务器时间")
        .Checkbox("使用服务器时间而不是电脑时间", () => ref C.UseServerTime)

        .Section("工具")
        .Widget("清理幽灵雇员", (x) =>
        {
            if(ImGui.Button(x))
            {
                var i = 0;
                foreach(var d in C.OfflineData)
                {
                    i += d.RetainerData.RemoveAll(x => x.Name == "");
                }
                DuoLog.Information($"已清理 {i} 条记录");
            }
        })

        .Section("导入/导出")
        .Widget(() =>
        {
            if(ImGui.Button("导出（不包含角色数据）"))
            {
                var clone = C.JSONClone();
                clone.OfflineData = null;
                clone.AdditionalData = null;
                clone.FCData = null;
                clone.SelectedRetainers = null;
                clone.Blacklist = null;
                clone.AutoLogin = "";
                Copy(EzConfig.DefaultSerializationFactory.Serialize(clone, false));
            }
            if(ImGui.Button("导入并合并角色数据"))
            {
                try
                {
                    var c = EzConfig.DefaultSerializationFactory.Deserialize<Config>(Paste());
                    c.OfflineData = C.OfflineData;
                    c.AdditionalData = C.AdditionalData;
                    c.FCData = C.FCData;
                    c.SelectedRetainers = C.SelectedRetainers;
                    c.Blacklist = C.Blacklist;
                    c.AutoLogin = C.AutoLogin;
                    if(c.GetType().GetFieldPropertyUnions().Any(x => x.GetValue(c) == null)) throw new NullReferenceException();
                    EzConfig.SaveConfiguration(C, $"Backup_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.json");
                    P.SetConfig(c);
                }
                catch(Exception e)
                {
                    e.LogDuo();
                }
            }
        });
}
