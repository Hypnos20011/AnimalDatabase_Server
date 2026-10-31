using AnimalDatabase.Api.Models;
using Dapper;
using MySqlConnector;

namespace AnimalDatabase.Api.Data;

public class StatsRepository : IStatsRepository
{
    private readonly string _connectionString;

    public StatsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AnimalDatabase")
                            ?? throw new InvalidOperationException("Connection string 'AnimalDatabase' not found.");
    }

    public async Task<AnimalStats> GetStatsAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        var stats = new AnimalStats();
        stats.TotalAnimals = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM animals");
        stats.DomesticatedAnimals =
            await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM animals WHERE is_domesticated = 1");

        var classRows = await connection.QueryAsync<(string Name, long Count)>(
            "SELECT COALESCE(taxonomic_class, 'unknown'), COUNT(*) FROM animals GROUP BY 1");
        stats.ByClass = classRows.ToDictionary(row => row.Name, row => (int)row.Count);
        var iucnRows = await connection.QueryAsync<(string Name, long Count)>(
            "SELECT COALESCE(iucn_status, 'unknown'), COUNT(*) FROM animals GROUP BY 1");
        stats.ByIucnStatus = iucnRows.ToDictionary(row => row.Name, row => (int)row.Count);

        return stats;
    }
}