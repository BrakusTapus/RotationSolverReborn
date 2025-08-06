﻿using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Rotations;

/// <summary>
/// Represents a custom rotation with various item-related methods.
/// </summary>
public partial class CustomRotation
{
    private static readonly BaseItem PhoenixDownItem = new(4570);

    #region Burst Medicine

    /// <summary>
    /// Gets the type of medicine.
    /// </summary>
    public abstract MedicineType MedicineType { get; }

    /// <summary>
    /// Gets the collection of available medicines.
    /// </summary>
    internal static MedicineItem[] Medicines { get; } = GetMedicines();

    private static MedicineItem[] GetMedicines()
    {
        var items = Service.GetSheet<Item>();
        var list = new List<MedicineItem>();
        foreach (var i in items)
        {
            if (i.FilterGroup == 6 && i.ItemSearchCategory.RowId == 43)
            {
                var med = new MedicineItem(i);
                if (med.Type != MedicineType.None)
                {
                    list.Add(med);
                }
            }
        }
        // Reverse the list
        int n = list.Count;
        var arr = new MedicineItem[n];
        for (int i = 0; i < n; i++)
        {
            arr[i] = list[n - i - 1];
        }
        return arr;
    }

    /// <summary>
    /// Uses the burst medicines.
    /// </summary>
    /// <param name="act">The action to be performed.</param>
    /// <param name="clippingCheck">Indicates whether to perform a clipping check.</param>
    /// <returns>True if a burst medicine was used; otherwise, false.</returns>
    public bool UseBurstMedicine(out IAction? act, bool clippingCheck = true)
    {
        act = null;

        bool isHostileTargetDummy = HostileTarget?.IsDummy() ?? false;
        bool isInHighEndDuty = DataCenter.Territory?.IsHighEndDuty ?? false;

        if (!isHostileTargetDummy && !isInHighEndDuty && DataCenter.CurrentTinctureUseType == TinctureUseType.InHighEndDuty)
        {
            return false;
        }

        if (DataCenter.CurrentTinctureUseType == TinctureUseType.Nowhere)
        {
            return false;
        }

        foreach (MedicineItem medicine in Medicines)
        {
            if (medicine.Type != MedicineType)
            {
                continue;
            }

            if (medicine.CanUse(out act, clippingCheck))
            {
                return true;
            }
        }

        return false;
    }
    #endregion

    #region MP Potions

    /// <summary>
    /// Gets the collection of available MP potions.
    /// </summary>
    internal static MpPotionItem[] MpPotions { get; } = GetMpPotions();

    private static MpPotionItem[] GetMpPotions()
    {
        var items = Service.GetSheet<Item>();
        var list = new List<MpPotionItem>();
        foreach (var i in items)
        {
            if (i.FilterGroup == 9 && i.ItemSearchCategory.RowId == 43)
            {
                list.Add(new MpPotionItem(i));
            }
        }
        // Reverse the list
        int n = list.Count;
        var arr = new MpPotionItem[n];
        for (int i = 0; i < n; i++)
        {
            arr[i] = list[n - i - 1];
        }
        return arr;
    }

    /// <summary>
    /// Uses an MP potion.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if an MP potion was used; otherwise, false.</returns>
    private static bool UseMpPotion(IAction nextGCD, out IAction? act)
    {
        MpPotionItem? best = null;
        foreach (var a in MpPotions)
        {
            if (a.CanUse(out _, true))
            {
                if (best == null || a.MaxMp >= best.MaxMp)
                {
                    best = a;
                }
            }
        }
        act = best;
        return act != null;
    }
    #endregion

    #region HP Potions

    /// <summary>
    /// Gets the collection of available HP potions.
    /// </summary>
    internal static HpPotionItem[] HpPotions { get; } = GetHpPotions();

    private static HpPotionItem[] GetHpPotions()
    {
        var items = Service.GetSheet<Item>();
        var list = new List<HpPotionItem>();
        foreach (var i in items)
        {
            if (i.FilterGroup == 8 && i.ItemSearchCategory.RowId == 43)
            {
                list.Add(new HpPotionItem(i));
            }
        }
        // Reverse the list
        int n = list.Count;
        var arr = new HpPotionItem[n];
        for (int i = 0; i < n; i++)
        {
            arr[i] = list[n - i - 1];
        }
        return arr;
    }

    /// <summary>
    /// Uses an HP potion.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if an HP potion was used; otherwise, false.</returns>
    private static bool UseHpPotion(IAction nextGCD, out IAction? act)
    {
        HpPotionItem? best = null;
        foreach (var a in HpPotions)
        {
            if (a.CanUse(out _, true))
            {
                if (best == null || a.MaxHp >= best.MaxHp)
                {
                    best = a;
                }
            }
        }
        act = best;
        return act != null;
    }
    #endregion
}