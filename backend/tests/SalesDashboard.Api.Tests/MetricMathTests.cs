using SalesDashboard.Api.Shared.Metrics;
using Xunit;

namespace SalesDashboard.Api.Tests;

/// <summary>Unit tests for the shared metric formulas (R4, R6, D6).</summary>
public sealed class MetricMathTests
{
    [Fact]
    public void Margin_ZeroRevenue_IsNull()
    {
        Assert.Null(MetricMath.Margin(revenue: 0m, cost: 0m));
    }

    [Fact]
    public void Margin_ComputesFractionOfRevenue()
    {
        Assert.Equal(0.25m, MetricMath.Margin(revenue: 100m, cost: 75m));
    }

    [Fact]
    public void AverageCheck_ZeroCount_IsNull()
    {
        Assert.Null(MetricMath.AverageCheck(revenue: 1000m, count: 0));
    }

    [Fact]
    public void AverageCheck_DividesRevenueByCount()
    {
        Assert.Equal(250m, MetricMath.AverageCheck(revenue: 1000m, count: 4));
    }

    [Fact]
    public void RelativeChange_ZeroPrevious_IsNull()
    {
        Assert.Null(MetricMath.RelativeChange(current: 100m, previous: 0m));
    }

    [Fact]
    public void RelativeChange_ComputesRelativeDelta()
    {
        Assert.Equal(0.12m, MetricMath.RelativeChange(current: 112m, previous: 100m));
    }

    [Fact]
    public void PointsChange_EitherNull_IsNull()
    {
        Assert.Null(MetricMath.PointsChange(currentMargin: 0.22m, previousMargin: null));
        Assert.Null(MetricMath.PointsChange(currentMargin: null, previousMargin: 0.20m));
    }

    [Fact]
    public void PointsChange_IsDifferenceOfMargins()
    {
        // 22% vs 20% -> +2 points, i.e. 0.02 as a fraction.
        Assert.Equal(0.02m, MetricMath.PointsChange(currentMargin: 0.22m, previousMargin: 0.20m));
    }
}
