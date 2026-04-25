using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoRetainer.UI.NeoUI.AdvancedEntries;
public unsafe sealed class CharacterSync : NeoUIEntry
{
    public override string Path => "高级/角色同步";

    private List<string> ToDelete = [];

    public override void Draw()
    {
        if(ToDelete.Count > 0)
        {
            if(ImGuiEx.BeginDefaultTable(["名称", "##control"]))
            {
                foreach(var item in ToDelete)
                {
                    var ocd = C.OfflineData.FirstOrDefault(x => x.NameWithWorld == item);
                    if(ocd != null)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGuiEx.Text($"{ocd.NameWithWorld}");
                        ImGui.TableNextColumn();
                        if(ImGui.SmallButton("从列表排除"))
                        {
                            new TickScheduler(() => ToDelete.Remove(item));
                        }
                    }
                    else
                    {
                        new TickScheduler(() => ToDelete.Remove(item));
                    }
                }
                ImGui.EndTable();
            }
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Trash, "从 AutoRetainerCN 删除列出的角色", enabled: ImGuiEx.Ctrl))
            {
                C.OfflineData.RemoveAll(x => ToDelete.Contains(x.NameWithWorld));
            }
            ImGuiEx.Tooltip("按住 CTRL 并点击");
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Ban, "取消"))
            {
                ToDelete.Clear();
            }
            return;
        }

        ImGuiEx.TextWrapped($"一键清理已删除角色。");
        var jbInstalled = Svc.PluginInterface.InstalledPlugins.Any(x => x.InternalName == "JustBackup" && x.IsLoaded);
        if(!jbInstalled)
        {
            ImGuiEx.TextWrapped(EColor.RedBright, "要继续操作，需要先安装 JustBackup 插件。");
            if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.WindowMaximize, "打开插件安装器"))
            {
                Svc.PluginInterface.OpenPluginInstallerTo(PluginInstallerOpenKind.AllPlugins, "JustBackup");
            }
            return;
        }
        ImGuiEx.TextWrapped($"""
            1. 输入 /justbackup 创建备份，并确认备份成功且保存到了安全位置。
            2. 打开 FFXIV Lodestone 上的角色列表。
            """);
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.ExternalLinkSquareAlt, "立即打开角色列表"))
        {
            ShellStart("https://eu.finalfantasyxiv.com/lodestone/account/select_character/");
        }
        ImGuiEx.TextWrapped($"3. 确认已登录正确账号，然后按 CTRL+A 再按 CTRL+C 复制整个页面内容。");
        ImGuiEx.TextWrapped($"4. 完成后，点击下面的按钮：");
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Paste, "准备清理角色"))
        {
            Parse();
        }
    }

    void Parse()
    {
        try
        {
            var lines = Paste().Split("\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var isParsing = false;
            List<string> charas = [];
            for(var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if(line == "Character")
                {
                    isParsing = true;
                }
                else if(line == "Update Character List")
                {
                    isParsing = false;
                }
                if(isParsing)
                {
                    if(!line.Contains('[') && !line.Contains(']') && line.Contains(' '))
                    {
                        var chara = line;
                        var world = lines[i + 1].Split(' ')[0];
                        var n = $"{chara}@{world}".Trim();
                        if(n != "")
                        {
                            charas.Add(n);
                        }
                    }
                }
            }
            if(charas.Count == 0)
            {
                Notify.Error("未读取到任何角色");
            }
            else
            {
                ToDelete = [.. C.OfflineData.Select(x => x.NameWithWorld).Where(x => !charas.Contains(x))];
                PluginLog.Debug($"To Delete: \n{ToDelete.Print("\n")}");
            }
        }
        catch(Exception e)
        {
            e.Log();
            Notify.Error("无法解析角色列表");
        }
    }
}
