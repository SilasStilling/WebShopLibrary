using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace WebShopLibrary.Database
{
    public class LogDBConnection
    {
        private readonly string _connectionStringLog;

        public LogDBConnection(string connectionStringLog)
        {
            _connectionStringLog = connectionStringLog;
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionStringLog);
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, SqlParameter[] parameters = null)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var dataTable = new DataTable();
                        dataTable.Load(reader);
                        return dataTable;
                    }
                }
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string query, SqlParameter[] parameters = null)
        {
            using (var connection = GetConnection())
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
