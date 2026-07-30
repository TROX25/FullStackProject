using MediatR;
using MojProjekt.Application.Common;
using MojProjekt.Application.Listings.Contracts;
using MojProjekt.Application.Listings.Mapping;

namespace MojProjekt.Application.Listings.Queries;

public sealed class GetListingsQueryHandler(IListingRepository repository)
    : IRequestHandler<GetListingsQuery, PagedResult<ListingSummaryDto>>
{
    public async Task<PagedResult<ListingSummaryDto>> Handle(GetListingsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var (items, totalCount) = await repository.GetPagedAsync(page, pageSize, cancellationToken);

        return new PagedResult<ListingSummaryDto>(
            items.Select(l => l.ToSummaryDto()).ToList(),
            page,
            pageSize,
            totalCount);
    }
}
