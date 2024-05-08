using ADOCRUD.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Data;
using System.Security.Cryptography.Xml;

namespace ADOCRUD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly IDistributedCache cache;
        //private string keyname = "ProductData";
       
        public ProductController(IConfiguration configuration, IDistributedCache cache)
        {
            this.configuration = configuration;
            this.cache = cache;
        }
        [HttpGet]
        [Route("GetAll")]
        public async Task<List<ProductModel>> GetAllProduct()
        {
            try
            {
                string cacheKey = "ID";
                string cachedData = await cache.GetStringAsync(cacheKey);
                List<ProductModel> ProductModels;
                if (!string.IsNullOrEmpty(cachedData))
                {
                    ProductModels = JsonConvert.DeserializeObject<List<ProductModel>>(cachedData);
                }
                else
                {
                    ProductModels = await GetProductDataFromSource(); 
                    string serializedData = JsonConvert.SerializeObject(ProductModels);
                    await cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(35) 
                    });
                }
                return ProductModels;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw; 
            }
        }
        private async Task<List<ProductModel>> GetProductDataFromSource()
        {
            List<ProductModel> ProductModels = new List<ProductModel>();
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
                    ProductModels.Add(productModel);
                }
            }
            return ProductModels;
        }
        [HttpPost]
        [Route("PostProduct")]
        public async Task<IActionResult> PostProduct(ProductModel obj)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
                {
                    SqlCommand cmd = new SqlCommand("Insert into Product values(@ProductId, @ProductName, @ProductPrice, getDate())", con);
                    cmd.Parameters.AddWithValue("@ProductId", obj.ProductId);
                    cmd.Parameters.AddWithValue("@ProductName", obj.ProductName);
                    cmd.Parameters.AddWithValue("@ProductPrice", obj.ProductPrice);
                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
                string cacheKey = "ID";
                await cache.RemoveAsync(cacheKey); 
                return Ok(obj);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }   
        [HttpDelete]
        [Route("DeleteProduct")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
                {
                    con.Open();
                    string sqlQuery = "DELETE FROM Product WHERE ID = @ProductId";

                    using (SqlCommand cmd = new SqlCommand(sqlQuery, con))
                    {
                        
                        cmd.Parameters.AddWithValue("@ProductId", id);
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        string cacheKey = "ID";
                        await cache.RemoveAsync(cacheKey);
                        if (rowsAffected > 0)
                        {
                            
                            return Ok($"Product with ID {id} deleted successfully.");
                        }
                        else
                        {
                            
                            return NotFound($"Product with ID {id} not found.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while deleting the product: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("GetProductById")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = new SqlCommand("SELECT * FROM Product WHERE ID = @Id", con);
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    ProductModel product = new ProductModel
                    {
                        ProductId = Convert.ToInt32(reader["ID"]),
                        ProductName = reader["ProductName"].ToString(),
                        ProductPrice = Convert.ToDecimal(reader["ProductPrice"]),
                        EntryDate = Convert.ToDateTime(reader["ProductEntryDate"])
                    };
                    con.Close();
                    return Ok(product);
                }
                else
                {
                    con.Close();
                    return NotFound("Product not found");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        [HttpGet]
        [Route("GetProductByName")]
        public async Task<IActionResult> GetProductByName(string name)
        {
            try
            {
                SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = new SqlCommand("SELECT * FROM Product WHERE ProductName = @Name", con);
                cmd.Parameters.AddWithValue("@Name", name);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    ProductModel product = new ProductModel
                    {
                        ProductId = Convert.ToInt32(reader["ID"]),
                        ProductName = reader["ProductName"].ToString(),
                        ProductPrice = Convert.ToDecimal(reader["ProductPrice"]),
                        EntryDate = Convert.ToDateTime(reader["ProductEntryDate"])
                    };

                    con.Close();
                    return Ok(product);
                }
                else
                {
                    con.Close();
                    return NotFound("Product not found");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        [HttpGet]
        [Route("GetProductByNamePrice")]
        public async Task<IActionResult> GetProductByNamePrice(string name,float price)
        {
            try
            {
                SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = new SqlCommand("SELECT * FROM Product WHERE ProductName = @Name AND ProductPrice = @Price", con);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Price", price);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    ProductModel product = new ProductModel
                    {
                        ProductId = Convert.ToInt32(reader["ID"]),
                        ProductName = reader["ProductName"].ToString(),
                        ProductPrice = Convert.ToDecimal(reader["ProductPrice"]),
                        EntryDate = Convert.ToDateTime(reader["ProductEntryDate"])
                    };

                    con.Close();
                    return Ok(product);
                }
                else
                {
                    con.Close();
                    return NotFound("Product not found");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        [HttpGet]
        [Route("GetTotalId")]
        public async Task<IActionResult> GetTotalID()
        {
            try
            {
                SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = new SqlCommand("SELECT COUNT(ID) as CountID From Product", con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {    
                    int totalCount = Convert.ToInt32(reader["CountID"]);
                    con.Close();
                    string message = $"Total ID present in database are: {totalCount}";
                    return Ok(message);
                }
                else
                {
                    con.Close();
                    return NotFound("Product not found");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
