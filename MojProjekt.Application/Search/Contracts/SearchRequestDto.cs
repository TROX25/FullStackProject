namespace MojProjekt.Application.Search.Contracts;

public sealed record SearchRequestDto(string Query, int? MaxResults);
