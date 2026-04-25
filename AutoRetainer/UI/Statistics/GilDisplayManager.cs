using AutoRetainerAPI.Configuration;
using Dalamud.Interface.Components;
using ECommons.ExcelServices;

namespace AutoRetainer.UI.Statistics;

public sealed class GilDisplayManager
{
    private GilDisplayManager() { }

    public void Draw()
    {
        ImGuiEx.SetNextItemWidthScaled(200f);
        ImGui.InputInt("忽略金币少于此值的角色/雇员", ref C.MinGilDisplay.ValidateRange(0, int.MaxValue));
        ImGuiComponents.HelpMarker($"被忽略的雇员金币仍会计入角色/数据中心总计。只有角色自身金币和所有雇员金币都低于此值时，该角色才会被忽略。被忽略的角色不会计入数据中心总计。");
        ref var filter = ref Ref<string>.Get();
        ImGui.Checkbox("只显示角色总计", ref C.GilOnlyChars);
        ImGui.SameLine();
        ImGuiEx.SetNextItemFullWidth();
        ImGui.InputTextWithHint("##fltr", "筛选...", ref filter, 50);
        Dictionary<ExcelWorldHelper.Region, List<OfflineCharacterData>> data = [];
        foreach(var x in C.OfflineData)
        {
            if(ExcelWorldHelper.TryGet(x.World, out var world))
            {
                if(!data.ContainsKey((ExcelWorldHelper.Region)world.DataCenter.Value.Region))
                {
                    data[(ExcelWorldHelper.Region)world.DataCenter.Value.Region] = [];
                }
                data[(ExcelWorldHelper.Region)world.DataCenter.Value.Region].Add(x);
            }
        }
        var globalTotal = 0L;
        foreach(var x in data)
        {
            ImGuiEx.Text($"{x.Key}:");
            var dcTotal = 0L;
            foreach(var c in x.Value)
            {
                if(c.NoGilTrack) continue;
                if(filter != "" && !$"{c.Name}@{c.World}".Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
                FCData fcdata = null;
                var charTotal = c.Gil + c.RetainerData.Sum(s => s.Gil);
                foreach(var fc in C.FCData)
                {
                    if(S.FCData.GetHolderChara(fc.Key, fc.Value) == c && fc.Value.GilCountsTowardsChara)
                    {
                        fcdata = fc.Value;
                        charTotal += fcdata.Gil;
                        break;
                    }
                }
                if(charTotal > C.MinGilDisplay)
                {
                    if(!C.GilOnlyChars)
                    {
                        ImGuiEx.Text($"    {Censor.Character(c.Name, c.World)}: {c.Gil:N0}");
                        foreach(var r in c.RetainerData)
                        {
                            if(r.Gil > C.MinGilDisplay)
                            {
                                ImGuiEx.Text($"        {Censor.Retainer(r.Name)}: {r.Gil:N0}");
                            }
                        }
                        if(fcdata != null && fcdata.Gil > 0)
                        {
                            ImGuiEx.Text(ImGuiColors.DalamudYellow, $"        部队 {fcdata.Name}: {fcdata.Gil:N0}");
                        }
                    }
                    ImGuiEx.Text(ImGuiColors.DalamudViolet, $"    {Censor.Character(c.Name, c.World)}{(fcdata != null && fcdata.Gil > 0 ? "+部队" : "")} 总计：{charTotal:N0}");
                    if(ImGuiEx.HoveredAndClicked("点击重新登录到该角色"))
                    {
                        if(!MultiMode.Relog(c, out var error, Internal.RelogReason.Command))
                        {
                            Notify.Error(error);
                        }
                    }
                    dcTotal += charTotal;
                    ImGui.Separator();
                }
            }
            ImGuiEx.Text(ImGuiColors.DalamudOrange, $"数据中心总计（{x.Key}）：{dcTotal:N0}");
            globalTotal += dcTotal;
            ImGui.Separator();
            ImGui.Separator();
        }
        ImGuiEx.Text(ImGuiColors.DalamudOrange, $"全部总计：{globalTotal:N0}");
    }
}
