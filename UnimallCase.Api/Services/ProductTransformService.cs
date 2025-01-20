using System.Text.Json;
using UnimallCase.Api.Models;
using Microsoft.Extensions.Logging;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnimallCase.Api.Services
{
    public class ProductTransformService : IProductTransformService
    {
        private readonly RestClient _client;
        private readonly string _apiKey;
        private readonly ILogger<ProductTransformService> _logger;
        private readonly IProductImageCrawlerService _imageCrawlerService;

        public ProductTransformService(
            string openAiApiKey, 
            ILogger<ProductTransformService> logger,
            IProductImageCrawlerService imageCrawlerService)
        {
            _logger = logger;
            _apiKey = openAiApiKey;
            _client = new RestClient("https://api.openai.com/v1/");
            _imageCrawlerService = imageCrawlerService;
        }

        public async Task<Product> TransformProductAsync(Product product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            try
            {
                _logger.LogInformation("Starting product transformation for SKU: {Sku}", product.Sku);

                // Get image count from URL if available
                int crawledImageCount = 0;
                if (!string.IsNullOrEmpty(product.Url))
                {
                    crawledImageCount = await _imageCrawlerService.GetImageCountFromUrlAsync(product.Url);
                    _logger.LogInformation("Found {Count} images from URL for SKU: {Sku}", crawledImageCount, product.Sku);
                }

                var transformedProduct = new Product
                {
                    Sku = product.Sku,
                    ParentSku = product.ParentSku,
                    OriginalPrice = product.OriginalPrice,
                    DiscountedPrice = product.DiscountedPrice,
                    Url = product.Url,
                    Images = product.Images ?? new List<string>(),
                    Attributes = product.Attributes ?? new List<ProductAttribute>()
                };

                // First translate main product info
                var mainTranslation = await TranslateMainProductInfo(product);
                transformedProduct.Name = mainTranslation.Name;
                transformedProduct.Description = mainTranslation.Description;
                transformedProduct.Brand = mainTranslation.Brand;
                transformedProduct.Category = mainTranslation.Category;

                // Then translate attributes if any exist
                if (product.Attributes?.Any() == true)
                {
                    transformedProduct.Attributes = await TranslateAttributes(product.Attributes);
                }

                // Calculate final score
                transformedProduct.Score = await CalculateScore(transformedProduct, mainTranslation.Name, mainTranslation.Description);

                _logger.LogInformation("Completed product transformation for SKU: {Sku}", product.Sku);
                return transformedProduct;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming product with SKU: {Sku}", product.Sku);
                throw;
            }
        }

        private async Task<(string Name, string Description, string Brand, string Category)> TranslateMainProductInfo(Product product)
        {
            var prompt = $@"Translate and optimize the following product information to English. Respond ONLY with a valid JSON object in exactly this format, nothing else:

{{
    ""name"": ""<translated product name>"",
    ""description"": ""<translated product description>"",
    ""brand"": ""<translated brand>"",
    ""category"": ""<translated category>""
}}

Product Information:
Name: {product.Name}
Description: {product.Description}
Brand: {product.Brand}
Category: {product.Category}";

            try
            {
                var request = new RestRequest("chat/completions", Method.Post);
                request.AddHeader("Authorization", $"Bearer {_apiKey}");
                request.AddHeader("Content-Type", "application/json");

                var requestBody = new
                {
                    model = "gpt-3.5-turbo-1106",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a product information translator. Respond ONLY with the requested JSON format, nothing else." },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.3,
                    response_format = new { type = "json_object" },
                    max_tokens = 500
                };

                request.AddJsonBody(JsonConvert.SerializeObject(requestBody));

                _logger.LogDebug("Sending request to OpenAI for main product info translation");
                var response = await _client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    var errorContent = JsonConvert.DeserializeObject<JObject>(response.Content ?? "{}");
                    var errorMessage = errorContent?["error"]?["message"]?.ToString() ?? response.ErrorMessage;
                    throw new Exception($"OpenAI API request failed: {errorMessage}");
                }

                var jsonResponse = JObject.Parse(response.Content);
                var responseContent = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();

                if (string.IsNullOrEmpty(responseContent))
                {
                    throw new Exception("OpenAI API response was empty or in unexpected format");
                }

                _logger.LogDebug("Received OpenAI response for main product info translation");

                try
                {
                    var parsedResponse = JObject.Parse(responseContent);
                    
                    // Validate required fields
                    if (parsedResponse["name"] == null || parsedResponse["description"] == null || 
                        parsedResponse["brand"] == null || parsedResponse["category"] == null)
                    {
                        throw new Exception("OpenAI response missing required fields");
                    }

                    return (
                        Name: parsedResponse["name"].ToString(),
                        Description: parsedResponse["description"].ToString(),
                        Brand: parsedResponse["brand"].ToString(),
                        Category: parsedResponse["category"].ToString()
                    );
                }
                catch (JsonReaderException ex)
                {
                    _logger.LogError(ex, "Failed to parse main product info response as JSON. Response content: {Content}", responseContent);
                    throw new Exception("Main product info response was not in valid JSON format");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating main product info");
                throw;
            }
        }

        private async Task<int> CalculateScore(Product product, string translatedName, string translatedDescription)
        {
            try
            {
                var imageCount = product.Images?.Count ?? 0;
                var attributeCount = product.Attributes?.Count ?? 0;
                
                // Calculate base scores for each component
                int nameScore = 0;
                if (!string.IsNullOrWhiteSpace(translatedName))
                {
                    nameScore = Math.Min(25, (int)(translatedName.Length / 4.0)); // Longer names get better scores up to 25
                }

                int descriptionScore = 0;
                if (!string.IsNullOrWhiteSpace(translatedDescription))
                {
                    descriptionScore = Math.Min(20, (int)(translatedDescription.Length / 25.0)); // Longer descriptions get better scores up to 20
                }

                // Image score based on count (max 35 points)
                int imageScore = imageCount switch
                {
                    0 => 0,
                    1 => 10,
                    2 => 20,
                    3 => 25,
                    4 => 30,
                    _ => 35  // Maximum for 5+ images
                };

                // Attribute score based on count (max 20 points)
                int attributeScore = Math.Min(20, attributeCount * 4); // 4 points per attribute up to 20

                // Calculate total score
                int totalScore = nameScore + descriptionScore + imageScore + attributeScore;

                // Apply penalties for missing critical data
                if (string.IsNullOrWhiteSpace(product.Brand))
                    totalScore = (int)(totalScore * 0.9); // 10% penalty for missing brand

                if (string.IsNullOrWhiteSpace(product.Category))
                    totalScore = (int)(totalScore * 0.9); // 10% penalty for missing category

                if (imageCount < 3)
                    totalScore = (int)(totalScore * 0.8); // 20% penalty for fewer than 3 images

                if (attributeCount < 3)
                    totalScore = (int)(totalScore * 0.9); // 10% penalty for fewer than 3 attributes

                // Ensure score is between 0 and 100
                totalScore = Math.Max(0, Math.Min(100, totalScore));

                _logger.LogInformation(
                    "Score breakdown for SKU {Sku}:\n" +
                    "Name Score: {NameScore}/25\n" +
                    "Description Score: {DescScore}/20\n" +
                    "Image Score: {ImageScore}/35 (Count: {ImageCount})\n" +
                    "Attribute Score: {AttrScore}/20 (Count: {AttrCount})\n" +
                    "Total Score: {TotalScore}/100",
                    product.Sku,
                    nameScore,
                    descriptionScore,
                    imageScore,
                    imageCount,
                    attributeScore,
                    attributeCount,
                    totalScore);

                return totalScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating score");
                return 0;
            }
        }

        private async Task<List<ProductAttribute>> TranslateAttributes(List<ProductAttribute> attributes)
        {
            if (attributes?.Any() != true)
                return new List<ProductAttribute>();

            try
            {
                var prompt = @"Translate the following product attributes to English. Return a JSON array of objects, each with 'key' and 'name' fields. Example format:
[
    {
        ""key"": ""Material"",
        ""name"": ""Cotton""
    },
    {
        ""key"": ""Color"",
        ""name"": ""Blue""
    }
]

Attributes to translate:
" + string.Join("\n", attributes.Select(a => $"{a.Key}: {a.Name}"));

                _logger.LogInformation("Sending request to OpenAI for attributes translation. Count: {Count}", attributes.Count);

                var request = new RestRequest("chat/completions", Method.Post);
                request.AddHeader("Authorization", $"Bearer {_apiKey}");
                request.AddHeader("Content-Type", "application/json");
                
                var requestBody = new
                {
                    model = "gpt-3.5-turbo-1106",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a translator. You must respond with a JSON object containing an 'attributes' array. Each object in the array must have 'key' and 'name' fields for the translated attributes. Example response format: { \"attributes\": [ { \"key\": \"Material\", \"name\": \"Cotton\" } ] }" },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.3,
                    response_format = new { type = "json_object" },
                    max_tokens = 500
                };

                request.AddJsonBody(JsonConvert.SerializeObject(requestBody));

                _logger.LogDebug("OpenAI API Request: {Request}", JsonConvert.SerializeObject(requestBody));
                var response = await _client.ExecuteAsync(request);

                _logger.LogDebug("OpenAI API Raw Response: {Response}", response.Content);

                if (!response.IsSuccessful)
                {
                    var errorContent = JsonConvert.DeserializeObject<JObject>(response.Content ?? "{}");
                    var errorMessage = errorContent?["error"]?["message"]?.ToString() ?? response.ErrorMessage;
                    throw new Exception($"OpenAI API request failed: {errorMessage}");
                }

                var jsonResponse = JObject.Parse(response.Content);
                var responseContent = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();
                if (string.IsNullOrEmpty(responseContent))
                {
                    throw new Exception("OpenAI API response was empty or in unexpected format");
                }

                _logger.LogInformation("Received OpenAI response for attributes translation");
                _logger.LogDebug("Attributes translation response: {Content}", responseContent);

                try
                {
                    // Parse the response content as a JObject first
                    var parsedContent = JObject.Parse(responseContent);
                    
                    // Extract the attributes array from the response
                    var attributesArray = parsedContent["attributes"]?.ToString() ?? responseContent;
                    
                    var translatedAttributes = JsonConvert.DeserializeObject<List<ProductAttribute>>(attributesArray);
                    if (translatedAttributes == null || !translatedAttributes.Any())
                    {
                        _logger.LogWarning("No attributes were translated. Raw response: {Response}", responseContent);
                        return attributes; // Return original attributes if translation failed
                    }
                    return translatedAttributes;
                }
                catch (JsonReaderException ex)
                {
                    _logger.LogError(ex, "Failed to parse attributes response as JSON. Response content: {Content}", responseContent);
                    return attributes; // Return original attributes if parsing failed
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating attributes: {Error}", ex.Message);
                return attributes; // Return original attributes if translation failed
            }
        }
    }
}
