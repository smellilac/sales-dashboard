using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Features.Categories;

/// <summary>
/// The category breakdown block (CAT-COUNT). Every category is present, including those with no sales;
/// <see cref="Items"/> is ordered by revenue descending. All figures cover <see cref="Period"/> and are Paid
/// only (D2).
/// </summary>
public sealed record CategoriesResponse(
    PeriodDto Period,
    IReadOnlyList<CategoryRow> Items);
