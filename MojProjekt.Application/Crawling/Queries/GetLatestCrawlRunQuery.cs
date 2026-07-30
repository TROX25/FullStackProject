using MediatR;
using MojProjekt.Application.Crawling.Contracts;

namespace MojProjekt.Application.Crawling.Queries;

public sealed record GetLatestCrawlRunQuery : IRequest<CrawlRunDto?>;
