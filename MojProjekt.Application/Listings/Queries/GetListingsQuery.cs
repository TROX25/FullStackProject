using MediatR;
using MojProjekt.Application.Common;
using MojProjekt.Application.Listings.Contracts;

namespace MojProjekt.Application.Listings.Queries;

public sealed record GetListingsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<ListingSummaryDto>>;
