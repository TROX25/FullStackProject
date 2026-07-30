namespace MojProjekt.Application.Search;

public sealed record QueryInterpretationResult(
    SearchCriteria Criteria,
    string IntentSummary,
    IReadOnlyList<string> Warnings,
    bool UsedFallbackExtraction);
