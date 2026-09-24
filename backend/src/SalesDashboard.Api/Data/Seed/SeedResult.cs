namespace SalesDashboard.Api.Data.Seed;

/// <summary>Counts written by a seed run, plus the instant the data was generated for.</summary>
public sealed record SeedResult(
    DateTimeOffset GeneratedAt,
    int Managers,
    int Customers,
    int Categories,
    int Products,
    int Sales,
    int SaleItems);
