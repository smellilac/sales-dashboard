using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SalesDashboard.Api.Shared.Errors;
using SalesDashboard.Api.Shared.Period;
using Xunit;

namespace SalesDashboard.Api.Tests;

/// <summary>Unit tests for mapping <c>ErrorOr</c> errors to HTTP results (D12).</summary>
public sealed class ErrorMappingTests
{
    [Fact]
    public void Validation_MapsToProblemPerField_WithCodesInExtensions()
    {
        var errors = new List<Error>
        {
            Error.Validation(
                code: PeriodErrorCodes.Required,
                description: "'from' is required.",
                metadata: new Dictionary<string, object> { [PeriodErrorCodes.FieldKey] = "from" }),
        };

        var result = errors.ToProblemResult();

        var validation = Assert.IsType<ValidationProblem>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, validation.ProblemDetails.Status);

        Assert.True(validation.ProblemDetails.Errors.ContainsKey("from"));
        Assert.Contains("'from' is required.", validation.ProblemDetails.Errors["from"]);

        Assert.True(validation.ProblemDetails.Extensions.ContainsKey("codes"));
        var codes = Assert.IsType<Dictionary<string, string>>(validation.ProblemDetails.Extensions["codes"]);
        Assert.True(codes.ContainsKey("from"));
        Assert.Equal(PeriodErrorCodes.Required, codes["from"]);
    }

    [Fact]
    public void MultipleValidationErrors_GroupByField()
    {
        var errors = new List<Error>
        {
            Error.Validation(
                code: PeriodErrorCodes.Required,
                description: "'from' is required.",
                metadata: new Dictionary<string, object> { [PeriodErrorCodes.FieldKey] = "from" }),
            Error.Validation(
                code: PeriodErrorCodes.Required,
                description: "'to' is required.",
                metadata: new Dictionary<string, object> { [PeriodErrorCodes.FieldKey] = "to" }),
        };

        var result = errors.ToProblemResult();

        var validation = Assert.IsType<ValidationProblem>(result);
        Assert.True(validation.ProblemDetails.Errors.ContainsKey("from"));
        Assert.True(validation.ProblemDetails.Errors.ContainsKey("to"));
    }

    [Fact]
    public void NotFound_MapsTo404()
    {
        var errors = new List<Error> { Error.NotFound(description: "Missing.") };

        var result = errors.ToProblemResult();

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Fact]
    public void Failure_MapsTo500()
    {
        var errors = new List<Error> { Error.Failure(description: "Boom.") };

        var result = errors.ToProblemResult();

        var problem = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, problem.StatusCode);
    }
}
