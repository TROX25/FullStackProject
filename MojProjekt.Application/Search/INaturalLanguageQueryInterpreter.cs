namespace MojProjekt.Application.Search;

/// <summary>
/// Turns a raw natural-language search query into structured SearchCriteria. Implementations must
/// never throw for a merely ambiguous/unusual query — they should return best-effort criteria plus
/// warnings, falling back to a non-AI extractor if the AI call itself fails.
/// </summary>
public interface INaturalLanguageQueryInterpreter
{
    Task<QueryInterpretationResult> InterpretAsync(string query, CancellationToken cancellationToken);
}
