using AutoRetainerAPI.Configuration;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using ECommons.ChatMethods;
using ECommons.ExcelServices;
using ECommons.EzContextMenu;
using ECommons.Interop;
using Lumina.Excel.Sheets;
using UIColor = ECommons.ChatMethods.UIColor;

namespace AutoRetainer.Internal;

internal unsafe class ContextMenuManager
{
    private SeString Prefix = new SeStringBuilder().AddUiForeground(" ", 539).Build();

    public ContextMenuManager()
    {
        ContextMenuPrefixRemover.Initialize();
        Svc.ContextMenu.OnMenuOpened += ContextMenu_OnMenuOpened;
    }

    private void ContextMenu_OnMenuOpened(IMenuOpenedArgs args)
    {
        if(Data == null) return;
        if(!Data.GetIMSettings().IMEnableContextMenu) return;
        if(args.MenuType == ContextMenuType.Inventory && args.Target is MenuTargetInventory inv && inv.TargetItem != null)
        {
            var id = inv.TargetItem.Value.ItemId % 1_000_000;
            if(id != 0 && inv.TargetItem.Value.ItemId < 2_000_000)
            {
                if(Data.GetIMSettings(true).IMProtectList.Contains(id))
                {
                    args.AddMenuItem(new MenuItem()
                    {
                        Name = new SeStringBuilder().Append(Prefix).AddText("= 物品已被保护 =").Build(),
                        OnClicked = (a) =>
                        {
                            if(IsKeyPressed([LimitedKeys.LeftControlKey, LimitedKeys.RightControlKey]) && IsKeyPressed([LimitedKeys.RightShiftKey, LimitedKeys.LeftShiftKey]))
                            {
                                var t = $"物品 {ExcelItemHelper.GetName(id)} 已从保护列表移除";
                                Notify.Success(t);
                                ChatPrinter.Red("[AutoRetainer] " + t);
                                Data.GetIMSettings(true).IMProtectList.Remove(id);
                            }
                            else
                            {
                                Notify.Error($"按住 CTRL+SHIFT 并点击以移除此物品的保护");
                            }
                        }
                    }.RemovePrefix());
                }
                else
                {
                    var data = Svc.Data.GetExcelSheet<Item>().GetRow(id);
                    if(Data.GetIMSettings(true).IMAutoVendorSoft.Contains(id))
                    {
                        args.AddMenuItem(new MenuItem()
                        {
                            Name = new SeStringBuilder().Append(Prefix).AddUiForeground("- 从快速探险出售列表移除", (ushort)UIColor.Orange).Build(),
                            OnClicked = (a) =>
                            {
                                Data.GetIMSettings(true).IMAutoVendorSoft.Remove(id);
                                Notify.Info($"物品 {ExcelItemHelper.GetName(id)} 已从快速探险出售列表移除");
                            }
                        }.RemovePrefix());
                    }
                    else if(data.PriceLow > 0)
                    {
                        args.AddMenuItem(new MenuItem()
                        {
                            Name = new SeStringBuilder().Append(Prefix).AddUiForeground("+ 加入快速探险出售列表", (ushort)UIColor.Yellow).Build(),
                            OnClicked = (a) =>
                            {
                                if(Data.GetIMSettings(true).AddItemToList(IMListKind.SoftSell, id, out var error))
                                {
                                    Notify.Success($"物品 {ExcelItemHelper.GetName(id)} 已加入快速探险出售列表");
                                }
                                else
                                {
                                    Notify.Error(error);
                                }
                            }
                        }.RemovePrefix());
                    }

                    if(Data.GetIMSettings(true).IMAutoVendorHard.Contains(id))
                    {
                        args.AddMenuItem(new MenuItem()
                        {
                            Name = new SeStringBuilder().Append(Prefix).AddUiForeground("- 从无条件出售列表移除", (ushort)UIColor.Orange).Build(),
                            OnClicked = (a) =>
                            {
                                Data.GetIMSettings(true).IMAutoVendorHard.Remove(id);
                                Notify.Success($"物品 {ExcelItemHelper.GetName(id)} 已从无条件出售列表移除");
                            }
                        }.RemovePrefix());
                    }
                    else if(data.PriceLow > 0)
                    {
                        args.AddMenuItem(new MenuItem()
                        {
                            Name = new SeStringBuilder().Append(Prefix).AddUiForeground("+ 加入无条件出售列表", (ushort)UIColor.Yellow).Build(),
                            OnClicked = (a) =>
                            {
                                if(Data.GetIMSettings(true).AddItemToList(IMListKind.HardSell, id, out var error))
                                {
                                    Notify.Success($"物品 {ExcelItemHelper.GetName(id)} 已加入无条件出售列表");
                                }
                                else
                                {
                                    Notify.Error(error);
                                }
                            }
                        }.RemovePrefix());
                    }

                    if(Data.GetIMSettings(true).IMDiscardList.Contains(id))
                    {
                        args.AddMenuItem(new MenuItem()
                        {
                            Name = new SeStringBuilder().Append(Prefix).AddUiForeground("- 从丢弃列表移除", (ushort)UIColor.Orange).Build(),
                            OnClicked = (a) =>
                            {
                                Data.GetIMSettings(true).IMDiscardList.Remove(id);
                                Notify.Success($"物品 {ExcelItemHelper.GetName(id)} 已从丢弃列表移除");
                            }
                        }.RemovePrefix());
                    }
                    else
                    {
                        args.AddMenuItem(new MenuItem()
                        {
                            Name = new SeStringBuilder().Append(Prefix).AddUiForeground("+ 加入丢弃列表", (ushort)UIColor.Yellow).Build(),
                            OnClicked = (a) =>
                            {
                                if(Data.GetIMSettings(true).AddItemToList(IMListKind.Discard, id, out var error))
                                {
                                    Notify.Success($"物品 {ExcelItemHelper.GetName(id)} 已加入丢弃列表");
                                }
                                else
                                {
                                    Notify.Error(error);
                                }
                            }
                        }.RemovePrefix());
                    }

                    args.AddMenuItem(new MenuItem()
                    {
                        Name = new SeStringBuilder().Append(Prefix).AddText("保护物品，避免自动操作").Build(),
                        OnClicked = (a) =>
                        {
                            if(Data.GetIMSettings(true).AddItemToList(IMListKind.Protect, id, out var error))
                            {
                                Notify.Success($"{ExcelItemHelper.GetName(id)} 已加入保护列表");
                            }
                            else
                            {
                                Notify.Error(error);
                            }
                        }
                    }.RemovePrefix());
                }
            }
        }
    }

    public void Dispose()
    {
        Svc.ContextMenu.OnMenuOpened -= ContextMenu_OnMenuOpened;
    }
}
