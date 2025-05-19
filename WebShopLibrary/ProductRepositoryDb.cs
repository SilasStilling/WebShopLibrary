using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using WebShopLibrary.Database;

namespace WebShopLibrary
{
    public class ProductRepositoryDb
    {
        private readonly DBConnection _dbConnection;
        private readonly LogService _logService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductRepositoryDb(DBConnection dbConnection, LogService logService, IHttpContextAccessor httpContextAccessor)
        {
            _dbConnection = dbConnection;
            _logService = logService;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetCurrentUsername()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Anonymous";
        }

        public IEnumerable<Product> GetAll()
        {
            var products = new List<Product>();
            var connection = _dbConnection.GetConnection();
            var cmd = new SqlCommand("SELECT Id, Name, Model, Price, ImageData FROM Products", connection);

            try
            {
                connection.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var product = new Product
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Model = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Price = reader.GetDouble(3),
                        ImageData = reader.IsDBNull(4) ? null : (byte[])reader["ImageData"]
                    };
                    products.Add(product);
                }

                _logService.LogAsync("GetAllProducts", GetCurrentUsername(), "Success").Wait();
            }
            catch (Exception ex)
            {
                _logService.LogAsync("GetAllProducts", GetCurrentUsername(), "Failed", ex.Message).Wait();
            }
            finally
            {
                connection.Close();
            }
            return products;
        }

        public Product? Get(int id)
        {
            var connection = _dbConnection.GetConnection();
            var cmd = new SqlCommand("SELECT * FROM Products WHERE Id = @Id", connection);
            cmd.Parameters.AddWithValue("@Id", id);

            try
            {
                connection.Open();
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    _logService.LogAsync("GetProductById", GetCurrentUsername(), "Success").Wait();

                    return new Product
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Model = reader.GetString(2),
                        Price = reader.GetDouble(3),
                        ImageData = reader.IsDBNull(4) ? null : (byte[])reader["ImageData"]
                    };
                }
            }
            catch (Exception ex)
            {
                _logService.LogAsync("GetProductById", GetCurrentUsername(), "Failed", ex.Message).Wait();
            }
            finally
            {
                connection.Close();
            }
            return null;
        }

        public Product Add(Product product)
        {
            try
            {
                product.Validate();

                var connection = _dbConnection.GetConnection();
                var cmd = new SqlCommand("INSERT INTO Products (Name, Model, Price, ImageData) OUTPUT INSERTED.Id VALUES (@Name, @Model, @Price, @ImageData)", connection);
                cmd.Parameters.AddWithValue("@Name", product.Name);
                cmd.Parameters.AddWithValue("@Model", product.Model);
                cmd.Parameters.AddWithValue("@Price", product.Price);
                cmd.Parameters.AddWithValue("@ImageData", product.ImageData ?? (object)DBNull.Value);

                connection.Open();
                product.Id = (int)cmd.ExecuteScalar();
                _logService.LogAsync("CreateProduct", GetCurrentUsername(), "Success").Wait();
            }
            catch (Exception ex)
            {
                _logService.LogAsync("CreateProduct", GetCurrentUsername(), "Failed", ex.Message).Wait();
            }

            return product;
        }

        public Product Update(Product product)
        {
            try
            {
                product.Validate();

                var connection = _dbConnection.GetConnection();
                var cmd = new SqlCommand("UPDATE Products SET Name = @Name, Model = @Model, Price = @Price, ImageData = @ImageData WHERE Id = @Id", connection);
                cmd.Parameters.AddWithValue("@Id", product.Id);
                cmd.Parameters.AddWithValue("@Name", product.Name);
                cmd.Parameters.AddWithValue("@Model", product.Model);
                cmd.Parameters.AddWithValue("@Price", product.Price);
                cmd.Parameters.AddWithValue("@ImageData", product.ImageData ?? (object)DBNull.Value);

                connection.Open();
                cmd.ExecuteNonQuery();
                _logService.LogAsync("UpdateProduct", GetCurrentUsername(), "Success").Wait();
            }
            catch (Exception ex)
            {
                _logService.LogAsync("UpdateProduct", GetCurrentUsername(), "Failed", ex.Message).Wait();
            }

            return product;
        }

        public void Remove(int id)
        {
            var connection = _dbConnection.GetConnection();
            var cmd = new SqlCommand("DELETE FROM Products WHERE Id = @Id", connection);
            cmd.Parameters.AddWithValue("@Id", id);

            try
            {
                connection.Open();
                cmd.ExecuteNonQuery();
                _logService.LogAsync("DeleteProduct", GetCurrentUsername(), "Success").Wait();
            }
            catch (Exception ex)
            {
                _logService.LogAsync("DeleteProduct", GetCurrentUsername(), "Failed", ex.Message).Wait();
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
