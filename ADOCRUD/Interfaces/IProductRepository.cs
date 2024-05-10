using ADOCRUD.Model;

namespace ADOCRUD.Interfaces
{
    public interface IProductRepository
    {
        Task<List<ProductModel>> GetAllProducts();
        Task AddProduct(ProductModel product);
        Task UpdateProduct(ProductModel product);
        Task<int> DeleteProduct(int id);
        Task<ProductModel> GetProductById(int id);
        Task<ProductModel> GetProductByName(string name);
        Task<ProductModel> GetProductByNamePrice(string name, float price);
        Task<int> GetTotalID();
        Task<List<ProductModel>> GetColorProducts(string color);

    }
}
