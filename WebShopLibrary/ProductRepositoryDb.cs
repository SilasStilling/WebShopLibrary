using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using WebShopLibrary.Database;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Http.Internal;

namespace WebShopLibrary
{
    public class ProductRepositoryDb
    {
        private readonly DBConnection _dbConnection;
        public ProductRepositoryDb(DBConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public IEnumerable<Product> GetAll()
        {
            var products = new List<Product>();
            var connection = _dbConnection.GetConnection();
            var cmd = new SqlCommand("SELECT Id, Name, Model, Price, ImageData FROM Products", connection); // Corrected column name

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
                        ImageData = reader.IsDBNull(4) ? null : (byte[])reader["ImageData"] // Now matches the updated schema
                    };
                    products.Add(product);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database error: {ex.Message}");
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
            finally
            {
                connection.Close();
            }
            return null;
        }

        public Product Add(Product product)
        {
            product.Validate();
            product.Id = 0;
            var connection = _dbConnection.GetConnection();
            var cmd = new SqlCommand("INSERT INTO Products (Name, Model, Price, ImageData) OUTPUT INSERTED.Id VALUES (@Name, @Model, @Price, @ImageData)", connection);
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Model", product.Model);
            cmd.Parameters.AddWithValue("@Price", product.Price);
            cmd.Parameters.AddWithValue("@ImageData", product.ImageData ?? (object)DBNull.Value); // Fjernet @Image

            try
            {
                connection.Open();
                product.Id = (int)cmd.ExecuteScalar();
            }
            finally
            {
                connection.Close();
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
            }
            finally
            {
                connection.Close();
            }
        }

        private byte[] ConvertToBytes(IFormFile formFile)
        {
            using (var memoryStream = new MemoryStream())
            {
                formFile.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private IFormFile ConvertToFormFile(byte[] bytes, string fileName)
        {
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, fileName, fileName);
        }
    }
}