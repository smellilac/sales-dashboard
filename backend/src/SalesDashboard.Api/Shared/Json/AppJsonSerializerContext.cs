using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SalesDashboard.Api.Data.Entities;
using SalesDashboard.Api.Shared.Metrics;
using SalesDashboard.Api.Shared.Period;

namespace SalesDashboard.Api.Shared.Json;

/// <summary>
/// Source-generated JSON contracts for the API (camelCase, nulls written explicitly per D6). Registered
/// first in the resolver chain in <c>Program.cs</c>. Enums serialize as strings.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    Converters = [typeof(JsonStringEnumConverter<SaleStatus>), typeof(JsonStringEnumConverter<CustomerSegment>)])]
[JsonSerializable(typeof(ValueMetric))]
[JsonSerializable(typeof(CountMetric))]
[JsonSerializable(typeof(MarginMetric))]
[JsonSerializable(typeof(PeriodDto))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
[JsonSerializable(typeof(SaleStatus))]
[JsonSerializable(typeof(CustomerSegment))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
