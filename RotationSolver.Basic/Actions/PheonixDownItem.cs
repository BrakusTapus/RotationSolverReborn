using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Actions;
internal class PhoenixDownItem : BaseItem
{
    public PhoenixDownItem(Item item) : base(item)
    {
       
    }

    private static bool AnyLivingHealerInParty()
    {
        foreach (IBattleChara member in DataCenter.PartyMembers)
        {
            if (member.IsJobCategory(JobRole.Healer) && !member.IsDead)
                return true;
        }
        return false;
    }

    protected override bool CanUseThis => Service.Config.UsePhoenixDown && !AnyLivingHealerInParty() && DataCenter.DeathTarget != null;
}
