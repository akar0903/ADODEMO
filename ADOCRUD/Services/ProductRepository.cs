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

        //public async Task GetAllProducts()
        //{
        //    string cacheKey = "ID";
        //    string cachedData = await cache.GetStringAsync(cacheKey);
        //    List<ProductModel> products;
        //    if (!string.IsNullOrEmpty(cachedData))
        //    {
        //        products = JsonConvert.DeserializeObject<List<ProductModel>>(cachedData);
        //    }
        //    else
        //    {
        //        products =  GetProductDataFromSource();
        //        string serializedData = JsonConvert.SerializeObject(products);
        //        await cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
        //        {
        //            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(35)
        //        });
        //    }
        //}

        //private async List<ProductModel> GetProductDataFromSource()
        //{
        //    List<ProductModel> products = new List<ProductModel>();
        //    DataTable dataTable = new DataTable();
        //    using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
        //    {
        //        SqlCommand cmd = new SqlCommand("SELECT * FROM Product", con);
        //        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
        //        await con.OpenAsync();
        //        adapter.Fill(dataTable);
        //        for (int i = 0; i < dataTable.Rows.Count; i++)
        //        {
        //            ProductModel productModel = new ProductModel();
        //            productModel.ProductId = Convert.ToInt32(dataTable.Rows[i]["ID"]);
        //            productModel.ProductName = dataTable.Rows[i]["ProductName"].ToString();
        //            productModel.ProductPrice = Convert.ToDecimal(dataTable.Rows[i]["ProductPrice"]);
        //            productModel.ProductColor = dataTable.Rows[i]["ProductColor"].ToString();
        //            productModel.EntryDate = Convert.ToDateTime(dataTable.Rows[i]["ProductEntryDate"]);
        //            products.Add(productModel);
        //        }
        //    }
        //    return products;
        //}
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
                    productModel.ProductColor = dataTable.Rows[i]["ProductColor"].ToString();
                    products.Add(productModel);
                }
            }
            return products;
        }
        public async Task AddProduct(ProductModel product)
        {
            string cacheKey = "ID";
            using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("Insert into Product values(@ProductId, @ProductName, @ProductPrice, GETDATE(),@ProductColor)", con);
                cmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
                cmd.Parameters.AddWithValue("@ProductPrice", product.ProductPrice);
                cmd.Parameters.AddWithValue("@ProductColor", product.ProductColor);
                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
            
            await cache.RemoveAsync(cacheKey);
        }
        public async Task UpdateProduct(ProductModel product)
        {
            string cacheKey = "ID";
            using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("UPDATE Product SET ProductName = @ProductName, ProductPrice = @ProductPrice, ProductEntryDate = GETDATE(), ProductColor = @ProductColor WHERE ID = @ProductId", con);
                cmd.Parameters.AddWithValue("@ProductId", product.ProductId);
                cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
                cmd.Parameters.AddWithValue("@ProductPrice", product.ProductPrice);
                cmd.Parameters.AddWithValue("@ProductColor", product.ProductColor);
                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
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
            string cacheKey = $"ID:{id}";
            string cachedData = await cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<ProductModel>(cachedData);
            }
            else
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

                        // Store the retrieved data in the cache
                        await cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(product), new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) 
                        });

                        return product;
                    }
                }
            }

            return null;
        }
        public async Task<ProductModel> GetProductByName(string name)
        {
            string cacheKey = $"ID:{name}";
            string cachedData = await cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonConvert.DeserializeObject<ProductModel>(cachedData);
            }
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
                    await cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(product), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) 
                    });
                    return product;
                }
                return null;
            }
        }
        public async Task<ProductModel> GetProductByNamePrice(string name, float price)
        {
            string cacheKey = $"ID:{name}:{price}";

            // Check if data exists in cache
            string cachedData = await cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                // If data is found in cache, return it
                return JsonConvert.DeserializeObject<ProductModel>(cachedData);
            }
            else
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
                        await cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(product), new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) 
                        });
                        return product;
                    }
                }
            }

            return null;
        }
        public async Task<int> GetTotalID()
        {
            string cacheKey = "TotalID";
            string cachedData = await cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                // If total ID count exists in cache, return it directly
                return int.Parse(cachedData);
            }
            else
            {
                using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
                {
                    SqlCommand cmd = new SqlCommand("SELECT COUNT(ID) as CountID FROM Product", con);
                    await con.OpenAsync();
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        int totalCount = Convert.ToInt32(reader["CountID"]);
                        await cache.SetStringAsync(cacheKey, totalCount.ToString(), new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) 
                        });

                        return totalCount;
                    }
                    else
                    {
                        return 0; 
                    }
                }
            }
        }
        public async Task<List<ProductModel>> GetColorProducts(string color)
        {
            string cacheKey = $"ID:{color}";
            string cachedData = await cache.GetStringAsync(cacheKey);
            List<ProductModel> products;

            if (!string.IsNullOrEmpty(cachedData))
            {
                products = JsonConvert.DeserializeObject<List<ProductModel>>(cachedData);
            }
            else
            {
                products = await GetColorProductsFromSource(color);
                string serializedData = JsonConvert.SerializeObject(products);
                await cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(35)
                });
            }
            return products;
        }

        private async Task<List<ProductModel>> GetColorProductsFromSource(string color)
        {
            List<ProductModel> products = new List<ProductModel>();
            DataTable dataTable = new DataTable();
            using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            {
                SqlCommand cmd = new SqlCommand("SELECT * FROM Product WHERE ProductColor = @ProductColor", con);
                cmd.Parameters.AddWithValue("@ProductColor", color);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                await con.OpenAsync();
                adapter.Fill(dataTable);
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ProductModel productModel = new ProductModel();
                    productModel.ProductId = Convert.ToInt32(dataTable.Rows[i]["ID"]);
                    productModel.ProductPrice = Convert.ToDecimal(dataTable.Rows[i]["ProductPrice"]);
                    productModel.ProductName = dataTable.Rows[i]["ProductName"].ToString()??"";
                    productModel.EntryDate = Convert.ToDateTime(dataTable.Rows[i]["ProductEntryDate"]);
                    productModel.ProductColor = dataTable.Rows[i]["ProductColor"].ToString();
                    products.Add(productModel);
                }
            }
            return products;
        }

    }
}
