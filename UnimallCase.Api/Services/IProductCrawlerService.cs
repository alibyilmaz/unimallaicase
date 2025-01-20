using UnimallCase.Api.Models;

namespace UnimallCase.Api.Services
{
    public interface IProductCrawlerService
    {
        Task<List<Product>> CrawlProductAsync(string productUrl);
    }
}
