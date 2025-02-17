using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebShopLibrary.Database;


namespace WebShopLibrary.Database
{
    public class DBConnection
    {
        private readonly string _connectionString;
        public DBConnection(string connectionString)
        {
            _connectionString = connectionString;
        }
        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
