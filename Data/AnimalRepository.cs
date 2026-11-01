using AnimalDatabase.Api.Models;
using Dapper;
using MySqlConnector;

namespace AnimalDatabase.Api.Data;

public class AnimalRepository
{
    private const string SelectAnimals =
        "SELECT a.*, (SELECT i.url FROM animal_images i WHERE i.animal_id = a.id ORDER BY i.sort_order, i.id LIMIT 1) AS thumbnail_url FROM animals a";

    private readonly string _connectionString;

    public AnimalRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AnimalDatabase")
                            ?? throw new InvalidOperationException("Connection string 'AnimalDatabase' not found.");
    }

    public async Task<IReadOnlyList<Animal>> GetAllAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<Animal>($"{SelectAnimals} ORDER BY a.common_name");
        return rows.ToList();
    }

    public async Task<Animal?> GetByIdAsync(int id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<Animal>(
            $"{SelectAnimals} WHERE a.id = @Id",
            new { Id = id });
    }

    public async Task<IReadOnlyList<AnimalImage>> GetImagesByAnimalIdAsync(int animalId)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<AnimalImage>(
            "SELECT * FROM animal_images WHERE animal_id = @AnimalId ORDER BY sort_order, id",
            new { AnimalId = animalId });
        return rows.ToList();
    }

    public async Task<Animal> CreateAsync(CreateAnimalRequest request)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var id = await connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO animals (
                common_name, scientific_name, family, taxonomic_class, is_domesticated,
                size_length_cm_min, size_length_cm_max, size_height_cm_min, size_height_cm_max,
                weight_kg_min, weight_kg_max, lifespan_years_min, lifespan_years_max,
                iucn_status, habitat, habitat_type, range_region, profile)
            VALUES (
                @CommonName, @ScientificName, @Family, @TaxonomicClass, @IsDomesticated,
                @SizeLengthCmMin, @SizeLengthCmMax, @SizeHeightCmMin, @SizeHeightCmMax,
                @WeightKgMin, @WeightKgMax, @LifespanYearsMin, @LifespanYearsMax,
                @IucnStatus, @Habitat, @HabitatType, @RangeRegion, @Profile);
            SELECT LAST_INSERT_ID();
            """,
            ToParameters(request),
            transaction);

        await InsertImagesAsync(connection, transaction, id, request.Images);

        await transaction.CommitAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<Animal?> UpdateAsync(int id, CreateAnimalRequest request)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var exists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM animals WHERE id = @Id",
            new { Id = id },
            transaction);
        if (exists == 0)
        {
            return null;
        }

        var parameters = ToParameters(request);
        parameters.Add("Id", id);
        await connection.ExecuteAsync(
            """
            UPDATE animals SET
                common_name = @CommonName,
                scientific_name = @ScientificName,
                family = @Family,
                taxonomic_class = @TaxonomicClass,
                is_domesticated = @IsDomesticated,
                size_length_cm_min = @SizeLengthCmMin,
                size_length_cm_max = @SizeLengthCmMax,
                size_height_cm_min = @SizeHeightCmMin,
                size_height_cm_max = @SizeHeightCmMax,
                weight_kg_min = @WeightKgMin,
                weight_kg_max = @WeightKgMax,
                lifespan_years_min = @LifespanYearsMin,
                lifespan_years_max = @LifespanYearsMax,
                iucn_status = @IucnStatus,
                habitat = @Habitat,
                habitat_type = @HabitatType,
                range_region = @RangeRegion,
                profile = @Profile
            WHERE id = @Id;
            """,
            parameters,
            transaction);

        await connection.ExecuteAsync(
            "DELETE FROM animal_images WHERE animal_id = @Id",
            new { Id = id },
            transaction);
        await InsertImagesAsync(connection, transaction, id, request.Images);

        await transaction.CommitAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(
            "DELETE FROM animals WHERE id = @Id",
            new { Id = id });
        return affected > 0;
    }

    private static DynamicParameters ToParameters(CreateAnimalRequest request)
    {
        return new DynamicParameters(new
        {
            request.CommonName,
            request.ScientificName,
            request.Family,
            request.TaxonomicClass,
            request.IsDomesticated,
            request.SizeLengthCmMin,
            request.SizeLengthCmMax,
            request.SizeHeightCmMin,
            request.SizeHeightCmMax,
            request.WeightKgMin,
            request.WeightKgMax,
            request.LifespanYearsMin,
            request.LifespanYearsMax,
            request.IucnStatus,
            request.Habitat,
            request.HabitatType,
            request.RangeRegion,
            request.Profile
        });
    }

    private static async Task InsertImagesAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        int animalId,
        IEnumerable<CreateAnimalImageRequest> images)
    {
        var sortOrder = 0;
        foreach (var image in images)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO animal_images (animal_id, kind, url, caption, sort_order)
                VALUES (@AnimalId, @Kind, @Url, @Caption, @SortOrder);
                """,
                new { AnimalId = animalId, image.Kind, image.Url, image.Caption, SortOrder = sortOrder++ },
                transaction);
        }
    }
}