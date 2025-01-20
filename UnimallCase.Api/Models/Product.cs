using System.Text.Json.Serialization;

namespace UnimallCase.Api.Models
{
    public class Product
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string ParentSku { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public string Url { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new List<string>();
        public int? Score { get; set; }
        public List<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
    }

    public class ProductAttribute
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
