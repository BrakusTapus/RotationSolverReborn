using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Rotations;

/// <summary>
/// Represents a custom rotation with various item-related methods.
/// </summary>
partial class CustomRotation
{
    #region Burst Medicine

    /// <summary>
    /// Gets the type of medicine.
    /// </summary>
    public abstract MedicineType MedicineType { get; }

    /// <summary>
    /// Gets the collection of available medicines.
    /// </summary>
    internal static MedicineItem[] Medicines { get; } = Service.GetSheet<Item>()
        .Where(i => i.FilterGroup == 6 && i.ItemSearchCategory.RowId == 43)
        .Select(i => new MedicineItem(i))
        .Where(i => i.Type != MedicineType.None).Reverse().ToArray();

    /// <summary>
    /// Uses the burst medicines.
    /// </summary>
    /// <param name="act">The action to be performed.</param>
    /// <param name="clippingCheck">Indicates whether to perform a clipping check.</param>
    /// <returns>True if a burst medicine was used; otherwise, false.</returns>
    protected bool UseBurstMedicine(out IAction? act, bool clippingCheck = true)
    {
        act = null;

        bool isHostileTargetDummy = HostileTarget?.IsDummy() ?? false;
        bool isInHighEndDuty = DataCenter.IsInHighEndDuty;

        if (!isHostileTargetDummy && !isInHighEndDuty && DataCenter.RightNowTinctureUseType == TinctureUseType.InHighEndDuty)
        {
            return false;
        }

        if (DataCenter.RightNowTinctureUseType == TinctureUseType.Nowhere) return false;

        foreach (var medicine in Medicines)
        {
            if (medicine.Type != MedicineType) continue;

            if (medicine.CanUse(out act, clippingCheck)) return true;
        }

        return false;
    }
    #endregion

    #region MP Potions

    /// <summary>
    /// Gets the collection of available MP potions.
    /// </summary>
    internal static MpPotionItem[] MpPotions { get; } = Service.GetSheet<Item>()
        .Where(i => i.FilterGroup == 9 && i.ItemSearchCategory.RowId == 43)
        .Select(i => new MpPotionItem(i)).Reverse().ToArray();

    /// <summary>
    /// Uses an MP potion.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if an MP potion was used; otherwise, false.</returns>
    private static bool UseMpPotion(IAction nextGCD, out IAction? act)
    {
        var acts = from a in MpPotions
                   where a.CanUse(out _, true)
                   orderby a.MaxMp
                   select a;

        act = acts.LastOrDefault();
        return act != null;
    }
    #endregion

    #region HP Potions

    /// <summary>
    /// Gets the collection of available HP potions.
    /// </summary>
    internal static HpPotionItem[] HpPotions { get; } = Service.GetSheet<Item>()
        .Where(i => i.FilterGroup == 8 && i.ItemSearchCategory.RowId == 43)
        .Select(i => new HpPotionItem(i)).Reverse().ToArray();

    /// <summary>
    /// Uses an HP potion.
    /// </summary>
    /// <param name="nextGCD">The next GCD action.</param>
    /// <param name="act">The action to be performed.</param>
    /// <returns>True if an HP potion was used; otherwise, false.</returns>
    private static bool UseHpPotion(IAction nextGCD, out IAction? act)
    {
        var acts = from a in HpPotions
                   where a.CanUse(out _, true)
                   orderby a.MaxHp
                   select a;

        act = acts.LastOrDefault();
        return act != null;
    }
    #endregion
}