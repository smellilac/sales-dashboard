namespace SalesDashboard.Api.Data.Seed;

/// <summary>Behavioural archetype of a manager; drives how often, how big and how cleanly they sell (D13).</summary>
internal enum ManagerProfileKind
{
    Strong,
    Weak,
    BigDeals,
    ManySmall,
    Vacation,
    Terminated,
    Average,
}
