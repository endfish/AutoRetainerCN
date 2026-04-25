using System.Diagnostics;

namespace AutoRetainer.UI
{
    public static class CustomAboutTab
    {
        private static string GetImageURL()
        {
            return Svc.PluginInterface.Manifest.IconUrl ?? "";
        }

        public static void Draw()
        {
            ImGuiEx.LineCentered("About1", delegate
            {
                ImGuiEx.Text($"{Svc.PluginInterface.Manifest.Name} - {Svc.PluginInterface.Manifest.AssemblyVersion}");
            });

            ImGuiEx.LineCentered("About0", () =>
            {
                ImGuiEx.Text($"由 Puni.sh 和 NightmareXIV 发布并开发");
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.SameLine(0, 0);
                ImGuiEx.Text(ImGuiColors.DalamudRed, FontAwesomeIcon.Heart.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 0);
                ImGuiEx.Text($"，中文本地化由 Endfish 维护");
            });

            ImGuiHelpers.ScaledDummy(10f);
            ImGuiEx.LineCentered("About2", delegate
            {
                if(ThreadLoadImageHandler.TryGetTextureWrap(GetImageURL(), out var texture))
                {
                    ImGui.Image(texture.Handle, new(200f, 200f));
                }
            });
            ImGuiHelpers.ScaledDummy(10f);
            ImGuiEx.LineCentered("About3", delegate
            {
                ImGui.TextWrapped("项目公告、更新和支持请查看社区与源码仓库。");
            });
            ImGuiEx.LineCentered("About4", delegate
            {
                if(ImGui.Button("Discord"))
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = "https://discord.gg/Zzrcc8kmvy",
                        UseShellExecute = true
                    });
                }
                ImGui.SameLine();
                if(ImGui.Button("插件源"))
                {
                    ImGui.SetClipboardText("https://love.puni.sh/ment.json");
                    Notify.Success("链接已复制到剪贴板");
                }
                ImGui.SameLine();
                if(ImGui.Button("源码"))
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = Svc.PluginInterface.Manifest.RepoUrl,
                        UseShellExecute = true
                    });
                }
                ImGui.SameLine();
                if(ImGui.Button("赞助 Puni.sh 平台"))
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = "https://ko-fi.com/spetsnaz",
                        UseShellExecute = true
                    });
                }
            });
        }
    }
}
