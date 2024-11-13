using Lumina.Excel.Sheets;

namespace RotationSolver.GameData.Getters.Actions;

internal class ActionSingleRotationGetter(Lumina.GameData gameData, ClassJob job)
    : ActionRotationGetterBase(gameData)
{
    public override bool IsDutyAction => false;

    protected override bool AddToList(Lumina.Excel.Sheets.Action item)
    {
        if (!base.AddToList(item)) return false;

        var category = item.ClassJobCategory.Value;

        if (!category.IsSingleJobForCombat()) return false;

        var jobName = job.Abbreviation.ExtractText();
        return (bool?)category.GetType().GetRuntimeProperty(jobName)?.GetValue(category) ?? false;
    }
}

internal abstract class ActionMultiRotationGetter(Lumina.GameData gameData)
    : ActionRotationGetterBase(gameData)
{
    protected static bool IsADutyAction(Lumina.Excel.Sheets.Action action)
    {
        return !action.IsRoleAction && !action.IsPvP && action.ActionCategory.RowId
            is not 10 and not 11 // Not System
            and not 9 and not 15; // Not LB.
    }

    protected override bool AddToList(Lumina.Excel.Sheets.Action item)
    {
        if (!base.AddToList(item)) return false;

        var category = item.ClassJobCategory.Value;
        if (category == null) return false;

        if (category.IsSingleJobForCombat()) return false;

        return true;
    }
}

internal class ActionDutyRotationGetter(Lumina.GameData gameData)
    : ActionMultiRotationGetter(gameData)
{
    public override bool IsDutyAction => true;

    protected override bool AddToList(Lumina.Excel.Sheets.Action item)
    {
        if (!base.AddToList(item)) return false;
        return IsADutyAction(item);
    }
}

internal class ActionRoleRotationGetter(Lumina.GameData gameData)
    : ActionMultiRotationGetter(gameData)
{
    public override bool IsDutyAction => false;

    protected override bool AddToList(Lumina.Excel.Sheets.Action item)
    {
        if (!base.AddToList(item)) return false;
        return !IsADutyAction(item);
    }
}
