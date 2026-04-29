namespace Ambev.DeveloperEvaluation.WebApi.Common;

/// <summary>
/// Error envelope as defined in <c>/.doc/general-api.md</c>:
/// <c>{ "type": "...", "error": "...", "detail": "..." }</c>.
/// </summary>
public class ApiErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
