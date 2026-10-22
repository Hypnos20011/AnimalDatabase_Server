using AnimalDatabase.Api.Models;
using Dapper;
using MySqlConnector;

namespace AnimalDatabase.Api.Data;

public class SearchRepository
{
    private readonly string _connectionString;

    public SearchRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AnimalDatabase")
                            ?? throw new InvalidOperationException("Connection string 'AnimalDatabase' not found.");
    }

    public async Task<bool> AddAsync(CreateSearchRequest request)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(
            """
            INSERT INTO searches (query_text, animal_id)
            SELECT @QueryText, a.id FROM animals a WHERE a.id = @AnimalId;
            """,
            new { QueryText = (request.QueryText ?? string.Empty).Trim(), request.AnimalId });
        return affected > 0;
    }

    public async Task<IReadOnlyList<TopSearch>> GetTopAsync(int limit)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<TopSearch>(
            """
            SELECT
                a.id AS animal_id,
                a.common_name,
                (SELECT i.url FROM animal_images i WHERE i.animal_id = a.id ORDER BY i.sort_order, i.id LIMIT 1) AS thumbnail_url,
                COUNT(*) AS search_count
            FROM searches s
            JOIN animals a ON a.id = s.animal_id
            GROUP BY a.id, a.common_name
            ORDER BY search_count DESC, a.common_name
            LIMIT @Limit;
            """,
            new { Limit = limit });
        return rows.ToList();
    }
}