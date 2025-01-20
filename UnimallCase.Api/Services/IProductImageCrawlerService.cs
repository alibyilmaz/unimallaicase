using System.Threading.Tasks;

namespace UnimallCase.Api.Services
{
    public interface IProductImageCrawlerService
    {
        Task<int> GetImageCountFromUrlAsync(string url);
    }
}
