using Npgsql;
using Dapper;
using Sms.Core.Models;

namespace Sms.Z1.Services
{
    public class PgDatabaseService : IDatabaseService
    {
        private readonly string _connString;

        public PgDatabaseService(string connString)
        {
            _connString = connString;
        }
        public async Task InitSchemaAsync()
        {
            using var conn = new NpgsqlConnection(_connString);
            await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS menu_items (
                id TEXT PRIMARY KEY,
                article TEXT,
                name TEXT,
                price NUMERIC,
                full_path TEXT
            );");
        }

        public async Task SaveMenuItemsAsync(IEnumerable<MenuItem> items)
        {
            using var conn = new NpgsqlConnection(_connString);
            const string sql = @"
            INSERT INTO menu_items (id, article, name, price, full_path)
            VALUES (@Id, @Article, @Name, @Price, @FullPath)
            ON CONFLICT (id) DO UPDATE SET 
                name = EXCLUDED.name, 
                price = EXCLUDED.price;";
            await conn.ExecuteAsync(sql, items);
        }
    }
}
