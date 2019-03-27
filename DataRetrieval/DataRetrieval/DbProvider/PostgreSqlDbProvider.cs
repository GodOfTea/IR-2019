using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace DataRetrieval.DbProvider
{
    public class PostgreSqlDbProvider
    {
        private readonly string connectionString;

        public PostgreSqlDbProvider(
            string connectionString = "Host=84.201.147.162;Port=5432;Database=CoderLiQ;Username=developer;Password=rtfP@ssw0rd")
        {
            this.connectionString = connectionString;
        }

        public async Task<IEnumerable<Dictionary<string, object>>> ExecuteReadSqlCommandAsync(string command)
        {
            var result = new List<Dictionary<string, object>>();
//            var result2 = new List<object[]>();

            using (var conn = new NpgsqlConnection(connectionString))
            {
                await conn.OpenAsync();
                // Retrieve all rows
                using (var cmd = new NpgsqlCommand(command, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
//                            var objs = new object[reader.FieldCount];
//                            reader.GetValues(objs);
//                            result2.Add(objs);

                            var obj = new Dictionary<string, object>();

                            for (var i = 0; i < reader.FieldCount; i++) obj[reader.GetName(i)] = reader.GetValue(i);

                            result.Add(obj);
                        }
                    }
                }
            }

//            return result2;
            return result;
        }

        public async Task<IEnumerable<Dictionary<string, object>>> GetRowsAsync(string tableName = "movies", string fields = "*", string condition = "true", int count = int.MaxValue)
        {
            var command = $"SELECT {fields} FROM {tableName} WHERE {condition} LIMIT {count}";
            return await ExecuteReadSqlCommandAsync(command);
        }
    }

}