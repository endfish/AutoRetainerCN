using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRetainer.UI.NeoUI;
public sealed unsafe class UserInterface : NeoUIEntry
{
    public override string Path => "界面";

    public override NuiBuilder Builder => new NuiBuilder()

        .Section("界面")
        .Checkbox("匿名显示雇员", () => ref C.NoNames, "在常规界面中隐藏雇员名称。调试菜单和插件日志中不会隐藏。启用后，不同页面中的角色/雇员编号不保证一一对应，例如雇员页面的“雇员 1”不一定是统计页面的同一个雇员。")
        .Checkbox("在雇员界面显示快捷菜单", () => ref C.UIBar)
        .Checkbox("显示扩展雇员信息", () => ref C.ShowAdditionalInfo, "在主界面显示雇员装等/采集/鉴别力以及当前探险名称。")
        .Widget("按 ESC 时不关闭 AutoRetainerCN 窗口", (x) =>
        {
            if(ImGui.Checkbox(x, ref C.IgnoreEsc)) Utils.ResetEscIgnoreByWindows();
        })
        .Checkbox("状态栏仅显示最重要的图标", () => ref C.StatusBarMSI)
        .SliderInt(120f, "状态栏图标大小", () => ref C.StatusBarIconWidth, 32, 128)
        .Checkbox("游戏启动时打开 AutoRetainerCN 窗口", () => ref C.DisplayOnStart)
        //.Checkbox("Skip item sell/trade confirmation while plugin is active", () => ref C.SkipItemConfirmations)
        .Checkbox("启用标题画面按钮（需要重启插件）", () => ref C.UseTitleScreenButton)
        .Checkbox("隐藏角色搜索", () => ref C.NoCharaSearch)
        .Checkbox("已完成角色不闪烁背景", () => ref C.NoGradient)
        .Checkbox("同目录第二个游戏实例不再提示", () => ref C.No2ndInstanceNotify, "启用后，第二个游戏实例会自动跳过 AutoRetainerCN 加载。除非在主实例中关闭此选项，否则第二实例无法加载插件。")

        .Section("雇员页角色排序")
        .Checkbox("启用", () => ref C.EnableRetainerSort)
        .TextWrapped("这只影响显示顺序，不会影响角色处理顺序。")
        .Widget(() => UIUtils.DrawSortableEnumList("rorder", C.RetainersVisualOrders))

        .Section("潜艇/飞空艇页角色排序")
        .Checkbox("启用", () => ref C.EnableDeployablesSort)
        .TextWrapped("这只影响显示顺序，不会影响角色处理顺序。")
        .Widget(() => UIUtils.DrawSortableEnumList("dorder", C.DeployablesVisualOrders));



}
