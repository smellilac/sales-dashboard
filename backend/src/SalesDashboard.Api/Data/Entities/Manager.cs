namespace SalesDashboard.Api.Data.Entities;

/// <summary>A sales manager. Team is a plain string (MODEL); initials and avatar colour are not stored.</summary>
public sealed class Manager
{
    public required Guid Id { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string Team { get; init; }

    public required string Position { get; init; }

    public required bool IsActive { get; init; }

    public ICollection<Sale> Sales { get; init; } = [];
}
