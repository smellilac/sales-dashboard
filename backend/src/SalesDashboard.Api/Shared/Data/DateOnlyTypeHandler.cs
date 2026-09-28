using System.Data;
using Dapper;

namespace SalesDashboard.Api.Shared.Data;

/// <summary>
/// Teaches Dapper to bind and read <see cref="DateOnly"/>. Dapper rejects <see cref="DateOnly"/> parameters with
/// <see cref="NotSupportedException"/> before the value reaches Npgsql (which supports it), so timeseries bucketing
/// (D4) needs this handler for both sending <c>fromDay</c>/<c>toDay</c> and reading <c>bucketStart</c>/<c>bucketEnd</c>.
/// Registered once at startup in <c>Program.cs</c>.
/// </summary>
internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        parameter.DbType = DbType.Date;
        parameter.Value = value;
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly date => date,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        _ => throw new InvalidCastException(
            $"Cannot convert {value.GetType()} to {nameof(DateOnly)}."),
    };
}
