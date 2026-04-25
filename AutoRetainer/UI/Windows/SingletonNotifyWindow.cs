namespace AutoRetainer.UI.Windows;
public class SingletonNotifyWindow : NotifyWindow
{
    private bool IAmIdiot = false;
    private WindowSystem ws;
    public SingletonNotifyWindow() : base("AutoRetainerCN - 警告！")
    {
        IsOpen = true;
        ws = new();
        Svc.PluginInterface.UiBuilder.Draw += ws.Draw;
        ws.AddWindow(this);
    }

    public override void OnClose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= ws.Draw;
    }

    public override void DrawContent()
    {
        ImGuiEx.Text($"AutoRetainerCN 检测到另一个使用相同数据路径配置的插件实例正在运行。");
        ImGuiEx.Text($"为防止数据丢失，插件加载已暂停。");
        if(ImGui.Button("关闭此窗口且不加载 AutoRetainerCN"))
        {
            IsOpen = false;
        }
        if(ImGui.Button("了解如何正确运行 2 个或更多游戏实例"))
        {
            ShellStart("https://github.com/PunishXIV/AutoRetainer/issues/62");
        }
        ImGui.Separator();
        ImGui.Checkbox($"我同意这可能导致丢失所有 AutoRetainerCN 数据", ref IAmIdiot);
        if(!IAmIdiot) ImGui.BeginDisabled();
        if(ImGui.Button("加载 AutoRetainerCN"))
        {
            IsOpen = false;
            new TickScheduler(P.Load);
        }
        if(!IAmIdiot) ImGui.EndDisabled();
    }
}
