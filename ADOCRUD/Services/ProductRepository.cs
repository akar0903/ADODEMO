using ADOCRUD.Interfaces;
using ADOCRUD.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;

namespace ADOCRUD.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly IConfiguration configuration;
        private readonly IDistributedCache cache;
        public ProductRepository(IConfiguration configuration, IDistributedCache cache)
        {
            this.configuration = configuration;
            this.cache = cache;
        }

        public async Task<List<ProductModel>> GetAllProducts()
        {
            string cacheKey = "ID";
            string cachedData = await cache.GetStringAsync(cacheKey);
            List<ProductModel> products;
            if (!string.IsNullOrEmpty(cachedData))
            {
                products = JsonConvert.DeserializeObject<List<ProductModel>>(cachedData);
            }
            else
            {
                products = await GetProductDataFromSource();
                string serializedData = JsonConvert.SerializeObject(products);
                await cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(35)
                });
            }
            return products;
        }

        private async Task<List<ProductModel>> GetProductDataFromSource()
        {
            List<ProductModel> products = new List<ProductModel>();
            DataTable dataTable = new DataTable();
            using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM Product", con);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                await con.OpenAsync();
                adapter.Fill(dataTable);
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ProductModel productModel = new ProductModel();
                    productModel.ProductId = Convert.ToInt32(dataTable.Rows[i]["ID"]);
                    productModel.ProductName = dataTable.Rows[i]["ProductName"].ToString();
                    productModel.ProductPrice = Convert.ToDecimal(dataTable.Rows[i]["ProductPrice"]);
                    productModel.EntryDate = Convert.ToDateTime(dataTable.Rows[i]["ProductEntryDate"]);
                    products.Add(productModel);
                }
            }
            return products;
        }
        public async Task AddProduct(ProductModel product)
        {
            using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("Insert into Product values(@ProductId, @ProductName, @ProductPrice, GETDATE())", con);
                cmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
                cmd.Parameters.AddWithValue("@ProductPrice", product.ProductPrice);
                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            string cacheKey = "ID";
            await cache.RemoveAsync(cacheKey);
        }
        public async Task<int> DeleteProduct(int id)
        {
            using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                await con.OpenAsync();
                string sqlQuery = "DELETE FROM Product WHERE ID = @ProductId";
                using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                {
                    cmd.Parameters.AddWithValue("@ProductId", id);
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        await cache.RemoveAsync("ID");
                    }
                    return rowsAffected;
                }
            }
        }
        public async Task<ProductModel> GetProductById(int id)
        {
            using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM Product WHERE ID = @Id", con);
                cmd.Parameters.AddWithValue("@Id", id);
                await con.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    ProductModel product = new ProductModel
                    {
                        ProductId = Convert.ToInt32(reader["ID"]),
                        ProductName = reader["ProductName"].ToString(),
                        ProductPrice = Convert.ToDecimal(reader["ProductPrice"]),
                        EntryDate = Convert.ToDateTime(reader["ProductEntryDate"])
                    };
                    return product;
                }
                return null;
            }
        }
        public async Task<ProductModel> GetProductByName(string name)
        {
            using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM Product WHERE ProductName = @Name", con);
                cmd.Parameters.AddWithValue("@Name", name);
                await con.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    ProductModel product = new ProductModel
                    {
                        ProductId = Convert.ToInt32(reader["ID"]),
                        ProductName = reader["ProductName"].ToString(),
                        ProductPrice = Convert.ToDecimal(reader["ProductPrice"]),
                        EntryDate = Convert.ToDateTime(reader["ProductEntryDate"])
                    };
                    return product;
                }
                return null;
            }
        }
        public async Task<ProductModel> GetProductByNamePrice(string name,float price)
        {
            using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM Product WHERE ProductName = @Name AND ProductPrice = @Price", con);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Price", price);
                await con.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    ProductModel product = new ProductModel
                    {
                        ProductId = Convert.ToInt32(reader["ID"]),
                        ProductName = reader["ProductName"].ToString(),
                        ProductPrice = Convert.ToDecimal(reader["ProductPrice"]),
                        EntryDate = Convert.ToDateTime(reader["ProductEntryDate"])
                    };
                    return product;
                }
                return null;
            }
        }
        public async Task<int> GetTotalID()
        {
            using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT COUNT(ID) as CountID From Product", con);
                await con.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                if (reader.Read())
                {
                    int totalCount = Convert.ToInt32(reader["CountID"]);
                    con.Close();
                    return totalCount;
                }
                else
                {
                    con.Close();
                    return 0;
                }
                
            }
        }


    }
}
