using ErrorOr;
using SalesDashboard.Api.Shared.Period;
using Xunit;

namespace SalesDashboard.Api.Tests;

/// <summary>Unit tests for period validation and UTC-bound computation (D3, D4).</summary>
public sealed class ReportingPeriodTests
{
    private static readonly TimeZoneInfo Moscow = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");

    private static ReportingPeriod CreateOrThrow(DateOnly from, DateOnly to)
    {
        var result = ReportingPeriod.Create(from, to, Moscow);
        Assert.False(result.IsError);
        return result.Value;
    }

    [Fact]
    public void SingleDay_CurrentAndPreviousAreOneDay()
    {
        var day = new DateOnly(2026, 9, 24);

        var period = CreateOrThrow(day, day);

        Assert.Equal(day, period.Current.From);
        Assert.Equal(day, period.Current.To);
        // Previous is the single preceding day.
        Assert.Equal(day.AddDays(-1), period.Previous.From);
        Assert.Equal(day.AddDays(-1), period.Previous.To);
    }

    [Fact]
    public void SevenDays_PreviousIsTheSevenDaysBefore()
    {
        var from = new DateOnly(2026, 9, 18);
        var to = new DateOnly(2026, 9, 24);

        var period = CreateOrThrow(from, to);

        Assert.Equal(new DateOnly(2026, 9, 11), period.Previous.From);
        Assert.Equal(new DateOnly(2026, 9, 17), period.Previous.To);
    }

    [Fact]
    public void ThirtyDays_PreviousIsTheThirtyDaysBefore()
    {
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 30);

        var period = CreateOrThrow(from, to);

        Assert.Equal(new DateOnly(2026, 8, 2), period.Previous.From);
        Assert.Equal(new DateOnly(2026, 8, 31), period.Previous.To);
    }

    [Fact]
    public void CrossesMonthEnd_KeepsInclusiveDays()
    {
        var from = new DateOnly(2026, 1, 28);
        var to = new DateOnly(2026, 2, 3);

        var period = CreateOrThrow(from, to);

        Assert.Equal(from, period.Current.From);
        Assert.Equal(to, period.Current.To);
        Assert.Equal(new DateOnly(2026, 1, 21), period.Previous.From);
        Assert.Equal(new DateOnly(2026, 1, 27), period.Previous.To);
    }

    [Fact]
    public void CrossesYearEnd_PreviousSpansIntoPreviousYear()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 1, 7);

        var period = CreateOrThrow(from, to);

        Assert.Equal(new DateOnly(2025, 12, 25), period.Previous.From);
        Assert.Equal(new DateOnly(2025, 12, 31), period.Previous.To);
    }

    [Fact]
    public void UtcBounds_ForMoscow_AreShiftedByThreeHours()
    {
        var from = new DateOnly(2026, 9, 1);
        var to = new DateOnly(2026, 9, 30);

        var period = CreateOrThrow(from, to);

        // 2026-09-01 00:00 Moscow (UTC+3) == 2026-08-31 21:00 UTC.
        Assert.Equal(new DateTime(2026, 8, 31, 21, 0, 0, DateTimeKind.Utc), period.Current.StartUtc);
        // Half-open upper bound: 2026-10-01 00:00 Moscow == 2026-09-30 21:00 UTC.
        Assert.Equal(new DateTime(2026, 9, 30, 21, 0, 0, DateTimeKind.Utc), period.Current.EndUtc);
        Assert.Equal(DateTimeKind.Utc, period.Current.StartUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, period.Current.EndUtc.Kind);
    }

    [Fact]
    public void PreviousUtcBounds_AbutCurrentStart()
    {
        var period = CreateOrThrow(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        // The previous window's exclusive end is exactly the current window's inclusive start.
        Assert.Equal(period.Current.StartUtc, period.Previous.EndUtc);
    }

    [Fact]
    public void MissingFrom_IsRequiredError_OnFromField()
    {
        var result = ReportingPeriod.Create(from: null, to: new DateOnly(2026, 9, 24), Moscow);

        AssertValidation(result, PeriodErrorCodes.Required, "from");
    }

    [Fact]
    public void MissingTo_IsRequiredError_OnToField()
    {
        var result = ReportingPeriod.Create(new DateOnly(2026, 9, 24), to: null, Moscow);

        AssertValidation(result, PeriodErrorCodes.Required, "to");
    }

    [Fact]
    public void FromAfterTo_IsOrderError_OnFromField()
    {
        var result = ReportingPeriod.Create(new DateOnly(2026, 9, 25), new DateOnly(2026, 9, 24), Moscow);

        AssertValidation(result, PeriodErrorCodes.FromAfterTo, "from");
    }

    [Fact]
    public void LongerThanMax_IsTooLongError_OnToField()
    {
        var from = new DateOnly(2026, 1, 1);
        // Inclusive length of MaxLengthDays + 1 days.
        var to = from.AddDays(PeriodErrorCodes.MaxLengthDays);

        var result = ReportingPeriod.Create(from, to, Moscow);

        AssertValidation(result, PeriodErrorCodes.TooLong, "to");
    }

    [Fact]
    public void ExactlyMaxLength_IsAccepted()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = from.AddDays(PeriodErrorCodes.MaxLengthDays - 1);

        var result = ReportingPeriod.Create(from, to, Moscow);

        Assert.False(result.IsError);
    }

    private static void AssertValidation(ErrorOr<ReportingPeriod> result, string expectedCode, string expectedField)
    {
        Assert.True(result.IsError);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(expectedCode, error.Code);
        Assert.NotNull(error.Metadata);
        Assert.Equal(expectedField, Assert.Contains(PeriodErrorCodes.FieldKey, error.Metadata));
    }
}
