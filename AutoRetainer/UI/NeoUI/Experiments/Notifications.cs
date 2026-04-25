namespace AutoRetainer.UI.NeoUI.Experiments;
public class Notifications : ExperimentUIEntry
{
    public override string Name => "通知";

    public override void Draw()
    {
        ImGui.Checkbox($"有雇员完成探险时显示叠加层通知", ref C.NotifyEnableOverlay);
        ImGui.Checkbox($"副本或战斗中不显示叠加层", ref C.NotifyCombatDutyNoDisplay);
        ImGui.Checkbox($"包含其他角色", ref C.NotifyIncludeAllChara);
        ImGui.Checkbox($"忽略未启用多角色模式的其他角色", ref C.NotifyIgnoreNoMultiMode);
        ImGui.Checkbox($"在游戏聊天中显示通知", ref C.NotifyDisplayInChatX);
        ImGuiEx.Text($"游戏处于非活动状态时：（需要安装并启用 NotificationMaster）");
        ImGui.Checkbox($"雇员可用时发送桌面通知", ref C.NotifyDeskopToast);
        ImGui.Checkbox($"闪烁任务栏", ref C.NotifyFlashTaskbar);
        ImGui.Checkbox($"AutoRetainerCN 已启用或多角色模式运行时不通知", ref C.NotifyNoToastWhenRunning);
    }
}
