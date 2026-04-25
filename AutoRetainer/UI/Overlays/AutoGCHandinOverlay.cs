using AutoRetainerAPI.Configuration;
using Dalamud.Interface.Style;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoRetainer.UI.Overlays;

internal unsafe class AutoGCHandinOverlay : Window
{
    internal float height;
    internal bool Allowed = false;
    internal int Remaining = 0;
    public AutoGCHandinOverlay() : base("AutoRetainerCN 军票交纳叠加层", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoSavedSettings, true)
    {
        RespectCloseHotkey = false;
        IsOpen = true;
    }

    public override void Draw()
    {
        if(Allowed)
        {
            ImGui.Checkbox("启用自动筹备品交纳", ref AutoGCHandin.Operation);
        }
        if(C.OfflineData.TryGetFirst(x => x.CID == Svc.ClientState.LocalContentId, out var d) && !AutoGCHandin.Operation)
        {
            ImGui.SameLine();
            ImGuiEx.SetNextItemWidthScaled(200);
            ImGuiEx.EnumCombo("##mode", ref d.GCDeliveryType, Lang.GCDeliveryTypeNames);
            if(d.GCDeliveryType == GCDeliveryType.Hide_Gear_Set_Items)
            {
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text(Lang.IconWarning);
                ImGui.PopFont();
            }
            if(d.GCDeliveryType == GCDeliveryType.Show_All_Items)
            {
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text($"{Lang.IconWarning}{Lang.IconWarning}{Lang.IconWarning}");
                ImGui.PopFont();
            }
        }
        //1078	Priority Seal Allowance	Company seals earned are increased.	ui/icon/016000/016518.tex	0	0	All Classes	1	dk05th_stup0t		False	False	False	False	False	False	False	False	False	0	1	False	False	15	0	False	0	False	0	False	0	0	0	False
        if(!Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1078) && InventoryManager.Instance()->GetInventoryItemCount(14946) > 0)
        {
            ImGui.SameLine();
            ImGuiEx.Text(GradientColor.Get(ImGuiColors.DalamudRed, ImGuiColors.DalamudYellow), $"可以使用军票优先支给票据");
        }
        if(!Player.IsInHomeWorld)
        {
            ImGui.SameLine();
            ImGuiEx.Text(GradientColor.Get(ImGuiColors.DalamudRed, ImGuiColors.DalamudYellow), $"当前在外服世界。不会获得部队点数。");
        }
        if(AutoGCHandin.Operation && Remaining != 0)
        {
            ImGui.SameLine();
            ImGuiEx.Text(ImGuiColors.DalamudViolet, $"剩余 {Remaining} 件物品");
        }
        height = ImGui.GetWindowSize().Y;
    }

    public override bool DrawConditions()
    {
        return Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInQuestEvent] && (Allowed || (TryGetAddonByName<AtkUnitBase>("GrandCompanySupplyList", out var addon)
                && addon->UldManager.NodeListCount > 20
                && addon->UldManager.NodeList[5]->IsVisible()));
    }
}
