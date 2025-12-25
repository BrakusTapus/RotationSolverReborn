using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using RotationSolver.UI;
using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Ranged;

[Rotation("KirboMCH", CombatType.PvP, GameVersion = "7.4", Description = "Kirbo's Machinist PvP Rotation!")]
[SourceCode(Path = "main/ExtraRotations/Ranged/KirboMCHPvp.cs")]
[ExtraRotation]
public sealed class KirboMCHPvp : MachinistRotation
{
    #region Properties
    private static bool HasActiveGuard => Player.HasStatus(true, StatusID.Guard);

    /// <summary>
    ///     Gets the current Heat Stacks.
    /// </summary>
    private static byte PvP_OverheatedStacks
    {
        get
        {
            byte pvP_OverheatedStacks = CustomRotation.Player.StatusStack(true, StatusID.Heat);
            if (pvP_OverheatedStacks != byte.MaxValue)
            {
                return pvP_OverheatedStacks;
            }

            return 3;
        }
    }
    private static bool IsPvPOverheated => Player.HasStatus(true, StatusID.Overheated_3149);
    private static float OverheatedStatusTime => Player.StatusTime(true, StatusID.Overheated_3149);
    private static bool PlayerHasWildfire => Player.HasStatus(true, StatusID.Wildfire_2018);
    private static float PlayerWildfireStatusTime => Player.StatusTime(true, StatusID.Wildfire_2018);
    private static bool PvPTargetHasWildfire => CurrentTarget != null && CurrentTarget.HasStatus(true, StatusID.Wildfire_1323);
    private static float PvPTargetWildfireStatusTime => CurrentTarget!.StatusTime(true, StatusID.Wildfire_1323);
    private static float AnalysisStatusTime => Player.StatusTime(true, StatusID.Analysis);
    private static bool PlayerHasBravery => Player.HasStatus(true, StatusID.Bravery);
    private enum LBMethod
    {
        [Description("MCH LB NEW")] MCHLBNEW,
        [Description("MCH LB 4")] MCHLB4
    }
    #endregion

    #region Config Options
    [RotationConfig(CombatType.PvP, Name = "GuardCancel")]
    private bool GuardCancel { get; set; } = true;

    [RotationConfig(CombatType.PvP, Name = "Emergency Healing")]
    private bool EmergencyHealing { get; set; } = false;

    /* TODO should consider removing this
    //[RotationConfig(CombatType.PvP, Name = "LowHPNoBlastCharge")]
    //public bool LowHPNoBlastCharge { get; set; } = true;

    //[RotationConfig(CombatType.PvP, Name = "LowHPNoBlastChargeThreshold")]
    //public int LowHPNoBlastChargeThreshold { get; set; } = 15000;
    */

    [RotationConfig(CombatType.PvP, Name = "AnalysisOnDrill")]
    private bool AnalysisOnDrill { get; set; } = true;

    [RotationConfig(CombatType.PvP, Name = "AnalysisOnAirAnchor")]
    private bool AnalysisOnAirAnchor { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "AnalysisOnBioBlaster")]
    private bool AnalysisOnBioBlaster { get; set; } = true;

    [RotationConfig(CombatType.PvP, Name = "AnalysisOnChainsaw")]
    private bool AnalysisOnChainsaw { get; set; } = true;

    [RotationConfig(CombatType.PvP, Name = "Auto Bishop")]
    private bool AutoBishop { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "Use Purify")]
    public bool UsePurifyPvP { get; set; } = false;

    [RotationConfig(CombatType.PvP, Name = "LB method")]
    private LBMethod LBMethodPicker { get; set; } = LBMethod.MCHLBNEW;
    #endregion Rotation Config

    #region oGCD Logic
    [RotationDesc(DescType.BurstActions)][RotationDesc(ActionID.WildfirePvP)]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        // Should prevent any actions if the option 'guardCancel' is enabled and Player has the Guard buff up
        if (GuardCancel && HasActiveGuard)
        {
            return false;
        }

        if (DoPurify(out act))
        {
            return true;
        }

        if (EmergencyHealing && EmergencyLowHP(out act))
        {
            return true;
        }

        // Bishop Turret should be used off cooldown
        // Note: Could prolly be improved using 'ChoiceTarget' in the IBaseAction
        if (AutoBishop && BishopAutoturretPvP.CanUse(out act, skipAoeCheck: true, usedUp: true)) // Without MustUse, returns CastType 7 invalid // BishopAutoturretPvP.action.CastType
        {
            return true;
        }

        // Eagle Eye Shot
        if (UseEagleEyeShot(out act))
        {
            return true;
        }

        // Bravery
        if (BraveryPvP.CanUse(out act) && NumberOfAllHostilesInRange > 0 && nextGCD.IsTheSameTo(false, ActionID.FullMetalFieldPvP, ActionID.DrillPvP, (ActionID)29415, ActionID.ChainSawPvP))
        {
            return true;
        }

        // Analysis should be used on any of the tools depending on which options are enabled
        if (AnalysisPvP.CanUse(out act, usedUp: true) && NumberOfAllHostilesInRange > 0 && !Player.HasStatus(true, StatusID.Analysis) && !IsLastAction(ActionID.AnalysisPvP))
        {
            if (AnalysisOnDrill && nextGCD.IsTheSameTo(false, ActionID.DrillPvP) && Player.HasStatus(true, StatusID.DrillPrimed))
            {
                return true;
            }
            if (AnalysisOnChainsaw && nextGCD.IsTheSameTo(false, ActionID.ChainSawPvP) && Player.HasStatus(true, StatusID.ChainSawPrimed))
            {
                return true;
            }
            if (AnalysisOnBioBlaster && nextGCD.IsTheSameTo(false, ActionID.BioblasterPvP) && Player.HasStatus(true, StatusID.BioblasterPrimed))
            {
                return true;
            }
            if (AnalysisOnAirAnchor && nextGCD.IsTheSameTo(false, ActionID.AirAnchorPvP) && Player.HasStatus(true, StatusID.AirAnchorPrimed))
            {
                return true;
            }
        }

        // wildfire
        if (nextGCD.IsTheSameTo(false, FullMetalFieldPvP) && WildfirePvP.CanUse(out act))
        {
            return true;
        }

        // WildfirePvP Should be used only right after getting the 5th Heat Stacks
        //if ((IsLastGCD((ActionID)41469) || IsPvPOverheated) &&
        //    !Player.WillStatusEnd(2f, true, StatusID.Overheated_3149) &&
        //    CurrentTarget != null &&
        //    CurrentTarget.GetHealthRatio() >= 0.5 &&
        //    !CustomRotationEx.IsPvPNpc(CurrentTarget.Name.ToString())
        //    && WildfirePvP.CanUse(out act))
        //{
        //    return true;
        //}

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.GuardPvP)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (GuardCancel && HasActiveGuard)
        {
            return false;
        }

        return base.DefenseSingleAbility(nextGCD, out action);
    }

    [RotationDesc(ActionID.RecuperatePvP)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (EmergencyHealing && EmergencyLowHP(out act))
        {
            return true;
        }
        return base.HealSingleAbility(nextGCD, out act);
    }
    #endregion oGCD Logic

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        act = null;
        // Should prevent any actions if the option 'guardCancel' is enabled and Player has the Guard buff up
        if (GuardCancel && HasActiveGuard)
        {
            return false;
        }

        if (EmergencyHealing && EmergencyLowHP(out act))
        {
            return true;
        }

        if (DoPurify(out act))
        {
            return true;
        }

        // Eagle Eye Shot
        if (UseEagleEyeShot(out act))
        {
            return true;
        }

        // early analysis
        if (UseEarlyAnalysis(out act))
        {
            return true;
        }

        // New LB logic
        if (TryUseLB(out act))
        {
            return true;
        }


        // Drill
        if (!IsPvPOverheated && DrillPvP.CanUse(out act, usedUp: true) && Player.HasStatus(true, StatusID.DrillPrimed))
        {
            return true;
        }

        // FullMetalField
        if (FullMetalFieldPvP.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        // Uses BioBlaster automatically when a Target is in range
        if (!IsPvPOverheated && BioblasterPvP.CanUse(out act, usedUp: true, skipAoeCheck: true) && Player.HasStatus(true, StatusID.BioblasterPrimed))
        {
            return true;
        }

        // BlazingShot
        if (BlazingShotPvP.CanUse(out act))
        {
            if (Player.HasStatus(true, StatusID.Overheated_3149))
            {
                return true;
            }
        }

        // FullMetalField
        if (!IsPvPOverheated && FullMetalFieldPvP.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        // Scattergun is used if Player is not overheated and available
        if (!IsPvPOverheated && ScattergunPvP.CanUse(out act, usedUp: true, skipAoeCheck: true))
        {
            return true;
        }

        // Chainsaw
        if (!IsPvPOverheated && ChainSawPvP.CanUse(out act, usedUp: false, skipAoeCheck: true) && Player.HasStatus(true, StatusID.ChainSawPrimed))
        {
            return true;
        }

        // Air Anchor is used if Player is not overheated and available
        if (!IsPvPOverheated && AirAnchorPvP.CanUse(out act) && Player.HasStatus(true, StatusID.AirAnchorPrimed))
        {
            return true;
        }

        // Drill old
        if (!IsPvPOverheated && DrillPvP.CanUse(out act, usedUp: true) && Player.HasStatus(true, StatusID.DrillPrimed))
        {
            return true;
        }

        // Blast Charge is used if available
        // Note: Stop Using Blast Charge if Player's HP is low + moving + not overheated (since our movement slows down a lot we do this to be able retreat)
        if (BlastChargePvP.CanUse(out act/*, skipCastingCheck: true*/) /*&& CurrentTarget != null && CurrentTarget.DistanceToPlayer() < 20*/)
        {
            //if (Player.CurrentHp <= LowHPNoBlastChargeThreshold && NumberOfAllHostilesInRange > 0 && LowHPNoBlastCharge && IsMoving) // Maybe add InCombat as well
            //{
            //    return false;
            //}
            //else
            //{
            //    return true;
            //}
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion GCD Logic

    #region Extra Methods

    #region Common
    // TODO can prolly be removed
    private bool EmergencyLowHP(out IAction? act)
    {
        if (Player.HasStatus(true, StatusID.Guard))
        {
            act = null;
            return false;
        }

        //if (Player.CurrentHp <= 25000 && GuardPvP.CanUse(out _) && !Player.HasStatus(true, StatusID.Guard) && NumberOfAllHostilesInMaxRange >= 1)
        //{
        //    return GuardPvP.CanUse(out act);
        //}

        if (Player.CurrentMp == Player.MaxMp && Player.CurrentHp <= 37500 /*&& !Player.HasStatus(true, StatusID.Guard)*/ && RecuperatePvP.CanUse(out _))
        {
            return RecuperatePvP.CanUse(out act);
        }

        if (Player.CurrentMp >= 7500 && Player.CurrentHp <= 37500 /*&& !Player.HasStatus(true, StatusID.Guard)*/ && RecuperatePvP.CanUse(out _))
        {
            return RecuperatePvP.CanUse(out act);
        }

        if (Player.CurrentMp >= 5000 && Player.CurrentHp <= 32000 /*&& !Player.HasStatus(true, StatusID.Guard)*/ && RecuperatePvP.CanUse(out _))
        {
            return RecuperatePvP.CanUse(out act);
        }

        if (Player.CurrentMp >= 2500 && Player.CurrentHp <= 25000 /*&& GuardPvP.Cooldown.IsCoolingDown && !Player.HasStatus(true, StatusID.Guard)*/ && RecuperatePvP.CanUse(out _))
        {
            return RecuperatePvP.CanUse(out act);
        }
        act = null;
        return false;
    }

    // Purify logic
    private bool DoPurify(out IAction? action)
    {
        action = null;

        if (!UsePurifyPvP)
        {
            return false;
        }

        List<int> purifiableStatusesIDs = new()
        {
          //1343, // Stun (Gets cleansed right before debuff falls off. When Purify is used manually it can be used immediately after player gets stunned)
            1344, // Heavy
            1345, // Bind
            1347, // Silence
            3219, // Deep Freeze
            3085  // Miracle of Nature
        };

        // Bail early if no purifiable status is present
        if (!purifiableStatusesIDs.Any(id => Player.HasStatus(false, (StatusID)id)))
        {
            return false;
        }

        // Basic resource info
        uint currentHP = Player.CurrentHp;
        uint maxHP = Player.MaxHp;
        uint currentMP = Player.CurrentMp;
        uint maxMP = Player.MaxMp;

        const int purifyCost = 2500;
        const int recuperateCost = 2500;

        // HP % thresholds
        double hpPercent = (double)currentHP / maxHP;

        // Decision logic:
        // 1. If HP < 40% and you don’t have enough MP for BOTH Purify + Recuperate → skip Purify, keep MP to heal
        if (hpPercent < 0.40 && currentMP < recuperateCost * 2)
        {
            return false;
        }

        // 2. If HP is very low (<25%), always prioritize saving for Recuperate
        if (hpPercent < 0.25 && currentMP < recuperateCost + purifyCost)
        {
            return false;
        }

        // 3. Only use Purify if MP >= Purify cost
        if (currentMP < purifyCost)
        {
            return false;
        }

        // 4. If all checks pass, we can use Purify
        return PurifyPvP.CanUse(out action);
    }

    // Checks if player has Guard
    private static bool IsGuarded() => Player.HasStatus(true, StatusID.Guard);

    // Checks if target has Guard
    private static bool TargetHasGuard(IBattleChara target) => target.HasStatus(false, StatusID.Guard);

    // Checks if an object is a player character
    private static bool IsPlayerCharacter(IBattleChara battleChara)
    {
        return battleChara.GetObjectKind() == ObjectKind.Player;
    }

    // Checks amount of enemies targeting player
    private static bool EnemiesTargetingSelf(int numEnemies) => DataCenter.AllHostileTargets.Count(o => o.IsTargetable && !o.IsDead && o.TargetObjectId == Svc.Objects.LocalPlayer?.GameObjectId) >= numEnemies;

    // check out the guard logic https://github.com/awgil/ffxiv_bossmod/blob/master/BossMod/Autorotation/Utility/RolePvPUtility.cs#L60
    #endregion

    #region Limit Break
    private bool TryUseLB(out IAction? act)
    {
        act = default;

        switch (LBMethodPicker)
        {
            case LBMethod.MCHLBNEW:
                return UseMCHLBNEW(out act);

            case LBMethod.MCHLB4:
                return UseMCHLB4(out act);

            default:
                return false;
        }
    }

    // TODO compare with 'UseMCHLB4' to find out which method is better
    private bool UseMCHLBNEW(out IAction? action)
    {

        action = null;

        if (CurrentLimitBreakLevel == 0)
        {
            return false;
        }

        // https://na.finalfantasyxiv.com/lodestone/playguide/contentsguide/frontline/4/
        const int EstimatedLBDamage = 28000;
        const int MinEffectiveHp = (int)(EstimatedLBDamage * 0.5); // ~20000

        IBattleChara? target = CustomRotation.AllHostileTargets
        .Where(obj =>
            obj.CurrentHp >= MinEffectiveHp &&
            obj.CurrentHp <= 35000 &&
            IsPlayerCharacter(obj) &&
            !obj.IsJobCategory(JobRole.Tank) &&
            !obj.IsJobCategory(JobRole.Melee) &&
                (
                 obj.IsJobCategory(JobRole.Healer) ||
                 obj.IsJobCategory(JobRole.RangedPhysical) ||
                 obj.IsJobCategory(JobRole.RangedMagical)
                ) &&
            obj.DistanceToPlayer() <= 50 &&
            !obj.HasStatus(false, StatusID.Guard)
            )
        .OrderBy(obj => obj.CurrentHp)
        .FirstOrDefault();

        if (target == null)
        {
            return false;
        }

        if (!MarksmansSpitePvP.CanUse(out action))
        {
            return false;
        }

        MarksmansSpitePvP.Target = new TargetResult(target, [target], target.Position);
        return true;
    }

    // class-level: only include roles you actually want to consider for LB
    private static readonly Dictionary<JobRole, (int minHp, int maxHp)> LbTargetThresholds = new()
    {
        { JobRole.Healer, (17000, 28000) },
        { JobRole.RangedMagical, (17000, 27000) },
        { JobRole.RangedPhysical, (17000, 30000) },
    };

    // TODO compare with 'UseMCHLBNEW' to find out which method is better
    private bool UseMCHLB4(out IAction? action)
    {
        action = null;

        if (CurrentLimitBreakLevel == 0)
            return false;

        if (IsGuarded())
            return false;

        if (!MarksmansSpitePvP.CanUse(out action))
            return false;

        // Filter only allowed job roles (Healer / RangedMagical / RangedPhysical) via threshold map
        var candidates = CustomRotation.AllHostileTargets
        .Where(obj => obj.IsTargetable)
        .Where(obj => obj.DistanceToPlayer() <= 50)
        .Where(obj => !TargetHasGuard(obj))
        .Where(obj =>
        {
            foreach (var kv in LbTargetThresholds)
            {
                if (obj.IsJobCategory(kv.Key))
                {
                    var (minHp, maxHp) = kv.Value;
                    return obj.CurrentHp >= minHp && obj.CurrentHp <= maxHp;
                }
            }
            return false; // exclude any job role not in the dictionary (e.g., Melee/Tank)
        })
        .OrderBy(obj => obj.CurrentHp)
        .ToList();

        if (candidates.Count == 0)
            return false;

        IBattleChara best = candidates.First();
        MarksmansSpitePvP.Target = new TargetResult(best, new[] { best }, best.Position);
        return true;
    }
    #endregion

    // Eagle Eye Shot Logic
    private bool UseEagleEyeShot(out IAction? action)
    {

        action = null;

        // https://na.finalfantasyxiv.com/lodestone/playguide/contentsguide/frontline/4/
        const int EstimatedDamage = 12000;
        const int MinEffectiveHp = (int)(EstimatedDamage * 0.1); // ~1200

        IBattleChara? target = CustomRotation.AllHostileTargets
        .Where(obj =>
            obj.CurrentHp >= MinEffectiveHp &&
            !obj.IsJobCategory(JobRole.Tank) &&
            !obj.IsJobCategory(JobRole.Melee) &&
            (obj.IsJobCategory(JobRole.Healer) ||
             obj.IsJobCategory(JobRole.RangedPhysical) ||
             obj.IsJobCategory(JobRole.RangedMagical)) &&
            obj.DistanceToPlayer() <= 40)
        .OrderBy(obj => obj.CurrentHp)
        .FirstOrDefault();

        if (target == null)
        {
            return false;
        }

        if (!EagleEyeShotPvP.CanUse(out action))
        {
            return false;
        }

        EagleEyeShotPvP.Target = new TargetResult(target, [target], target.Position);
        return true;
    }

    // Original idea was to check if an enemy is using MS on player, silly goose me didn't realize that MCH pvp LB does, in fact, not have a cast. So need to rework this idea.
    private bool ShouldGuardAgainstLB(out IAction? action)
    {
        action = null;

        // Exit early if Guard is on cooldown or already active
        if (!GuardPvP.CanUse(out _) || GuardPvP.Cooldown.IsCoolingDown || Player.HasStatus(true, StatusID.Guard))
        {
            return false;
        }

        foreach (IBattleChara enemy in CustomRotation.AllHostileTargets)
        {
            uint marksmanSpite = 29415;
            if (enemy != null &&
                enemy.IsJobs(ECommons.ExcelServices.Job.MCH) &&
                enemy.TargetObjectId == Player.GameObjectId &&
                enemy.CastActionId == marksmanSpite)
            {
                if (GuardPvP.CanUse(out action))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Early Analysis use
    private bool UseEarlyAnalysis(out IAction? action)
    {
        action = null;

        if (InCombat || NumberOfAllHostilesInMaxRange == 0 || Player.HasStatus(true, StatusID.Analysis))
        {
            return false;
        }

        if (Player.HasStatus(true, StatusID.DrillPrimed))
        {
            if (AnalysisPvP.CanUse(out action))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region MCH LB
    // Animation lock time for Marksman's Spite is ~1.60s (1600ms)
    private static IBaseAction MarksmansSpitePvP { get; } = new BaseAction((ActionID)29415);
    //private IBaseAction MarksmansSpitePvP2 => _MarksmansSpitePvPCreator.Value;
    //private readonly Lazy<IBaseAction> _MarksmansSpitePvPCreator = new Lazy<IBaseAction>(delegate
    //{
    //    IBaseAction action40 = new BaseAction((ActionID)29415);
    //    ActionSetting setting40 = action40.Setting;
    //    setting40.RotationCheck = () =>
    //    CurrentLimitBreakLevel == 1 &&
    //    action40.Target.Target.CurrentHp <= 30000 &&
    //        (
    //            action40.Target.Target.IsJobCategory(JobRole.RangedMagical) ||
    //            action40.Target.Target.IsJobCategory(JobRole.RangedPhysical) ||
    //            action40.Target.Target.IsJobCategory(JobRole.Healer)
    //        );
    //    setting40.TargetType = TargetType.LowHP;
    //    action40.Setting = setting40;
    //    return action40;
    //});
    #endregion

    #region Status Display
    public override bool ShowStatus => true;
    public override void DisplayRotationStatus()
    {
        //Get available width in the current ImGui window
        float availableWidth = ImGui.GetContentRegionAvail().X;
        using (ImRaii.IEndObject child = ImRaii.Child("playerinfo", new Vector2((availableWidth / 2), 200), true))
        {
            if (child.Success)
            {
                //var test = Svc.Targets.MouseOverTarget;
                //ImGui.Text("MO name: " + test?.Name);
                //ImGui.Text($"Target is PC: {(CurrentTarget != null && IsPlayerCharacter(CurrentTarget) ? "Yes" : "No")}");
                ImGui.Text("Player HPP: " + Player.GetHealthRatio());
                ImGui.Text($"Current LB Method: {typeof(LBMethod).GetMember(LBMethodPicker.ToString())[0].GetCustomAttribute<DescriptionAttribute>()?.Description ?? LBMethodPicker.ToString()}");
                ImGui.Text("LimitBreakLevel: " + CurrentLimitBreakLevel);
                ImguiTooltips.HoveredTooltip("CurrentUnits: " + CurrentCurrentUnits);
                ImGui.NewLine();

                ImGui.Text("HeatStacks: " + PvP_OverheatedStacks);
                ImGui.Text("Status Time Analysis: " + AnalysisStatusTime.ToString("F2") + "s");
                ImGui.NewLine();

                ImGui.Text("IsPvPOverheated (Player): " + IsPvPOverheated);
                ImGui.Text("Overheated StatusTime: " + OverheatedStatusTime.ToString("F2") + "s");
                ImGui.NewLine();

                ImGui.Text("PlayerHasWildfire: " + PlayerHasWildfire);
                ImGui.Text("PlayerWildfireStatusTime: " + PlayerWildfireStatusTime.ToString("F2") + "s");
                ImGui.NewLine();

                ImGui.Text("PvPTargetHasWildfire: " + PvPTargetHasWildfire);
                ImGui.Text("PvPTargetWildfireStatusTime: " + PvPTargetWildfireStatusTime.ToString("F2") + "s");
                ImGui.NewLine();

                ImGui.Text("BlastChargePvP Target: " + BlastChargePvP.Target.Target?.ToString());
                ImGui.Text("BishopAutoturretPvP Target: " + BishopAutoturretPvP.Target.Target?.ToString());
                ImGui.Text("BioblasterPvP Target: " + BioblasterPvP.Target.Target?.ToString());
                ImGui.NewLine();

                ImGui.TextColored(ImGuiColors.DalamudViolet, $"Player Is Casting: {Player.IsCasting}");

                ImGui.Text($"Player Cast Action ID: {(Player.IsCasting ? Player.CastActionId.ToString() : "N/A")}");
                ImGui.Text($"Player Cast Action ID: " + Player.CastActionId.ToString());

                ImGui.Text($"Player Targeting Player: {Player.TargetObject?.GameObjectId == Player.GameObjectId}");
                ImGui.Text($"TargetObject GameObjectId: {Player.TargetObject?.GameObjectId.ToString()}");
                ImGui.Text($"Player GameObjectId: {Player.GameObjectId.ToString()}");
                ImGui.NewLine();
            }
        }
        ImGui.SameLine();
        using (ImRaii.IEndObject child2 = ImRaii.Child("targetinfo", new Vector2(((availableWidth / 2) - 20), 200), true))
        {
            if (child2.Success)
            {
                if (CurrentTarget != null)
                {
                    ImGui.Text($"Current Target Name: {CurrentTarget.Name}");
                    ImGui.Text("Target HP ratio: " + CurrentTarget.GetHealthRatio());
                    ImGui.Text("Distance: " + CurrentTarget.DistanceToPlayer().ToString("F1") + "y");
                    ImGui.NewLine();

                    ImGui.Text($"Current Target Is Casting: {CurrentTarget.IsCasting}");

                    ImGui.Text($"Current Target Cast Action ID: {(CurrentTarget.IsCasting ? CurrentTarget.CastActionId.ToString() : "N/A")}");

                    ImGui.Text($"Current Target Targeting Player: {CurrentTarget.TargetObject?.GameObjectId == Player.GameObjectId}");
                    ImGui.Text($"Current Target GameObjectId: {CurrentTarget.GameObjectId.ToString()}");
                    ImGui.Text($"TargetObject's GameObjectId: {CurrentTarget.TargetObject?.GameObjectId.ToString()}");
                }
                else
                {
                    ImGui.TextColored(ImGuiColors.DalamudRed, "We don't have a target!");
                }
                ImGui.NewLine();
            }
        }
        foreach (IBattleChara enemy in CustomRotation.AllHostileTargets)
        {
            if (enemy == null) continue;

            string header = $"Name: {enemy.Name}, GameObjectId: {enemy.GameObjectId}";
            if (ImGui.CollapsingHeader(header))
            {
                ImGui.Text($"- Is Casting: {(enemy.IsCasting ? "Yes" : "No")}");
                ImGui.Text($"- Cast Action ID: {(enemy.IsCasting ? enemy.CastActionId.ToString() : "N/A")}");
                ImGui.Text($"- Targeting Player: {(enemy.CastTargetObjectId == Player.GameObjectId ? "Yes" : "No")}");
            }
        }
    }
    #endregion

    #region Limit Break value
    [Description("Limit Break Level")]
    private unsafe static byte CurrentLimitBreakLevel
    {
        get
        {
            FFXIVClientStructs.FFXIV.Client.Game.UI.LimitBreakController limitBreakController = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance()->LimitBreakController;
            ushort currentUnits = *&limitBreakController.CurrentUnits;

            if (currentUnits >= 9000)
            {
                return 3;
            }
            else if (currentUnits >= 6000)
            {
                return 2;
            }
            else if (currentUnits >= 3000)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }

    [Description("Current Units")]
    private unsafe static ushort CurrentCurrentUnits
    {
        get
        {
            FFXIVClientStructs.FFXIV.Client.Game.UI.LimitBreakController limitBreakController = FFXIVClientStructs.FFXIV.Client.Game.UI.UIState.Instance()->LimitBreakController;
            ushort currentUnits = *&limitBreakController.CurrentUnits;

            return currentUnits;
        }
    }
    #endregion

}