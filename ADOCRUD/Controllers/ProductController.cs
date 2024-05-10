using ADOCRUD.Interfaces;
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
        private readonly IProductRepository productRepository;
        //private readonly IProductManager manager;
        //private string keyname = "ProductData";
       
        public ProductController(IConfiguration configuration, IDistributedCache cache, IProductRepository productRepository)
        {
            this.configuration = configuration;
            this.cache = cache;
            this.productRepository = productRepository;
        }
        [HttpGet]
        [Route("GetAll")]
        public async Task<IActionResult> GetAllProduct()
        {
            try
            {
                var products = await productRepository.GetAllProducts();
                return Ok(products);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching products.");
            }
        }

        [HttpPost]
        [Route("PostProduct")]
        public async Task<IActionResult> AddProduct(ProductModel product)
        {
            try
            {
                await productRepository.AddProduct(product);
                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        [HttpPost]
        [Route("UpdateProduct")]
        public async Task<IActionResult> UpdateProduct(ProductModel product)
        {
            try
            {
                await productRepository.UpdateProduct(product);
                return Ok(product);
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
                var result = await productRepository.DeleteProduct(id);
                return result != null ? Ok($"Product with ID {id} deleted") : NotFound($"Product with ID {id} not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("GetProductById")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var product = await productRepository.GetProductById(id);
                if (product != null)
                    return Ok(product);
                else
                    return NotFound("Product not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("GetProductByName")]
        public async Task<IActionResult> GetProductByName(string name)
        {
            try
            {
                var product = await productRepository.GetProductByName(name);
                if (product != null)
                    return Ok(product);
                else
                    return NotFound("Product not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("GetProductByNamePrice")]
        public async Task<IActionResult> GetProductByNamePrice(string name,float price)
        {
            try
            {
                var product = await productRepository.GetProductByNamePrice(name,price);
                if (product != null)
                    return Ok(product);
                else
                    return NotFound("Product not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("GetTotalId")]
        public async Task<IActionResult> GetTotalID()
        {
            try
            {
                var totalCount = await productRepository.GetTotalID();
                return Ok(totalCount);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        [HttpGet]
        [Route("GetAllcolor")]
        public async Task<IActionResult> GetColorProducts(string color)
        {
            try
            {
                var products = await productRepository.GetColorProducts(color);
                return Ok(products);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "An error occurred while fetching products.");
            }
        }
    }
}
