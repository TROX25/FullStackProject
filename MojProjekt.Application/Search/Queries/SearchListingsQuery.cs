using MediatR;
using MojProjekt.Application.Search.Contracts;

namespace MojProjekt.Application.Search.Queries;

public sealed record SearchListingsQuery(string Query, int MaxResults) : IRequest<SearchResponseDto>;
