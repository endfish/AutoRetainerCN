using AutoRetainerAPI.Configuration;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using Lumina.Excel.Sheets;
using System.Numerics;
using GrandCompany = ECommons.ExcelServices.GrandCompany;
using GrandCompanyRank = Lumina.Excel.Sheets.GrandCompanyRank;

namespace AutoRetainer.UI.NeoUI.InventoryManagementEntries.GCDeliveryEntries;
public sealed unsafe class ExchangeLists : InventoryManagementBase
{
    private ImGuiEx.RealtimeDragDrop<GCExchangeItem> DragDrop = new("GCELDD", x => x.ID);
    public override string Name { get; } = "军队筹备品交纳/兑换列表";
    private GCExchangeCategoryTab? SelectedCategory = null;
    private GCExchangeCategoryTab? SelectedCategory2 = null;
    private GCExchangeRankTab? SelectedRank = null;
    private Guid SelectedPlanGuid = Guid.Empty;

    public override int DisplayPriority => -5;

    public override void Draw()
    {
        C.AdditionalGCExchangePlans.Where(x => x.GUID == Guid.Empty).Each(x => x.GUID = Guid.NewGuid());
        ImGuiEx.TextWrapped($"""
            选择军队筹备品自动交纳期间要自动购买的物品。
            购买逻辑：
            - 系统会尝试购买列表中第一个可用物品。
            - 会持续购买，直到背包中该物品数量达到指定目标数量。
            如果列表中的物品都无法购买，或背包放不下：
            - 系统会改为购买探险币。
            - 会持续购买探险币，直到探险币数量达到 65,000。
            达到探险币上限且没有其他可购买物品后：
            - 多余军票会被丢弃。
            """);

        var selectedPlan = C.AdditionalGCExchangePlans.FirstOrDefault(x => x.GUID == SelectedPlanGuid);
        ImGuiEx.InputWithRightButtonsArea(() =>
        {
            if(ImGui.BeginCombo("##selplan", selectedPlan?.DisplayName ?? "默认计划"))
            {
                if(ImGui.Selectable("默认计划", selectedPlan == null)) SelectedPlanGuid = Guid.Empty;
                ImGui.Separator();
                foreach(var x in C.AdditionalGCExchangePlans)
                {
                    ImGui.PushID(x.ID);
                    if(ImGui.Selectable(x.DisplayName)) SelectedPlanGuid = x.GUID;
                    ImGui.PopID();
                }
                ImGui.EndCombo();
            }
        }, () =>
        {
            if(ImGuiEx.IconButton(FontAwesomeIcon.Plus))
            {
                var newPlan = new GCExchangePlan();
                C.AdditionalGCExchangePlans.Add(newPlan);
                SelectedPlanGuid = newPlan.GUID;
            }
            ImGuiEx.Tooltip("新增计划");
            ImGui.SameLine(0, 1);
            if(ImGuiEx.IconButton(FontAwesomeIcon.Copy))
            {
                var clone = (selectedPlan ?? C.DefaultGCExchangePlan).DSFClone();
                clone.GUID = Guid.Empty;
                Copy(EzConfig.DefaultSerializationFactory.Serialize(clone));
            }
            ImGuiEx.Tooltip("复制");
            ImGui.SameLine(0, 1);
            if(ImGuiEx.IconButton(FontAwesomeIcon.Paste))
            {
                try
                {
                    var newPlan = EzConfig.DefaultSerializationFactory.Deserialize<GCExchangePlan>(Paste()) ?? throw new NullReferenceException();
                    newPlan.GUID.Regenerate();  
                    C.AdditionalGCExchangePlans.Add(newPlan);
                    SelectedPlanGuid = newPlan.GUID;
                }
                catch(Exception e)
                {
                    e.Log();
                    Notify.Error(e.Message);
                }
            }
            ImGuiEx.Tooltip("粘贴");
            if(selectedPlan != null)
            {
                ImGui.SameLine(0, 1);
                if(ImGuiEx.IconButton(FontAwesomeIcon.ArrowsUpToLine, enabled: ImGuiEx.Ctrl && selectedPlan != null))
                {
                    C.DefaultGCExchangePlan = selectedPlan.DSFClone();
                    C.DefaultGCExchangePlan.Name = "";
                    C.DefaultGCExchangePlan.GUID.Regenerate();
                    new TickScheduler(() => C.AdditionalGCExchangePlans.Remove(selectedPlan));
                }
                ImGuiEx.Tooltip("将此计划设为默认。当前默认计划会被覆盖。按住 CTRL 并点击。");
                ImGui.SameLine(0, 1);
                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash, enabled: ImGuiEx.Ctrl && selectedPlan != null))
                {
                    new TickScheduler(() => C.AdditionalGCExchangePlans.Remove(selectedPlan));
                }
                ImGuiEx.Tooltip("删除此计划。按住 CTRL 并点击。");
            }
        });

        if(SelectedPlanGuid == Guid.Empty)
        {
            DrawGCEchangeList(C.DefaultGCExchangePlan);
        }
        else
        {
            if(Data != null)
            {
                if(Data.ExchangePlan == SelectedPlanGuid)
                {
                    ImGuiEx.Text(ImGuiColors.ParsedGreen, UiBuilder.IconFont, FontAwesomeIcon.Check.ToIconString());
                    ImGui.SameLine();
                    ImGuiEx.Text(ImGuiColors.ParsedGreen, $"当前角色正在使用");
                    ImGui.SameLine();
                    if(ImGui.SmallButton("取消分配"))
                    {
                        Data.ExchangePlan = Guid.Empty;
                    }
                }
                else
                {
                    ImGuiEx.Text(ImGuiColors.DalamudOrange, UiBuilder.IconFont, FontAwesomeIcon.ExclamationTriangle.ToIconString());
                    ImGui.SameLine();
                    ImGuiEx.Text(ImGuiColors.DalamudOrange, $"当前角色未使用");
                    ImGui.SameLine();
                    if(ImGui.SmallButton("分配"))
                    {
                        Data.ExchangePlan = selectedPlan.GUID;
                    }
                }
                ImGui.SameLine();
            }

            var charas = C.OfflineData.Where(x => x.ExchangePlan == selectedPlan.GUID).ToArray();
            if(charas.Length > 0)
            {
                ImGuiEx.Text($"共 {charas.Length} 个角色正在使用");
                ImGuiEx.Tooltip($"{charas.Select(x => x.NameWithWorldCensored)}");
            }
            else
            {
                ImGuiEx.Text($"没有任何角色使用");
            }

                var planIndex = C.AdditionalGCExchangePlans.IndexOf(x => x.GUID == SelectedPlanGuid);
            if(planIndex == -1)
            {
                SelectedPlanGuid = Guid.Empty;
            }
            else
            {
                DrawGCEchangeList(C.AdditionalGCExchangePlans[planIndex]);
            }
        }
    }

    public void DrawGCEchangeList(GCExchangePlan plan)
    {
        ref string getFilter() => ref Ref<string>.Get($"{plan.ID}filter");
        ref bool onlySelected() => ref Ref<bool>.Get($"{plan.ID}onlySel");
        ref string getFilter2() => ref Ref<string>.Get($"{plan.ID}filter2");

        ImGui.PushID(plan.ID);
        plan.Validate();

        ImGuiEx.InputWithRightButtonsArea("GCPlanSettings", () =>
        {
            if(ReferenceEquals(plan, C.DefaultGCExchangePlan))
            {
                ImGui.BeginDisabled();
                var s = "默认兑换计划不能重命名";
                ImGui.InputText("##name", ref s, 1);
                ImGui.EndDisabled();
            }
            else
            {
                ImGui.InputTextWithHint($"##name", "名称", ref plan.Name, 100);
                ImGuiEx.Tooltip("兑换计划名称");
            }
        }, () =>
        {
            ImGui.SetNextItemWidth(100f);
            ImGui.InputInt("保留军票", ref plan.RemainingSeals.ValidateRange(0, 70000), 0, 0);
            ImGuiEx.HelpMarker($"执行购买列表后会保留这些军票。不过根据角色军衔，此值最多只能低于军票上限 20000。");
            ImGui.SameLine();
            ImGui.Checkbox("结束时购买物品", ref plan.FinalizeByPurchasing);
            ImGuiEx.HelpMarker("若选中，最后一次兑换后会购买物品；否则会等到军票再次达到上限才购买。");
        });

        ImGuiEx.SetNextItemFullWidth();
        if(ImGui.BeginCombo("##Add Items", "添加物品", ImGuiComboFlags.HeightLarge))
        {
            ImGuiEx.InputWithRightButtonsArea(() =>
            {
                ImGui.InputTextWithHint("##filter2", "搜索...", ref getFilter2(), 100);
            }, () =>
            {
                ImGui.SetNextItemWidth(100f);
                ImGuiEx.EnumCombo("##cat2", ref SelectedCategory2, nullName: "所有分类");
                ImGuiEx.Tooltip("分类");
            });
            foreach(var x in Utils.SharedGCExchangeListings)
            {
                if(getFilter2().Length > 0
                    && !x.Value.Data.Name.GetText().Contains(getFilter2(), StringComparison.OrdinalIgnoreCase)
                    && !x.Value.Category.ToString().Equals(getFilter2(), StringComparison.OrdinalIgnoreCase)
                    && !Utils.GCRanks[x.Value.MinPurchaseRank].Equals(getFilter2(), StringComparison.OrdinalIgnoreCase)
                    ) continue;
                if(SelectedCategory2 != null && x.Value.Category != SelectedCategory2.Value) continue;
                var cont = plan.Items.Select(s => s.ItemID).ToArray();
                if(ThreadLoadImageHandler.TryGetIconTextureWrap(x.Value.Data.Icon, false, out var t))
                {
                    ImGui.Image(t.Handle, new(ImGui.GetTextLineHeight()));
                    ImGui.SameLine();
                }
                if(ImGui.Selectable(x.Value.Data.GetName() + $"##{x.Key}", cont.Contains(x.Key), ImGuiSelectableFlags.DontClosePopups))
                {
                    plan.Items.Add(new(x.Key, 0));
                }
            }
            ImGui.EndCombo();
        }
        if(ImGui.BeginPopup("Ex"))
        {
            if(ImGui.Selectable("为了额外部队点数，自动填充最优武器和防具购买项"))
            {
                List<GCExchangeItem> items = [];
                var qualifyingItems = Utils.SharedGCExchangeListings.Where(x => (x.Value.Category == GCExchangeCategoryTab.Weapons || x.Value.Category == GCExchangeCategoryTab.Armor) && x.Value.Data.GetRarity() == ItemRarity.Green).ToDictionary();
                plan.Items.RemoveAll(x => qualifyingItems.ContainsKey(x.ItemID));
                foreach(var item in qualifyingItems)
                {
                    items.Add(new(item.Key, 0));
                }
                items = items.OrderByDescending(x => (double)Svc.Data.GetExcelSheet<GCSupplyDutyReward>().GetRow(x.Data.Value.LevelItem.RowId).SealsExpertDelivery / (double)Utils.SharedGCExchangeListings[x.ItemID].Seals).ToList();
                foreach(var x in items)
                {
                    plan.Items.Add(x);
                    x.Quantity = Utils.SharedGCExchangeListings[x.ItemID].Data.IsUnique ? 1 : 999;
                }
            }
            ImGuiEx.Tooltip("选择此项会用所有可购买的武器和装备填充计划。这样会购买这些物品并立刻交回军队，以最大化获得的部队点数。所有这些物品都会放在列表末尾，只在没有其他可购买项时购买。");
            if(ImGui.Selectable("添加所有缺失物品"))
            {
                foreach(var x in Utils.SharedGCExchangeListings)
                {
                    if(!plan.Items.Any(i => i.ItemID == x.Key))
                    {
                        plan.Items.Add(new(x.Key, 0));
                    }
                }
            }
            if(ImGui.Selectable("将数量重置为 0"))
            {
                plan.Items.Each(x => x.Quantity = 0);
                plan.Items.Each(x => x.QuantitySingleTime = 0);
            }
            if(ImGui.Selectable("移除数量为 0 的物品"))
            {
                plan.Items.RemoveAll(x => x.Quantity == 0 && x.QuantitySingleTime == 0);
            }
            if(ImGuiEx.Selectable("清空列表（按住 CTRL 并点击）", enabled: ImGuiEx.Ctrl))
            {
                plan.Items.Clear();
            }
            ImGui.EndPopup();
        }
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.AngleDoubleDown, "操作"))
        {
            ImGui.OpenPopup("Ex");
        }
        ImGui.SameLine();
        ImGuiEx.InputWithRightButtonsArea("Fltr2", () =>
        {
            ImGui.InputTextWithHint("##filter", "搜索...", ref getFilter(), 100);
        }, () =>
        {
            ImGui.Checkbox("仅已选择", ref onlySelected());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f);
            ImGuiEx.EnumCombo("##cat", ref SelectedCategory, nullName: "所有分类");
            ImGuiEx.Tooltip("分类");
        });



        DragDrop.Begin();
        if(ImGuiEx.BeginDefaultTable("GCDeliveryList", ["##dragDrop", "~物品", "军队", "等级", "价格", "分类", "保留", "一次性", "##controls"]))
        {
            for(var i = 0; i < plan.Items.Count; i++)
            {
                var currentItem = plan.Items[i];
                var meta = Utils.SharedGCExchangeListings[currentItem.ItemID];
                if(onlySelected() && currentItem.Quantity == 0) continue;
                if(getFilter().Length > 0
                    && !meta.Data.Name.GetText().Contains(getFilter(), StringComparison.OrdinalIgnoreCase)
                    && !meta.Category.ToString().Equals(getFilter(), StringComparison.OrdinalIgnoreCase)
                    && !Utils.GCRanks[meta.MinPurchaseRank].Equals(getFilter(), StringComparison.OrdinalIgnoreCase)
                    ) continue;
                if(SelectedCategory != null && meta.Category != SelectedCategory.Value) continue;
                ImGui.PushID(currentItem.ID);
                ImGui.TableNextRow();
                DragDrop.SetRowColor(currentItem);
                ImGui.TableNextColumn();
                DragDrop.NextRow();
                if(ImGuiEx.IconButton(FontAwesomeIcon.AngleDoubleUp))
                {
                    new TickScheduler(() =>
                    {
                        plan.Items.Remove(currentItem);
                        plan.Items.Insert(0, currentItem);
                    });
                }
                ImGui.SameLine(0, 1);
                ImGuiEx.Tooltip("移动到顶部");
                DragDrop.DrawButtonDummy(currentItem, plan.Items, i);
                ImGui.TableNextColumn();
                if(ThreadLoadImageHandler.TryGetIconTextureWrap(meta.Data.Icon, false, out var t))
                {
                    ImGui.Image(t.Handle, new(ImGui.GetFrameHeight()));
                    ImGui.SameLine();
                }
                ImGuiEx.TextV($"{meta.Data.Name.GetText()}");
                ImGui.TableNextColumn();
                foreach(var c in Enum.GetValues<GrandCompany>().Where(x => x != GrandCompany.Unemployed))
                {
                    if(ThreadLoadImageHandler.TryGetIconTextureWrap(60870 + (int)c, false, out var ctex))
                    {
                        var trans = !meta.Companies.Contains(c);
                        if(trans) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.2f);
                        ImGui.Image(ctex.Handle, new(ImGui.GetFrameHeight()));
                        if(trans) ImGui.PopStyleVar();
                        ImGuiEx.Tooltip($"{c}" + (trans ? "（不可用）" : ""));
                        ImGui.SameLine(0, 1);
                    }
                }
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"{meta.Data.LevelItem.RowId}");
                ImGui.TableNextColumn();
                if(Svc.Data.GetExcelSheet<GrandCompanyRank>().TryGetRow(meta.MinPurchaseRank, out var rank) && ThreadLoadImageHandler.TryGetIconTextureWrap(rank.IconFlames, false, out var tex))
                {
                    ImGui.Image(tex.Handle, new(ImGui.GetFrameHeight()));
                    var rankName = Utils.GCRanks[meta.MinPurchaseRank];
                    ImGuiEx.Tooltip(rankName);
                    if(ImGuiEx.HoveredAndClicked()) getFilter() = rankName;
                    ImGui.SameLine();
                }
                ImGuiEx.TextV($"{meta.Seals}");
                ImGui.TableNextColumn();
                ImGuiEx.TextV($"{meta.Category}");
                if(ImGuiEx.HoveredAndClicked()) getFilter() = meta.Category.ToString();
                ImGui.TableNextColumn();
                if(currentItem.Data.Value.IsUnique)
                {
                    ImGuiEx.Checkbox("唯一", ref currentItem.Quantity);
                }
                else
                {
                    ImGui.SetNextItemWidth(100f.Scale());
                    ImGui.InputInt("##qty", ref currentItem.Quantity.ValidateRange(0, int.MaxValue), 0, 0);
                }
                ImGuiEx.Tooltip("选择背包中要保留的物品数量");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(100f.Scale());
                ImGui.InputInt("##qtyonetime", ref currentItem.QuantitySingleTime.ValidateRange(0, currentItem.Data.Value.IsUnique ? 1 : int.MaxValue), 0, 0);
                ImGuiEx.Tooltip("选择一次性购买的数量。任何使用此计划的角色完成购买时，都会从此值扣除数量。归零后会回到“保留”数量。");
                ImGui.TableNextColumn();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Clone))
                {
                    plan.Items.Insert(i + 1, currentItem.JSONClone());
                }
                ImGuiEx.Tooltip("复制此条目。");
                ImGui.SameLine(0, 1);
                if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                {
                    new TickScheduler(() => plan.Items.Remove(currentItem));
                }
                ImGuiEx.Tooltip($"若列表中存在多个副本则删除该物品；若只有一个副本则将其数量设为 0。");
                ImGui.PopID();
            }
            ImGui.EndTable();
        }
        DragDrop.End();
        ImGui.PopID();
    }
}
