using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using WebShopLibrary.Database;

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
            var cmd = new SqlCommand("SELECT * FROM Products", connection);

            try
            {
                connection.Open();
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var product = new Product
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Model = reader.GetString(2),
                        Price = reader.GetDouble(3)
                    };
                    products.Add(product);
                }
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
                        Price = reader.GetDouble(3)
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
            var cmd = new SqlCommand("INSERT INTO Products (Name, Model, Price) OUTPUT INSERTED.Id VALUES (@Name, @Model, @Price)", connection);
            cmd.Parameters.AddWithValue("@Name", product.Name);
            cmd.Parameters.AddWithValue("@Model", product.Model);
            cmd.Parameters.AddWithValue("@Price", product.Price);
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

    }

}
