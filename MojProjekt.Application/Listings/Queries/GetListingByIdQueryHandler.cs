using MediatR;
using MojProjekt.Application.Listings.Contracts;
using MojProjekt.Application.Listings.Mapping;

namespace MojProjekt.Application.Listings.Queries;

public sealed class GetListingByIdQueryHandler(IListingRepository repository)
    : IRequestHandler<GetListingByIdQuery, ListingDetailDto?>
{
    public async Task<ListingDetailDto?> Handle(GetListingByIdQuery request, CancellationToken cancellationToken)
    {
        var listing = await repository.GetByIdAsync(request.Id, cancellationToken);
        return listing?.ToDetailDto();
    }
}
