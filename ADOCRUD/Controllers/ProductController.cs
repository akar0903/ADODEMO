using ADOCRUD.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography.Xml;

namespace ADOCRUD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IConfiguration configuration;
        public ProductController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        [HttpGet]
        [Route("GetAll")]
        public async Task<List<ProductModel>> GetAllProduct()
        {
            List<ProductModel> ProductModels = new List<ProductModel>();
            DataTable dataTable = new DataTable();
            SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
            SqlCommand cmd = new SqlCommand("select * from Product",con);
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            adapter.Fill(dataTable);
            for(int i = 0; i < dataTable.Rows.Count; i++)
            {
                ProductModel productModel = new ProductModel();
                productModel.ProductId = Convert.ToInt32(dataTable.Rows[i]["ID"]);
                productModel.ProductName = dataTable.Rows[i]["ProductName"].ToString();
                productModel.ProductPrice = Convert.ToDecimal(dataTable.Rows[i]["ProductPrice"]);
                productModel.EntryDate = Convert.ToDateTime(dataTable.Rows[i]["ProductEntryDate"]);
                ProductModels.Add(productModel);
            }
            return ProductModels;
        }
        [HttpPost]
        [Route("PostProduct")]
        public async Task<IActionResult> PostProduct(ProductModel obj)
        {
            try
            {
                SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = new SqlCommand("Insert into Product values('"+ obj.ProductId +"','" + obj.ProductName + "','" + obj.ProductPrice + "',getDate())", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                return Ok(obj);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }
        [HttpPost]
        [Route("UpdateProduct")]
        public async Task<IActionResult> UpdateProduct(ProductModel obj)
        {
            try
            {
                SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = new SqlCommand("UPDATE Product SET ProductName='" + obj.ProductName + "', ProductPrice='" + obj.ProductPrice + "', ProductEntryDate = GETDATE() WHERE ID='" + obj.ProductId + "'", con);
                con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
                return Ok(obj);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        //[HttpPost]
        //[Route("Product")]
        //public async Task<IActionResult> Product(ProductModel obj)
        //{
        //    SqlConnection con = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        //    SqlCommand cmd;
        //    SqlDataAdapter da;
        //    DataTable dataTable = new DataTable();
        //    try
        //    {
        //        cmd = new SqlCommand("Product",con);
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        cmd.Parameters.AddWithValue("@action", obj.ActionId);
        //        cmd.Parameters.AddWithValue("@ID", obj.ProductId);
        //        cmd.Parameters.AddWithValue("@ProductName", obj.ProductName);
        //        cmd.Parameters.AddWithValue("@ProductPrice", obj.ProductPrice);
        //        da= new SqlDataAdapter();
        //        da.Fill(dataTable);
        //        if (obj.ActionId == 4)
        //        {
        //            List<ProductModel> ProductModels = new List<ProductModel>();
        //            for (int i = 0; i < dataTable.Rows.Count; i++)
        //            {
        //                ProductModel productModel = new ProductModel();
        //                productModel.ProductId = Convert.ToInt32(dataTable.Rows[i]["ID"]);
        //                productModel.ProductName = dataTable.Rows[i]["ProductName"].ToString();
        //                productModel.ProductPrice = Convert.ToDecimal(dataTable.Rows[i]["ProductPrice"]);
        //                productModel.EntryDate = Convert.ToDateTime(dataTable.Rows[i]["ProductEntryDate"]);
        //                ProductModels.Add(productModel);
        //            }
        //            return Ok(ProductModels);
        //        }
        //        else
        //        {
        //            return Ok(obj); 
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
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
                        int rowsAffected = cmd.ExecuteNonQuery();
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
