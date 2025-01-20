using HtmlAgilityPack;
using System.Text.RegularExpressions;
using UnimallCase.Api.Models;
using Microsoft.Extensions.Logging;

namespace UnimallCase.Api.Services
{
    public class ProductCrawlerService : IProductCrawlerService
    {
        private readonly HttpClient _httpClient;
        private static readonly Dictionary<string, List<Product>> _cachedProducts = new();
        private readonly ILogger<ProductCrawlerService> _logger;

        public ProductCrawlerService(HttpClient httpClient, ILogger<ProductCrawlerService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<Product>> CrawlProductAsync(string productUrl)
        {
            if (_cachedProducts.ContainsKey(productUrl))
            {
                return _cachedProducts[productUrl];
            }

            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(productUrl);

            var products = new List<Product>();
            var mainProduct = await ExtractProductInfo(doc, productUrl);
            mainProduct.Url = productUrl;
            products.Add(mainProduct);

            // Extract variant products if they exist
            var variantNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'variant-list')]//a");
            if (variantNodes != null)
            {
                foreach (var variantNode in variantNodes)
                {
                    var variantUrl = "https://www.trendyol.com" + variantNode.GetAttributeValue("href", "");
                    if (variantUrl != productUrl)
                    {
                        var variantDoc = await web.LoadFromWebAsync(variantUrl);
                        var variantProduct = await ExtractProductInfo(variantDoc, variantUrl);
                        variantProduct.ParentSku = mainProduct.Sku;
                        variantProduct.Url = variantUrl;
                        products.Add(variantProduct);
                    }
                }
            }

            _cachedProducts[productUrl] = products;
            return products;
        }

        private async Task<Product> ExtractProductInfo(HtmlDocument doc, string url)
        {
            var product = new Product
            {
                Images = new List<string>(),
                Attributes = new List<ProductAttribute>()
            };

            try
            {
                // Extract SKU from URL
                var skuMatch = Regex.Match(url, @"p-(\d+)");
                product.Sku = skuMatch.Success ? skuMatch.Groups[1].Value : "";

                // Extract name
                var nameNode = doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'pr-new-br') or contains(@class, 'product-name')]");
                product.Name = nameNode?.InnerText.Trim() ?? "";

                // Extract price
                var priceNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'prc-dsc')]");
                if (priceNode != null)
                {
                    var priceText = priceNode.InnerText.Trim().Replace("TL", "").Trim();
                    if (decimal.TryParse(priceText.Replace(".", "").Replace(",", "."), out decimal price))
                    {
                        product.OriginalPrice = price;
                        product.DiscountedPrice = price;
                    }
                }

                // Extract brand
                var brandNode = doc.DocumentNode.SelectSingleNode("//a[contains(@class, 'product-brand-name-with-link')]");
                product.Brand = brandNode?.InnerText.Trim() ?? "";

                // Extract category
                var breadcrumbNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'breadcrumb')]//span");
                if (breadcrumbNodes != null)
                {
                    product.Category = string.Join(" > ", breadcrumbNodes.Select(n => n.InnerText.Trim()));
                }

                // Extract images using multiple methods
                var images = new HashSet<string>();

                // Method 1: Direct image elements
                var imageNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'gallery-container')]//img | //div[contains(@class, 'product-slide')]//img | //div[contains(@class, 'base-product-image')]//img");
                if (imageNodes != null)
                {
                    foreach (var imgNode in imageNodes)
                    {
                        var imgUrl = imgNode.GetAttributeValue("src", "");
                        // Try data-src if src is empty
                        if (string.IsNullOrEmpty(imgUrl))
                        {
                            imgUrl = imgNode.GetAttributeValue("data-src", "");
                        }

                        // Clean up image URL
                        if (!string.IsNullOrEmpty(imgUrl))
                        {
                            // Convert to full resolution
                            imgUrl = imgUrl.Replace("/mnresize/128/192/", "/")
                                         .Replace("/mnresize/1200/1800/", "/");
                            images.Add(imgUrl);
                        }
                    }
                }

                // Method 2: JavaScript data in sliderData
                var scriptNodes = doc.DocumentNode.SelectNodes("//script[contains(text(), 'sliderData') or contains(text(), 'images') or contains(text(), 'productImages')]");
                if (scriptNodes != null)
                {
                    foreach (var scriptNode in scriptNodes)
                    {
                        var script = scriptNode.InnerText;
                        
                        // Try different JSON patterns
                        var patterns = new[]
                        {
                            @"""imageUrl"":""([^""]+)""",
                            @"""images"":\[(.*?)\]",
                            @"""productImages"":\[(.*?)\]",
                            @"""images"":({[^}]+})",
                            @"""image"":""([^""]+)"""
                        };

                        foreach (var pattern in patterns)
                        {
                            var matches = Regex.Matches(script, pattern);
                            foreach (Match match in matches)
                            {
                                var imgUrl = match.Groups[1].Value.Replace("\\/", "/");
                                if (!string.IsNullOrEmpty(imgUrl) && (imgUrl.StartsWith("http") || imgUrl.StartsWith("/")))
                                {
                                    if (!imgUrl.StartsWith("http"))
                                    {
                                        imgUrl = "https://cdn.dsmcdn.com" + imgUrl;
                                    }
                                    images.Add(imgUrl);
                                }
                            }
                        }
                    }
                }

                // Method 3: Check for image URLs in data attributes
                var dataImageNodes = doc.DocumentNode.SelectNodes("//*[@data-original]");
                if (dataImageNodes != null)
                {
                    foreach (var node in dataImageNodes)
                    {
                        var imgUrl = node.GetAttributeValue("data-original", "");
                        if (!string.IsNullOrEmpty(imgUrl))
                        {
                            if (!imgUrl.StartsWith("http"))
                            {
                                imgUrl = "https://cdn.dsmcdn.com" + imgUrl;
                            }
                            images.Add(imgUrl);
                        }
                    }
                }

                // Method 4: Look for zoom images
                var zoomNodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'zoom')]");
                if (zoomNodes != null)
                {
                    foreach (var node in zoomNodes)
                    {
                        var imgUrl = node.GetAttributeValue("href", "");
                        if (!string.IsNullOrEmpty(imgUrl))
                        {
                            if (!imgUrl.StartsWith("http"))
                            {
                                imgUrl = "https://cdn.dsmcdn.com" + imgUrl;
                            }
                            images.Add(imgUrl);
                        }
                    }
                }

                product.Images = images.ToList();
                _logger.LogInformation("Found {Count} images for product {Sku}", images.Count, product.Sku);

                // Extract attributes
                var attributeNodes = doc.DocumentNode.SelectNodes("//ul[contains(@class, 'detail-attr-container')]//li");
                if (attributeNodes != null)
                {
                    foreach (var attrNode in attributeNodes)
                    {
                        var key = attrNode.SelectSingleNode(".//span")?.InnerText.Trim() ?? "";
                        var value = attrNode.InnerText.Replace(key, "").Trim();
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            product.Attributes.Add(new ProductAttribute { Key = key, Name = value });
                        }
                    }
                }

                _logger.LogInformation("Successfully extracted product info. SKU: {Sku}, Images: {ImageCount}", 
                    product.Sku, product.Images.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting product info from URL: {Url}. Error: {Error}", url, ex.Message);
            }

            return product;
        }
    }
}
