using System.Data;
using Npgsql;
using SalesDashboard.Api.Shared.Data;
using Xunit;

namespace SalesDashboard.Api.Tests;

/// <summary>Unit tests for the Dapper <see cref="DateOnly"/> handler used by the timeseries query.</summary>
public sealed class DateOnlyTypeHandlerTests
{
    private readonly DateOnlyTypeHandler _handler = new();

    [Fact]
    public void SetValue_BindsAsDate()
    {
        var parameter = new NpgsqlParameter();
        var value = new DateOnly(2026, 3, 15);

        _handler.SetValue(parameter, value);

        Assert.Equal(DbType.Date, parameter.DbType);
        Assert.Equal(value, parameter.Value);
    }

    [Fact]
    public void Parse_AcceptsDateOnly()
    {
        var value = new DateOnly(2026, 3, 15);

        Assert.Equal(value, _handler.Parse(value));
    }

    [Fact]
    public void Parse_ConvertsDateTime()
    {
        var dateTime = new DateTime(2026, 3, 15, 8, 30, 0, DateTimeKind.Unspecified);

        Assert.Equal(new DateOnly(2026, 3, 15), _handler.Parse(dateTime));
    }

    [Fact]
    public void Parse_UnsupportedType_Throws()
    {
        Assert.Throws<InvalidCastException>(() => _handler.Parse("2026-03-15"));
    }
}
