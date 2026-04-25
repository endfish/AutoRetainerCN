using AutoRetainerAPI.Configuration;
using Dalamud.Interface.Components;
using PunishLib.ImGuiMethods;

namespace AutoRetainer.UI.MainWindow.MultiModeTab;
public class CharaConfig
{
    public static void Draw(OfflineCharacterData data, bool isRetainer)
    {
        ImGui.PushID(data.CID.ToString());
        SharedUI.DrawMultiModeHeader(data);
        var b = new NuiBuilder()

        .Section("角色专属常规设置")
        .Widget(() =>
        {
            SharedUI.DrawServiceAccSelector(data);
            SharedUI.DrawPreferredCharacterUI(data);
        });
        if(isRetainer)
        {
            b = b.Section("雇员").Widget(() =>
            {
                ImGuiEx.Text($"自动筹备品交纳：");
                if(!AutoGCHandin.Operation)
                {
                    ImGuiEx.SetNextItemWidthScaled(200f);
                    ImGuiEx.EnumCombo("##gcHandin", ref data.GCDeliveryType, Lang.GCDeliveryTypeNames);
                }
                else
                {
                    ImGuiEx.Text($"当前无法修改");
                }
            });
        }
        else
        {
            b = b.Section("潜艇/飞空艇").Widget(() =>
            {
                ImGui.Checkbox($"等待远航完成", ref data.MultiWaitForAllDeployables);
                ImGuiComponents.HelpMarker("""此设置和全局选项类似，但只应用于单个角色。启用后，AutoRetainerCN 会等该角色所有潜艇/飞空艇返回后才登录处理。如果你因为其他原因已经登录该角色，它仍会重新派遣已完成的潜艇，除非全局设置“已登录时也等待”也已启用。""");
            });
        }
        b = b.Section("传送覆盖设置", data.GetAreTeleportSettingsOverriden() ? ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg] with { X = 1f } : null, true)
        .Widget(() =>
        {
            ImGuiEx.Text($"你可以为每个角色单独覆盖传送设置。");
            bool? demo = null;
            ImGuiEx.Checkbox("带有此标记的选项会使用全局配置值", ref demo);
            ImGuiEx.Checkbox("启用", ref data.TeleportOptionsOverride.Enabled);
            ImGui.Indent();
            ImGuiEx.Checkbox("为雇员流程传送...", ref data.TeleportOptionsOverride.Retainers);
            ImGui.Indent();
            ImGuiEx.Checkbox("...到个人房屋", ref data.TeleportOptionsOverride.RetainersPrivate);
            ImGuiEx.Checkbox("...到共享房屋", ref data.TeleportOptionsOverride.RetainersShared);
            ImGuiEx.Checkbox("...到部队房屋", ref data.TeleportOptionsOverride.RetainersFC);
            ImGuiEx.Checkbox("...到公寓", ref data.TeleportOptionsOverride.RetainersApartment);
            ImGui.Text("如果以上全部禁用或失败，将传送到旅馆。");
            ImGui.Unindent();
            ImGuiEx.Checkbox("潜艇/飞空艇流程传送到部队房屋", ref data.TeleportOptionsOverride.Deployables);
            ImGui.Unindent(); 
        }).Draw();
        SharedUI.DrawExcludeReset(data);
        ImGui.PopID();
    }
}
