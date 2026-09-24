using System.Runtime.InteropServices;

namespace SalesDashboard.Api.Shared.Period;

/// <summary>
/// A day range together with its half-open UTC bounds (D4).
/// <see cref="From"/> and <see cref="To"/> are inclusive business-time-zone days;
/// <see cref="StartUtc"/> is inclusive and <see cref="EndUtc"/> is exclusive, forming
/// <c>[From 00:00, To+1 00:00)</c> converted to UTC. Both instants have <see cref="DateTimeKind.Utc"/>.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct DateRange(DateOnly From, DateOnly To, DateTime StartUtc, DateTime EndUtc);
