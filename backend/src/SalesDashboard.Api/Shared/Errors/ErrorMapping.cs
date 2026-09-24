using ErrorOr;
using Microsoft.AspNetCore.Http.HttpResults;

namespace SalesDashboard.Api.Shared.Errors;

/// <summary>
/// Maps <see cref="ErrorOr"/> errors to an HTTP <see cref="IResult"/> (D12). Validation errors become a
/// 400 <c>ValidationProblem</c> grouped by field, NotFound becomes 404, and anything else becomes 500.
/// The <c>traceId</c> extension is added by <see cref="ProblemDetailsRegistration"/>.
/// </summary>
public static class ErrorMapping
{
    /// <summary>Field name used when a validation error carries no field in its metadata.</summary>
    private const string UnspecifiedField = "";

    public static IResult ToProblemResult(this IReadOnlyList<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Count == 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);
        }

        if (errors.Any(static error => error.Type == ErrorType.Validation))
        {
            return ValidationProblem(errors);
        }

        return errors[0].Type switch
        {
            ErrorType.NotFound => TypedResults.Problem(
                detail: errors[0].Description,
                statusCode: StatusCodes.Status404NotFound),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }

    private static ValidationProblem ValidationProblem(IReadOnlyList<Error> errors)
    {
        var validationErrors = errors.Where(static error => error.Type == ErrorType.Validation).ToList();

        var messages = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var codes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var error in validationErrors)
        {
            var field = FieldOf(error);

            if (!messages.TryGetValue(field, out var fieldMessages))
            {
                fieldMessages = [];
                messages[field] = fieldMessages;
                // First error for the field wins the code slot (one error per field is the norm here).
                codes[field] = error.Code;
            }

            fieldMessages.Add(error.Description);
        }

        var errorsByField = messages.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.ToArray(),
            StringComparer.Ordinal);

        var extensions = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["codes"] = codes,
        };

        return TypedResults.ValidationProblem(errorsByField, extensions: extensions);
    }

    private static string FieldOf(Error error) =>
        error.Metadata is { } metadata
        && metadata.TryGetValue(PeriodMetadataFieldKey, out var value)
        && value is string field
            ? field
            : UnspecifiedField;

    // Kept as a local constant to avoid a Shared.Period -> Shared.Errors reference just for the key name.
    private const string PeriodMetadataFieldKey = "field";
}
