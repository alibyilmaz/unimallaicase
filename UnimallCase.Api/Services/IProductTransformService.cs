using UnimallCase.Api.Models;

namespace UnimallCase.Api.Services
{
    public interface IProductTransformService
    {
        Task<Product> TransformProductAsync(Product product);
    }
}
