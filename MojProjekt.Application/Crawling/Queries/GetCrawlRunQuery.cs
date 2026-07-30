using MediatR;
using MojProjekt.Application.Crawling.Contracts;

namespace MojProjekt.Application.Crawling.Queries;

public sealed record GetCrawlRunQuery(Guid Id) : IRequest<CrawlRunDto?>;
