using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace UnimallCase.Api.Services
{
    public class ProductImageCrawlerService : IProductImageCrawlerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductImageCrawlerService> _logger;

        public ProductImageCrawlerService(ILogger<ProductImageCrawlerService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public async Task<int> GetImageCountFromUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return 0;
            }

            try
            {
                _logger.LogInformation("Crawling product images from URL: {Url}", url);

                var html = await _httpClient.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // For Trendyol
                if (url.Contains("trendyol.com"))
                {
                    var imageNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'gallery-container')]//img");
                    if (imageNodes != null)
                    {
                        return imageNodes.Count;
                    }
                }

                // Fallback: look for common image containers
                var commonImageNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'product-image') or contains(@class, 'gallery')]//img");
                if (commonImageNodes != null)
                {
                    return commonImageNodes.Count;
                }

                // Last resort: count all product-related images
                var allImages = doc.DocumentNode.SelectNodes("//img[contains(@src, 'product') or contains(@alt, 'product')]");
                return allImages?.Count ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crawling images from URL: {Url}. Error: {Error}", url, ex.Message);
                return 0;
            }
        }
    }
}
