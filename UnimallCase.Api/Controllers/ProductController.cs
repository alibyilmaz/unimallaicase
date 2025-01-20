using Microsoft.AspNetCore.Mvc;
using UnimallCase.Api.Models;
using UnimallCase.Api.Services;

namespace UnimallCase.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductCrawlerService _crawlerService;
        private readonly IProductTransformService _transformService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductCrawlerService crawlerService,
            IProductTransformService transformService,
            ILogger<ProductController> logger)
        {
            _crawlerService = crawlerService;
            _transformService = transformService;
            _logger = logger;
        }

        [HttpGet("crawl")]
        public async Task<ActionResult<List<Product>>> CrawlProduct([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url) || !url.Contains("trendyol.com"))
            {
                return BadRequest("Invalid Trendyol product URL");
            }

            try
            {
                // Use ConfigureAwait(false) to reduce thread switching overhead
                var products = await _crawlerService.CrawlProductAsync(url).ConfigureAwait(false);
                return Ok(products);
            }
            catch (Exception ex)
            {
                // Minimal logging to reduce performance impact
                _logger.LogError(ex, "Crawl error");
                return StatusCode(500, "Crawling failed");
            }
        }

        [HttpGet("crawl-and-transform")]
        public async Task<ActionResult<List<Product>>> CrawlAndTransformProduct([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url) || !url.Contains("trendyol.com"))
            {
                return BadRequest("Invalid Trendyol product URL");
            }

            try
            {
                // Reduce method call overhead
                var products = await _crawlerService.CrawlProductAsync(url).ConfigureAwait(false);
                if (!products.Any())
                {
                    return NotFound("No products found");
                }

                // Use LINQ for more efficient transformation
                var transformedProducts = await Task.WhenAll(
                    products.Select(p => _transformService.TransformProductAsync(p))
                ).ConfigureAwait(false);

                return Ok(transformedProducts.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transform error");
                return StatusCode(500, "Processing failed");
            }
        }

        [HttpPost("transform")]
        public async Task<ActionResult<Product>> TransformProduct([FromBody] Product product)
        {
            if (product == null || string.IsNullOrEmpty(product.Sku))
            {
                return BadRequest("Invalid product data");
            }

            try
            {
                var transformedProduct = await _transformService.TransformProductAsync(product).ConfigureAwait(false);
                return Ok(transformedProduct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transform error");
                return StatusCode(500, "Transformation failed");
            }
        }
    }
}
