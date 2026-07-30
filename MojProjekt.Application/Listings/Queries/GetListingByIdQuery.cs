using MediatR;
using MojProjekt.Application.Listings.Contracts;

namespace MojProjekt.Application.Listings.Queries;

public sealed record GetListingByIdQuery(Guid Id) : IRequest<ListingDetailDto?>;
