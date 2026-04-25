using ECommons.GameHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRetainer.UI.NeoUI;
public sealed unsafe class AccountWhitelist : NeoUIEntry
{
    public override void Draw()
    {
        ImGuiEx.TextWrapped($"你可以设置账号白名单。使用未加入白名单的账号登录时，AutoRetainerCN 不会记录任何角色、雇员或潜水艇。");
        if(C.WhitelistedAccounts.Count == 0)
        {
            ImGuiEx.TextWrapped(EColor.GreenBright, "当前白名单状态：已禁用。添加账号后会启用。");
        }
        else
        {
            ImGuiEx.TextWrapped(EColor.YellowBright, "当前白名单状态：已启用。移除所有账号即可禁用。");
        }

        foreach(var x in C.WhitelistedAccounts)
        {
            ImGui.PushID(x.ToString());
            if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
            {
                new TickScheduler(() => C.WhitelistedAccounts.Remove(x));
            }
            ImGui.SameLine();
            ImGuiEx.TextV($"账号 {x}");
            ImGui.PopID();
        }
    }
}
