namespace RotationSolver.Basic.Configuration.Conditions
{
    [Description("Condition Set")]
    internal class ConditionSet : DelayCondition
    {
        public List<ICondition> Conditions { get; set; } = [];

        public LogicalType Type;

        protected override bool IsTrueInside(ICustomRotation rotation)
        {
            if (Conditions.Count == 0)
            {
                return false;
            }

            switch (Type)
            {
                case LogicalType.And:
                    foreach (var c in Conditions)
                    {
                        if (!c.IsTrue(rotation)) return false;
                    }
                    return true;
                case LogicalType.Or:
                    foreach (var c in Conditions)
                    {
                        if (c.IsTrue(rotation)) return true;
                    }
                    return false;
                default:
                    return false;
            }
        }
    }

    internal enum LogicalType : byte
    {
        [Description("&&")]
        And,

        [Description("||")]
        Or,
    }
}
