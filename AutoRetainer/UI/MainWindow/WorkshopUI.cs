using AutoRetainer.Internal;
using AutoRetainer.Modules.Voyage;
using AutoRetainer.Modules.Voyage.Tasks;
using AutoRetainer.Modules.Voyage.VoyageCalculator;
using AutoRetainer.Scheduler.Tasks;
using AutoRetainer.UI.MainWindow.MultiModeTab;
using AutoRetainerAPI.Configuration;
using Dalamud.Game;
using Dalamud.Interface.Components;
using ECommons.MathHelpers;
using Lumina.Excel.Sheets;
using MessagePack;


namespace AutoRetainer.UI.MainWindow;

internal static unsafe class WorkshopUI
{
    private static float StatusTextWidth = 0;
    private static List<(ulong cid, ulong frame, Vector2 start, Vector2 end, float percent)> Bars = [];
    internal static void Draw()
    {
        List<OverlayTextData> overlayTexts = [];
        var sortedData = new List<OfflineCharacterData>();
        if(C.NoCurrentCharaOnTop)
        {
            sortedData = C.OfflineData.ApplyOrder(C.DeployablesVisualOrders);
        }
        else
        {
            if(C.OfflineData.TryGetFirst(x => x.CID == Svc.ClientState.LocalContentId, out var cdata))
            {
                sortedData.Add(cdata);
            }
            foreach(var x in C.OfflineData.ApplyOrder(C.DeployablesVisualOrders))
            {
                if(x.CID != Svc.ClientState.LocalContentId)
                {
                    sortedData.Add(x);
                }
            }
        }
        UIUtils.DrawSearch();
        foreach(var data in sortedData.Where(x => x.OfflineAirshipData.Count + x.OfflineSubmarineData.Count > 0 && !x.ExcludeWorkshop))
        {
            var search = Ref<string>.Get("SearchChara");
            if(search != "" && !$"{data.Name}@{data.World}".Contains(search, StringComparison.OrdinalIgnoreCase)) continue;
            ImGui.PushID($"Player{data.CID}");
            var rCurPos = ImGui.GetCursorPos();
            float pad = 0;
            ImGui.PushFont(UiBuilder.IconFont);
            ImGuiEx.ButtonCheckbox($"\uf21a##{data.CID}", ref data.WorkshopEnabled, 0xFF097000);
            ImGui.PopFont();
            ImGuiEx.Tooltip($"为此角色启用潜艇/飞空艇多角色模式");
            ImGuiEx.DragDropRepopulate("RepopWsEn", data.WorkshopEnabled, ref data.WorkshopEnabled);
            ImGui.SameLine(0, 3);
            if(ImGuiEx.IconButton(FontAwesomeIcon.DoorOpen))
            {
                if(MultiMode.Relog(data, out var error, RelogReason.ConfigGUI))
                {
                    Notify.Success("正在重新登录...");
                }
                else
                {
                    Notify.Error(error);
                }
            }
            ImGui.SameLine(0, 3);
            if(ImGuiEx.IconButton(FontAwesomeIcon.UserCog))
            {
                ImGui.OpenPopup($"popup{data.CID}");
            }
            ImGuiEx.Tooltip($"配置角色");
            ImGui.SameLine(0, 3);

            if(ImGui.BeginPopup($"popup{data.CID}"))
            {
                CharaConfig.Draw(data, false);
                ImGui.EndPopup();
            }

            if(data.NumSubSlots > data.GetVesselData(VoyageType.Submersible).Count)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.TextV(ImGuiColors.DalamudYellow, "\uf6e3");
                ImGui.PopFont();
                ImGuiEx.Tooltip($"可以建造新的潜水艇（{data.GetVesselData(VoyageType.Submersible).Count}/{data.NumSubSlots}）");
                ImGui.SameLine(0, 3);
            }

            if(data.IsNotEnoughSubmarinesEnabled())
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.TextV(ImGuiColors.DalamudOrange, "\ue4ac");
                ImGui.PopFont();
                ImGuiEx.Tooltip($"部分潜水艇未启用");
                ImGui.SameLine(0, 3);
            }

            if(data.IsThereNotAssignedSubmarine())
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.TextV(ImGuiColors.DalamudOrange, "\ue4ab");
                ImGui.PopFont();
                ImGuiEx.Tooltip($"部分潜水艇未在远航中");
                ImGui.SameLine(0, 3);
            }

            if(data.AreAnySuboptimalBuildsFound())
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.TextV(ImGuiColors.DalamudOrange, "\uf0ad");
                ImGui.PopFont();
                ImGuiEx.Tooltip($"发现非最优配置");
                ImGui.SameLine(0, 3);
            }

            if(data.AreAnyInvalidRedeploysActive())
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.TextV(ImGuiColors.DalamudRed, FontAwesomeIcon.ArrowsSpin.ToIconString());
                ImGui.PopFont();
                ImGuiEx.Tooltip($"重新派遣已启用，但部分解锁计划被设为强制使用。");
                ImGui.SameLine(0, 3);
            }

            if(C.OldStatusIcons)
            {
                if(C.MultiModeWorkshopConfiguration.MultiWaitForAll)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGuiEx.TextV("\uf252");
                    ImGui.PopFont();
                    ImGuiEx.Tooltip($"已全局启用等待所有潜艇/飞空艇返回。");
                    ImGui.SameLine(0, 3);
                }
                else if(data.MultiWaitForAllDeployables)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGuiEx.TextV("\uf252");
                    ImGui.PopFont();
                    ImGuiEx.Tooltip($"当前角色已启用等待所有潜艇/飞空艇返回。");
                    ImGui.SameLine(0, 3);
                }
            }

            data.DrawDCV();
            UIUtils.DrawTeleportIcons(data.CID);
            SharedUI.DrawLockout(data);

            var initCurpos = ImGui.GetCursorPos();
            var lst = data.GetVesselData(VoyageType.Airship).Where(s => data.GetEnabledVesselsData(VoyageType.Airship).Contains(s.Name))
                .Union(data.GetVesselData(VoyageType.Submersible).Where(x => data.GetEnabledVesselsData(VoyageType.Submersible).Contains(x.Name)))
                .Where(x => x.ReturnTime != 0).OrderBy(z => z.GetRemainingSeconds());
            //if (EzThrottler.Throttle("log")) PluginLog.Information($"{lst.Select(x => x.Name).Print()}");
            var lowestVessel = (C.MultiModeWorkshopConfiguration.MultiWaitForAll || data.MultiWaitForAllDeployables) && !data.AreAnyEnabledVesselsReturnInNext(C.MultiModeWorkshopConfiguration.MaxMinutesOfWaiting * 60) ? lst.LastOrDefault() : lst.FirstOrDefault();
            if(lowestVessel != default)
            {
                var prog = 1f - lowestVessel.GetRemainingSeconds() / (60f * 60f * 24f);
                prog.ValidateRange(0f, 1f);
                var pcol = prog == 1f ? (C.NoGradient ? 0xbb005000.ToVector4() : GradientColor.Get(0xbb500000.ToVector4(), 0xbb005000.ToVector4())) : 0xbb500000.ToVector4();
                ImGui.PushStyleColor(ImGuiCol.PlotHistogram, pcol);
                ImGui.ProgressBar(prog, new(ImGui.GetContentRegionAvail().X, ImGui.CalcTextSize("A").Y + ImGui.GetStyle().FramePadding.Y * 2), "");
                ImGui.PopStyleColor();
                ImGui.SetCursorPos(initCurpos);
            }

            var colpref = UIUtils.PushColIfPreferredCurrent(data);

            if(ImGuiEx.CollapsingHeader(data.GetCutCharaString(StatusTextWidth) + $"###workshop{data.CID}"))
            {
                MultiModeUI.SetAsPreferred(data);
                if(colpref) ImGui.PopStyleColor();
                pad = ImGui.GetStyle().FramePadding.Y;
                DrawTable(data);
            }
            else
            {
                MultiModeUI.SetAsPreferred(data);
                if(colpref) ImGui.PopStyleColor();
            }

            ImGui.SameLine(0, 0);
            List<(bool, string)> texts = [(data.RepairKits < C.UIWarningDepRepairNum, $"R: {data.RepairKits}"), (data.Ceruleum < C.UIWarningDepTanksNum, $"C: {data.Ceruleum}"), (data.InventorySpace < C.UIWarningDepSlotNum, $"I: {data.InventorySpace}")];
            overlayTexts.Add((new Vector2(ImGui.GetContentRegionMax().X - ImGui.GetStyle().FramePadding.X, rCurPos.Y + ImGui.GetStyle().FramePadding.Y), [.. texts]));
            ImGui.NewLine();

            ImGui.PopID();
        }
        StatusTextWidth = 0f;
        UIUtils.DrawOverlayTexts(overlayTexts, ref StatusTextWidth);
        Bars.RemoveAll(x => x.frame != Svc.PluginInterface.UiBuilder.FrameCount);

        ImGuiEx.LineCentered("WorkshopUI planner button", () =>
        {
            if(ImGui.Button("打开远航航线计划器"))
            {
                P.SubmarinePointPlanUI.IsOpen = true;
            }
            ImGui.SameLine();
            if(ImGui.Button("打开远航解锁计划器"))
            {
                P.SubmarineUnlockPlanUI.IsOpen = true;
            }
        });

        if(C.Verbose)
        {
            if(ImGui.CollapsingHeader("公开调试"))
            {
                try
                {
                    if(!P.TaskManager.IsBusy)
                    {
                        /*if (ImGui.Button("Resend currently selected submarine on previous voyage"))
                        {
                            TaskDeployOnPreviousVoyage.Enqueue();
                        }*/
                        if(ImGui.Button("选择最佳航线"))
                        {
                            TaskCalculateAndPickBestExpRoute.Enqueue();
                        }
                        if(ImGui.Button("选择包含 1 个解锁点的最佳航线"))
                        {
                            TaskCalculateAndPickBestExpRoute.Enqueue(VoyageUtils.GetSubmarineUnlockPlanByGuid(Data.GetAdditionalVesselData(GenericHelpers.Read(CurrentSubmarine.Get()->Name), VoyageType.Submersible).SelectedUnlockPlan) ?? new());
                        }
                        if(ImGui.Button("选择解锁航线（最多 5 点）"))
                        {
                            TaskDeployOnUnlockRoute.EnqueuePickOrCalc(VoyageUtils.GetSubmarineUnlockPlanByGuid(Data.GetAdditionalVesselData(GenericHelpers.Read(CurrentSubmarine.Get()->Name), VoyageType.Submersible).SelectedUnlockPlan) ?? new(), UnlockMode.MultiSelect);
                        }
                        if(ImGui.Button("选择解锁航线（仅 1 点）"))
                        {
                            TaskDeployOnUnlockRoute.EnqueuePickOrCalc(VoyageUtils.GetSubmarineUnlockPlanByGuid(Data.GetAdditionalVesselData(GenericHelpers.Read(CurrentSubmarine.Get()->Name), VoyageType.Submersible).SelectedUnlockPlan) ?? new(), UnlockMode.SpamOne);
                        }
                        if(ImGui.Button("选择航点计划器航线"))
                        {
                            var plan = VoyageUtils.GetSubmarinePointPlanByGuid(Data.GetAdditionalVesselData(GenericHelpers.Read(CurrentSubmarine.Get()->Name), VoyageType.Submersible).SelectedPointPlan);
                            if(plan != null)
                            {
                                TaskDeployOnPointPlan.EnqueuePick(plan);
                            }
                            else
                            {
                                DuoLog.Error($"未选择计划！");
                            }
                        }
                        foreach(var x in Data.OfflineSubmarineData)
                        {
                            if(ImGui.Button($"修理潜水艇 {x.Name} 的损坏部件"))
                            {
                                if(VoyageUtils.GetCurrentWorkshopPanelType() == PanelType.Submersible)
                                {
                                    TaskSelectVesselByName.Enqueue(x.Name, VoyageType.Submersible);
                                    TaskIntelligentRepair.Enqueue(x.Name, VoyageType.Submersible);
                                    P.TaskManager.Enqueue(VoyageScheduler.SelectQuitVesselMenu);
                                }
                                else
                                {
                                    Notify.Error("你不在潜水艇菜单中");
                                }
                            }
                        }
                        if(ImGui.Button("接近雇员铃"))
                        {
                            TaskInteractWithNearestBell.Enqueue(false);
                        }

                        if(ImGui.Button("接近面板"))
                        {
                            TaskInteractWithNearestPanel.Enqueue(false);
                        }

                        /*if (ImGui.Button("Redeploy current vessel on previous voyage"))
                        {
                            TaskRedeployPreviousLog.Enqueue();
                        }*/

                        //if (ImGui.Button($"Deploy current submarine on best experience route")) TaskDeployOnBestExpVoyage.Enqueue();

                    }
                    else
                    {
                        ImGuiEx.Text(EColor.RedBright, $"当前正在执行：{P.TaskManager.CurrentTask?.Name}");
                    }
                }
                catch(Exception e)
                {
                    ImGuiEx.TextWrapped(e.ToString());
                }
            }
        }
    }

    private static string data1 = "";
    private static VoyageType data2 = VoyageType.Airship;

    private static void DrawTable(OfflineCharacterData data)
    {
        var storePos = ImGui.GetCursorPos();
        foreach(var v in Bars.Where(x => x.cid == data.CID))
        {
            ImGui.SetCursorPos(v.start - ImGui.GetStyle().CellPadding with { Y = 0 });
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, 0xbb500000);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, 0);
            ImGui.ProgressBar(1f - Math.Min(1f, v.percent),
                new(ImGui.GetContentRegionAvail().X, v.end.Y - v.start.Y - ImGui.GetStyle().CellPadding.Y), "");
            ImGui.PopStyleColor(2);
        }
        ImGui.SetCursorPos(storePos);
        if(ImGui.BeginTable("##retainertable", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("名称", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("构造");
            ImGui.TableSetupColumn("远航");
            ImGui.TableSetupColumn("");
            ImGui.TableHeadersRow();
            for(var i = 0; i < data.OfflineAirshipData.Count; i++)
            {
                var vessel = data.OfflineAirshipData[i];
                DrawRow(data, vessel, VoyageType.Airship);
            }
            for(var i = 0; i < data.OfflineSubmarineData.Count; i++)
            {
                var vessel = data.OfflineSubmarineData[i];
                DrawRow(data, vessel, VoyageType.Submersible);
            }
            ImGui.EndTable();
        }
    }

    private static void DrawRow(OfflineCharacterData data, OfflineVesselData vessel, VoyageType type)
    {
        if(type == VoyageType.Airship && data.GetEnabledVesselsData(type).Count == 0 && C.HideAirships) return;
        ImGui.PushID($"{data.CID}/{vessel.Name}/{type}");
        var enabled = type == VoyageType.Airship ? data.EnabledAirships : data.EnabledSubs;
        var adata = data.GetAdditionalVesselData(vessel.Name, type);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, 0);
        var start = ImGui.GetCursorPos();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGuiEx.TextV(type == VoyageType.Airship ? "\ue22d" : "\uf21a");
        ImGui.PopFont();
        ImGui.SameLine();
        var disabled = data.OfflineSubmarineData.Count(x => data.EnabledSubs.Contains(x.Name)) + data.OfflineAirshipData.Count(x => data.EnabledAirships.Contains(x.Name)) >= 4 && !enabled.Contains(vessel.Name);
        if(disabled) ImGui.BeginDisabled();
        ImGuiEx.CollectionCheckbox($"{vessel.Name}##sub", vessel.Name, enabled);
        if(disabled) ImGui.EndDisabled();
        ImGui.SameLine();
        if(adata.VesselBehavior == VesselBehavior.Finalize)
        {
            ImGuiEx.IconButton(Lang.IconAnchor);
            repop();
        }
        else if(adata.VesselBehavior == VesselBehavior.Redeploy)
        {
            ImGuiEx.IconButton(Lang.IconResend);
            repop();
        }
        else if(adata.VesselBehavior == VesselBehavior.LevelUp)
        {
            ImGuiEx.IconButton(Lang.IconLevelup);
            repop();
        }
        else if(adata.VesselBehavior == VesselBehavior.Unlock)
        {
            ImGuiEx.IconButton(Lang.IconUnlock);
            repop();
            ImGui.SameLine();
            if(adata.UnlockMode == UnlockMode.WhileLevelling)
            {
                ImGuiEx.IconButton(Lang.IconLevelup);
            }
            else if(adata.UnlockMode == UnlockMode.SpamOne)
            {
                ImGuiEx.IconButton(Lang.IconRepeat);
            }
            else if(adata.UnlockMode == UnlockMode.MultiSelect)
            {
                ImGuiEx.IconButton(Lang.IconPath);
            }
            else
            {
                ImGuiEx.IconButton(Lang.IconWarning);
            }
        }
        else if(adata.VesselBehavior == VesselBehavior.Use_plan)
        {
            var plan = VoyageUtils.GetSubmarinePointPlanByGuid(adata.SelectedPointPlan);
            var valid = plan != null && plan.Points.Count.InRange(1, 5, true);
            var fast = plan != null && plan.Points.SequenceEqual(adata.Points.Where(x => x != 0).Select(x => (uint)x));
            Vector4? col = valid ? (fast ? EColor.GreenBright : null) : EColor.RedBright;
            if(col != null) ImGui.PushStyleColor(ImGuiCol.Text, col.Value);
            ImGuiEx.IconButton(Lang.IconPlanner);
            if(col != null) ImGui.PopStyleColor();
            if(valid) ImGuiEx.Tooltip(plan.Points.Select(x => $"{VoyageUtils.GetSubmarineExploration(x)?.FancyDestination()}").Print("\n"));
            repop();
        }
        else
        {
            ImGuiEx.IconButton(Lang.IconWarning);
        }
        void repop()
        {
            ImGuiEx.DragDropRepopulateClass("RepSubConf", adata, x =>
            {
                adata.VesselBehavior = x.VesselBehavior;
                if(adata.VesselBehavior == VesselBehavior.Use_plan)
                {
                    adata.SelectedPointPlan = x.SelectedPointPlan;
                }
                if(adata.VesselBehavior == VesselBehavior.Unlock)
                {
                    adata.SelectedUnlockPlan = x.SelectedUnlockPlan;
                    adata.UnlockMode = x.UnlockMode;
                }
            });
        }
        if(adata.IndexOverride > 0)
        {
            ImGui.SameLine();
            ImGuiEx.Text(ImGuiColors.DalamudGrey3, $"索引覆盖：{adata.IndexOverride}");
        }
        var end = ImGui.GetCursorPos();
        var p = vessel.GetRemainingSeconds() / (60f * 60f * 24f);
        if(vessel.ReturnTime != 0) Bars.Add((data.CID, Svc.PluginInterface.UiBuilder.FrameCount, start, end, vessel.ReturnTime == 0 ? 0 : p.ValidateRange(0f, 1f)));
        ImGui.TableNextColumn();
        ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, 0);
        if(adata.Level > 0)
        {
            var lvlf = 0;
            if(adata.CurrentExp > 0 && adata.NextLevelExp > 0)
            {
                lvlf = (int)(adata.CurrentExp * 100f / adata.NextLevelExp);
            }
            ImGuiEx.TextV(Lang.CharLevel + $"{adata.Level}".ReplaceByChar(Lang.Digits.Normal, Lang.Digits.GameFont));
            ImGui.SameLine(0, 0);
            ImGuiEx.Text(ImGuiColors.DalamudGrey3, $".{lvlf:D2}".ReplaceByChar(Lang.Digits.Normal, Lang.Digits.GameFont));
            ImGui.SameLine(0, 0);
            ImGuiEx.Text(adata.IsUnoptimalBuild(out var justification) ? ImGuiColors.DalamudOrange : null, adata.GetSubmarineBuild());
            if(justification != null)
            {
                ImGuiEx.Tooltip(justification);
            }
        }
        ImGui.TableNextColumn();
        ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, 0);

        if(vessel.ReturnTime == 0)
        {
            ImGuiEx.Text($"无远航");
        }
        else
        {
            List<string> points = [];
            foreach(var x in adata.Points)
            {
                if(x != 0)
                {
                    var d = Svc.Data.GetExcelSheet<SubmarineExploration>(ClientLanguage.Japanese).GetRowOrDefault(x);
                    if(d != null && d.Value.Location.ToString().Length > 0)
                    {
                        points.Add(d.Value.Location.ToString());
                    }
                }
            }
            ImGuiEx.Text(points.Join(""));
            ImGui.SameLine();
            if(C.TimerAllowNegative)
            {
                ImGuiEx.Text($"{VoyageUtils.Seconds2Time(vessel.GetRemainingSeconds())}");
            }
            else
            {
                ImGuiEx.Text(vessel.GetRemainingSeconds() > 0 ? $"{VoyageUtils.Seconds2Time(vessel.GetRemainingSeconds())}" : "远航已完成");
            }

        }
        ImGui.TableNextColumn();
        ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, 0);
        var n = $"{data.CID} {vessel.Name} settings";
        if(ImGuiEx.IconButton(FontAwesomeIcon.Cogs, $"{data.CID} {vessel.Name}"))
        {
            ImGui.OpenPopup(n);
        }
        if(ImGuiEx.BeginPopupNextToElement(n))
        {
            ImGui.CollapsingHeader($"{vessel.Name} - {Censor.Character(data.Name)} 配置  ##conf", ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.OpenOnArrow);
            ImGuiEx.Text($"船只行为：");
            ImGuiEx.EnumCombo("##vbeh", ref adata.VesselBehavior, Lang.VesselBehaviorNames);
            if(adata.VesselBehavior == VesselBehavior.Unlock)
            {
                ImGuiEx.Text($"解锁模式：");
                ImGuiEx.EnumCombo("##umode", ref adata.UnlockMode, Lang.UnlockModeNames);
                var currentPlan = VoyageUtils.GetSubmarineUnlockPlanByGuid(adata.SelectedUnlockPlan) ?? VoyageUtils.GetDefaultSubmarineUnlockPlan(false);
                var isDefault = VoyageUtils.GetSubmarineUnlockPlanByGuid(adata.SelectedUnlockPlan) == null;
                var text = Environment.TickCount64 % 2000 > 1000 ? "解锁所有航点" : "未选择计划或计划未知";
                if(ImGui.BeginCombo("##uplan", (currentPlan?.Name ?? text) + (isDefault ? "（默认）" : ""), ImGuiComboFlags.HeightLarge))
                {
                    if(ImGui.Button("打开编辑器"))
                    {
                        P.SubmarineUnlockPlanUI.IsOpen = true;
                        P.SubmarineUnlockPlanUI.SelectedPlanGuid = adata.SelectedUnlockPlan;
                    }
                    ImGui.SameLine();
                    if(ImGui.Button("清除计划"))
                    {
                        adata.SelectedUnlockPlan = Guid.Empty.ToString();
                    }
                    foreach(var x in C.SubmarineUnlockPlans)
                    {
                        if(ImGui.Selectable($"{x.Name}##{x.GUID}"))
                        {
                            adata.SelectedUnlockPlan = x.GUID;
                        }
                    }
                    ImGui.EndCombo();
                }
            }
            if(adata.VesselBehavior == VesselBehavior.Use_plan)
            {
                var currentPlan = VoyageUtils.GetSubmarinePointPlanByGuid(adata.SelectedPointPlan);
                if(ImGui.BeginCombo("##uplan", currentPlan.GetPointPlanName(), ImGuiComboFlags.HeightLarge))
                {
                    if(ImGui.Button("打开编辑器"))
                    {
                        P.SubmarinePointPlanUI.IsOpen = true;
                        P.SubmarinePointPlanUI.SelectedPlanGuid = adata.SelectedPointPlan;
                    }
                    ImGui.SameLine();
                    if(ImGui.Button("清除计划"))
                    {
                        adata.SelectedPointPlan = Guid.Empty.ToString();
                    }
                    foreach(var x in C.SubmarinePointPlans)
                    {
                        if(ImGui.Selectable($"{x.GetPointPlanName()}##{x.GUID}"))
                        {
                            adata.SelectedPointPlan = x.GUID;
                        }
                    }
                    ImGui.EndCombo();
                }
            }
            ImGui.Separator();
            ImGuiEx.SetNextItemWidthScaled(150f);
            ImGuiEx.SliderInt("索引覆盖", ref adata.IndexOverride, 0, 4, adata.IndexOverride == 0 ? "已禁用" : $"{adata.IndexOverride}");
            ImGuiComponents.HelpMarker($"如果 AutoRetainerCN 中的船只顺序与远航面板菜单不同，请使用此功能为顺序错误的船只设置正确索引。请确认索引与控制面板中的顺序一致。");
            if(ImGui.CollapsingHeader("我最近重命名了这艘船"))
            {
                if(ImGui.BeginCombo("##selprev", "选择之前的船只名称", ImGuiComboFlags.HeightLarge))
                {
                    var datas = ((Func<Dictionary<string, AdditionalVesselData>>)delegate
                    {
                        if(type == VoyageType.Airship) return data.AdditionalAirshipData;
                        if(type == VoyageType.Submersible) return data.AdditionalSubmarineData;
                        throw new ArgumentOutOfRangeException(nameof(type));
                    })();
                    foreach(var x in datas)
                    {
                        var d = data.GetVesselData(type).Any(z => z.Name == x.Key);
                        if(d) ImGui.BeginDisabled();
                        if(ImGui.Selectable($"{x.Key}"))
                        {
                            new TickScheduler(() =>
                            {
                                var copyTo = vessel.Name;
                                var newData = x.Value.JSONClone();
                                var toDelete = x.Key;
                                datas[copyTo] = x.Value;
                                datas.Remove(toDelete);
                                Notify.Success($"已将数据从 {toDelete} 移动到 {copyTo}");
                            });
                        }
                        if(d) ImGui.EndDisabled();
                    }
                    ImGui.EndCombo();
                }
            }
            if(C.Verbose)
            {
                if(ImGui.Button("Fake ready")) vessel.ReturnTime = (uint)P.Time;
                if(ImGui.Button("Fake ready+")) vessel.ReturnTime += 60u * (ImGui.GetIO().KeyCtrl ? 10u : 1u) * (ImGui.GetIO().KeyShift ? 10u : 1u);
                if(ImGui.Button("Fake ready-")) vessel.ReturnTime -= 60u * (ImGui.GetIO().KeyCtrl ? 10u : 1u) * (ImGui.GetIO().KeyShift ? 10u : 1u);
                if(ImGui.Button("Fake unready")) vessel.ReturnTime = (uint)(P.Time + 9999);
            }
            ImGui.EndPopup();
        }
        ImGui.PopID();
    }
}
