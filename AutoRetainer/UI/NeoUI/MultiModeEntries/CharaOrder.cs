using AutoRetainerAPI.Configuration;

namespace AutoRetainer.UI.NeoUI.MultiModeEntries;
public class CharaOrder : NeoUIEntry
{
    public override string Path => "多角色模式/功能、排除、顺序";

    private static string Search = "";
    private static ImGuiEx.RealtimeDragDrop<OfflineCharacterData> DragDrop = new("CharaOrder", x => x.Identity);

    public override bool NoFrame { get; set; } = true;

    public override void Draw()
    {
        C.OfflineData.RemoveAll(x => C.Blacklist.Any(z => z.CID == x.CID));
        var b = new NuiBuilder()
        .Section("角色顺序")
        .Widget("你可以在这里排序角色。此顺序会影响多角色模式处理角色的顺序，也会影响插件界面和登录叠加层中的显示顺序。", (x) =>
        {
            ImGuiEx.TextWrapped($"你可以在这里排序角色。此顺序会影响多角色模式处理角色的顺序，也会影响插件界面和登录叠加层中的显示顺序。");
            ImGui.SetNextItemWidth(150f);
            ImGui.InputText($"搜索", ref Search, 50);
            DragDrop.Begin();
            if(ImGui.BeginTable("CharaOrderTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("##ctrl");
                ImGui.TableSetupColumn("角色", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("开关");
                ImGui.TableSetupColumn("删除");
                ImGui.TableHeadersRow();

                for(var index = 0; index < C.OfflineData.Count; index++)
                {
                    var chr = C.OfflineData[index];
                    ImGui.PushID(chr.Identity);
                    ImGui.TableNextRow();
                    DragDrop.SetRowColor(chr.Identity);
                    ImGui.TableNextColumn();
                    DragDrop.NextRow();
                    DragDrop.DrawButtonDummy(chr, C.OfflineData, index);
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV((Search != "" && ($"{chr.Name}@{chr.World}").Contains(Search, StringComparison.OrdinalIgnoreCase)) ? ImGuiColors.ParsedGreen : (Search == "" ? null : ImGuiColors.DalamudGrey3), Censor.Character(chr.Name, chr.World));
                    ImGui.TableNextColumn();
                    if(ImGuiEx.ButtonCheckbox(FontAwesomeIcon.Users, ref chr.ExcludeRetainer, inverted: true))
                    {
                        chr.Enabled = false;
                        C.SelectedRetainers.Remove(chr.CID);
                    }
                    ImGuiEx.Tooltip("启用雇员");
                    ImGuiEx.DragDropRepopulate("EnRet", chr.ExcludeRetainer, ref chr.ExcludeRetainer);
                    ImGui.SameLine();
                    if(ImGuiEx.ButtonCheckbox(FontAwesomeIcon.Ship, ref chr.ExcludeWorkshop, inverted: true))
                    {
                        chr.WorkshopEnabled = false;
                        chr.EnabledSubs.Clear();
                        chr.EnabledAirships.Clear();
                    }
                    ImGuiEx.Tooltip("启用潜艇/飞空艇");
                    ImGuiEx.DragDropRepopulate("EnDep", chr.ExcludeWorkshop, x =>
                    {
                        chr.ExcludeWorkshop = x;
                        if(!x)
                        {
                            chr.EnabledSubs.Clear();
                            chr.EnabledAirships.Clear();
                        }
                    });
                    ImGui.SameLine();
                    ImGuiEx.ButtonCheckbox(FontAwesomeIcon.DoorOpen, ref chr.ExcludeOverlay, inverted: true);
                    ImGuiEx.Tooltip("显示在登录叠加层");
                    ImGuiEx.DragDropRepopulate("EnLog", chr.ExcludeOverlay, ref chr.ExcludeOverlay);
                    ImGui.SameLine();
                    ImGuiEx.ButtonCheckbox(FontAwesomeIcon.Coins, ref chr.NoGilTrack, inverted: true);
                    ImGuiEx.Tooltip("将此角色金币计入总数");
                    ImGuiEx.DragDropRepopulate("EnGil", chr.NoGilTrack, ref chr.NoGilTrack);
                    ImGui.SameLine();
                    ImGuiEx.ButtonCheckbox(FontAwesomeIcon.GasPump, ref chr.AutoFuelPurchase, color:ImGuiColors.TankBlue);
                    ImGuiEx.Tooltip("允许此角色从工房购买燃料");
                    ImGuiEx.DragDropRepopulate("EnFuel", chr.AutoFuelPurchase, ref chr.AutoFuelPurchase);
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.UserMinus))
                    {
                        chr.ClearFCData();
                    }
                    ImGuiEx.Tooltip("重置此角色的部队数据和潜艇/飞空艇数据。下次登录并访问工房面板后会重新生成。");
                    ImGui.SameLine();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl))
                    {
                        new TickScheduler(() => C.OfflineData.Remove(chr));
                    }
                    ImGuiEx.Tooltip($"按住 CTRL 并点击以删除已存储角色数据。重新登录后会重新创建。");
                    ImGui.SameLine();
                    if(ImGuiEx.IconButton("\uf057", enabled: ImGuiEx.Ctrl))
                    {
                        C.Blacklist.Add((chr.CID, chr.Name));
                    }
                    ImGuiEx.Tooltip($"按住 CTRL 并点击以删除已存储角色数据，并阻止之后再次创建，相当于完全排除此角色，不再由 AutoRetainerCN 处理。");

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
            DragDrop.End();
        });


        if(C.Blacklist.Count != 0)
        {
            b = b.Section("已排除角色")
                .Widget(() =>
                {
                    for(var i = 0; i < C.Blacklist.Count; i++)
                    {
                        var d = C.Blacklist[i];
                        ImGuiEx.TextV($"{d.Name} ({d.CID:X16})");
                        ImGui.SameLine();
                        if(ImGui.Button($"删除##bl{i}"))
                        {
                            C.Blacklist.RemoveAt(i);
                            C.SelectedRetainers.Remove(d.CID);
                            break;
                        }
                    }
                });
        }

        b.Draw();
    }
}
