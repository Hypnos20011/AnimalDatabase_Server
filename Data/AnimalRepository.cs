using AnimalDatabase.Api.Localization;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Security;
using AnimalDatabase.Api.Services;
using Dapper;
using MySqlConnector;

namespace AnimalDatabase.Api.Data;

public class AnimalRepository : IAnimalTranslationStore, IAnimalRepository
{
    private const string SelectAnimals =
        "SELECT a.id," +
        " a.created_at," +
        " a.updated_at," +
        "COALESCE(t.common_name, a.common_name) AS common_name, " +
        "a.scientific_name," +
        " a.family," +
        " a.taxonomic_class," +
        " a.is_domesticated," +
        "a.length_mm_min," +
        " a.length_mm_max," +
        " a.height_mm_min," +
        " a.height_mm_max," +
        "a.weight_grams_min," +
        " a.weight_grams_max," +
        " a.lifespan_months_min," +
        " a.lifespan_months_max,a.iucn_status," +
        " COALESCE(t.habitat, a.habitat) AS habitat, a.habitat_type, " +
        "COALESCE(t.range_region, a.range_region) AS range_region, " +
        "COALESCE(t.profile, a.profile) AS profile, " +
        "a.source_url, " +
        "a.status, a.submitted_by, a.reviewed_at, a.review_note, " +
        "(SELECT i.url FROM animal_images i WHERE i.animal_id = a.id ORDER BY i.sort_order, i.id LIMIT 1) " +
        "AS thumbnail_url FROM animals a" +
        " LEFT JOIN animal_translations t ON t.animal_id = a.id AND t.language = @Language";

    private readonly string _connectionString;

    public AnimalRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AnimalDatabase")
                            ?? throw new InvalidOperationException("Connection string 'AnimalDatabase' not found.");
    }

    public async Task<IReadOnlyList<Animal>> GetAllAsync(string language, Role? viewer)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<Animal>($"{SelectAnimals} WHERE a.status IN @Statuses ORDER BY common_name",
            new { Language = language, Statuses = AnimalVisibility.StatusesFor(viewer) });
        return rows.ToList();
    }

    public async Task<Animal?> GetByIdAsync(int id, string language, Role? viewer)
    {
        await using var connection = new MySqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<Animal>(
            $"{SelectAnimals} WHERE a.id = @Id AND a.status IN @Statuses",
            new { Id = id, Language = language, Statuses = AnimalVisibility.StatusesFor(viewer) });
    }

    public async Task<bool> IsVisibleAsync(int id, Role? viewer)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM animals WHERE id = @Id AND status IN @Statuses",
            new { Id = id, Statuses = AnimalVisibility.StatusesFor(viewer) });
        return count > 0;
    }

    public async Task<IReadOnlyList<AnimalImage>> GetImagesByAnimalIdAsync(int animalId, Role? viewer)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<AnimalImage>(
            "SELECT i.* FROM animal_images i JOIN animals a ON a.id = i.animal_id WHERE i.animal_id = @AnimalId AND a.status IN @Statuses ORDER BY i.sort_order, i.id",
            new { AnimalId = animalId, Statuses = AnimalVisibility.StatusesFor(viewer) });
        return rows.ToList();
    }

    public async Task<Animal> CreateAsync(CreateAnimalRequest request, Role submitter)
    {
        var status = submitter == Role.Admin ? AnimalStatus.Approved : AnimalStatus.Pending;
        var submittedBy = submitter == Role.Admin ? "admin" : "tester";
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var id = await connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO animals (
                common_name, scientific_name, family, taxonomic_class, is_domesticated,
                length_mm_min, length_mm_max, height_mm_min, height_mm_max,
                weight_grams_min, weight_grams_max, lifespan_months_min, lifespan_months_max,
                iucn_status, habitat, habitat_type, range_region, profile, source_url,
                status, submitted_by, reviewed_at)
            VALUES (
                @CommonName, @ScientificName, @Family, @TaxonomicClass, @IsDomesticated,
                @LengthMmMin, @LengthMmMax, @HeightMmMin, @HeightMmMax,
                @WeightGramsMin, @WeightGramsMax, @LifespanMonthsMin, @LifespanMonthsMax,
                @IucnStatus, @Habitat, @HabitatType, @RangeRegion, @Profile, @SourceUrl,
                @Status, @SubmittedBy, NULL);
            SELECT LAST_INSERT_ID();
            """,
            CreateParameters(request, status, submittedBy),
            transaction);

        await InsertImagesAsync(connection, transaction, id, request.Images);

        await transaction.CommitAsync();
        return (await GetByIdAsync(id, SupportedLanguages.Default, Role.Admin))!;
    }

    public async Task<AnimalWriteResult> UpdateAsync(int id, CreateAnimalRequest request, Role editor)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var status = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT status FROM animals WHERE id = @Id FOR UPDATE",
            new { Id = id },
            transaction);
        if (status is null)
        {
            await transaction.RollbackAsync();
            return new AnimalWriteResult(AnimalWriteStatus.NotFound, null);
        }

        if (editor == Role.Tester && status != AnimalStatus.Pending)
        {
            await transaction.RollbackAsync();
            return new AnimalWriteResult(AnimalWriteStatus.Forbidden, null);
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
                length_mm_min = @LengthMmMin,
                length_mm_max = @LengthMmMax,
                height_mm_min = @HeightMmMin,
                height_mm_max = @HeightMmMax,
                weight_grams_min = @WeightGramsMin,
                weight_grams_max = @WeightGramsMax,
                lifespan_months_min = @LifespanMonthsMin,
                lifespan_months_max = @LifespanMonthsMax,
                iucn_status = @IucnStatus,
                habitat = @Habitat,
                habitat_type = @HabitatType,
                range_region = @RangeRegion,
                profile = @Profile,
                source_url = @SourceUrl
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
        return new AnimalWriteResult(
            AnimalWriteStatus.Updated,
            await GetByIdAsync(id, SupportedLanguages.Default, Role.Admin));
    }

    public async Task<IReadOnlyList<Animal>> GetPendingAsync(string language)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<Animal>(
            $"{SelectAnimals} WHERE a.status = @Status ORDER BY a.created_at, a.id",
            new { Language = language, Status = AnimalStatus.Pending });
        return rows.ToList();
    }

    public async Task<bool> SaveTranslationAsync(int id, string language, AnimalTexts texts)
    {
        await using var connection = new MySqlConnection(_connectionString);

        var exists = await connection.ExecuteScalarAsync<int>
        (
            "SELECT COUNT(*) FROM animals WHERE id = @Id",
            new { Id = id }
        );
        if (exists == 0)
        {
            return false;
        }

        await connection.ExecuteAsync(
            """"
            INSERT INTO animal_translations(animal_id, language, common_name, habitat, range_region, profile)
            VALUES (@Id, @Language, @CommonName, @Habitat, @RangeRegion, @Profile)
            ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat),  range_region = VALUES(range_region), profile = VALUES(profile)
            """",
            new { Id = id, Language = language, texts.CommonName, texts.Habitat, texts.RangeRegion, texts.Profile });

        return true;
    }

    public async Task<AnimalTexts?> GetTranslationAsync(int id, string language)
    {
        await using var connection = new MySqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<AnimalTexts>(
            """
            SELECT common_name, habitat, range_region, profile
            FROM animal_translations 
            WHERE animal_id = @Id AND language = @Language
            """,
            new { Id = id, Language = language });
    }

    public async Task<EnglishAnimal?> GetEnglishAsync(int id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<EnglishAnimal>(
            """
            SELECT id, common_name, habitat, range_region, profile
            FROM animals
            WHERE id = @Id AND status = 'approved'
            """,
            new { Id = id });
    }

    public async Task<IReadOnlyList<EnglishAnimalRecord>> ListEnglishAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        var rows = await connection.QueryAsync<TranslationListRow>(
            """
            SELECT a.id,
                   a.common_name,
                   a.habitat,
                   a.range_region,
                   a.profile,
                   german.animal_id AS de_animal_id,
                   german.common_name AS de_common_name,
                   german.habitat AS de_habitat,
                   german.range_region AS de_range_region,
                   german.profile AS de_profile,
                   french.animal_id AS fr_animal_id,
                   french.common_name AS fr_common_name,
                   french.habitat AS fr_habitat,
                   french.range_region AS fr_range_region,
                   french.profile AS fr_profile
            FROM animals a
            LEFT JOIN animal_translations german ON german.animal_id = a.id AND german.language = 'de'
            LEFT JOIN animal_translations french ON french.animal_id = a.id AND french.language = 'fr'
            WHERE a.status = 'approved'
            ORDER BY a.id
            """);
        return rows.Select(ToRecord).ToList();
    }

    public async Task<AnimalDeleteResult> DeleteAsync(int id, bool pendingTesterOnly)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(
            pendingTesterOnly
                ? "DELETE FROM animals WHERE id = @Id AND status = 'pending' AND submitted_by = 'tester'"
                : "DELETE FROM animals WHERE id = @Id",
            new { Id = id });
        if (affected > 0)
        {
            return new AnimalDeleteResult(AnimalDeleteStatus.Deleted);
        }

        if (!pendingTesterOnly)
        {
            return new AnimalDeleteResult(AnimalDeleteStatus.NotFound);
        }

        var exists = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM animals WHERE id = @Id",
            new { Id = id });
        return new AnimalDeleteResult(exists == 0 ? AnimalDeleteStatus.NotFound : AnimalDeleteStatus.Forbidden);
    }

    public async Task<AnimalReviewResult> ApproveAsync(int id)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var affected = await connection.ExecuteAsync(
            "UPDATE animals SET status = 'approved', reviewed_at = CURRENT_TIMESTAMP, review_note = NULL WHERE id = @Id AND status = 'pending'",
            new { Id = id });
        return await GetReviewResultAsync(connection, id, affected);
    }

    public async Task<AnimalReviewResult> RejectAsync(int id, string? note)
    {
        await using var connection = new MySqlConnection(_connectionString);
        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        var affected = await connection.ExecuteAsync(
            "UPDATE animals SET status = 'rejected', reviewed_at = CURRENT_TIMESTAMP, review_note = @Note WHERE id = @Id AND status = 'pending'",
            new { Id = id, Note = trimmedNote });
        return await GetReviewResultAsync(connection, id, affected);
    }

    private static async Task<AnimalReviewResult> GetReviewResultAsync(MySqlConnection connection, int id, int affected)
    {
        if (affected > 0)
        {
            return new AnimalReviewResult(AnimalReviewStatus.Done);
        }

        var status = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT status FROM animals WHERE id = @Id",
            new { Id = id });
        return new AnimalReviewResult(status is null ? AnimalReviewStatus.NotFound : AnimalReviewStatus.NotPending);
    }

    private static EnglishAnimalRecord ToRecord(TranslationListRow row)
    {
        return new EnglishAnimalRecord
        {
            English = new EnglishAnimal
            {
                Id = row.Id,
                CommonName = row.CommonName,
                Habitat = row.Habitat,
                RangeRegion = row.RangeRegion,
                Profile = row.Profile
            },
            German = row.DeAnimalId is null
                ? null
                : new AnimalTexts
                {
                    CommonName = row.DeCommonName,
                    Habitat = row.DeHabitat,
                    RangeRegion = row.DeRangeRegion,
                    Profile = row.DeProfile
                },
            French = row.FrAnimalId is null
                ? null
                : new AnimalTexts
                {
                    CommonName = row.FrCommonName,
                    Habitat = row.FrHabitat,
                    RangeRegion = row.FrRangeRegion,
                    Profile = row.FrProfile
                }
        };
    }

    internal static DynamicParameters ToParameters(CreateAnimalRequest request)
    {
        var parameters = new DynamicParameters();
        parameters.Add("CommonName", request.CommonName);
        parameters.Add("ScientificName", request.ScientificName);
        parameters.Add("Family", request.Family);
        parameters.Add("TaxonomicClass", request.TaxonomicClass);
        parameters.Add("IsDomesticated", request.IsDomesticated);
        parameters.Add("LengthMmMin", request.LengthMmMin);
        parameters.Add("LengthMmMax", request.LengthMmMax);
        parameters.Add("HeightMmMin", request.HeightMmMin);
        parameters.Add("HeightMmMax", request.HeightMmMax);
        parameters.Add("WeightGramsMin", request.WeightGramsMin);
        parameters.Add("WeightGramsMax", request.WeightGramsMax);
        parameters.Add("LifespanMonthsMin", request.LifespanMonthsMin);
        parameters.Add("LifespanMonthsMax", request.LifespanMonthsMax);
        parameters.Add("IucnStatus", request.IucnStatus);
        parameters.Add("Habitat", request.Habitat);
        parameters.Add("HabitatType", request.HabitatType);
        parameters.Add("RangeRegion", request.RangeRegion);
        parameters.Add("Profile", request.Profile);
        parameters.Add("SourceUrl", request.SourceUrl);
        return parameters;
    }

    private static DynamicParameters CreateParameters(CreateAnimalRequest request, string status, string submittedBy)
    {
        var parameters = ToParameters(request);
        parameters.Add("Status", status);
        parameters.Add("SubmittedBy", submittedBy);
        return parameters;
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
                INSERT INTO animal_images (animal_id, kind, url, caption, attribution, source_page_url, sort_order)
                VALUES (@AnimalId, @Kind, @Url, @Caption, @Attribution, @SourcePageUrl, @SortOrder);
                """,
                new
                {
                    AnimalId = animalId, image.Kind, Url = WikimediaUrl.Normalize(image.Url), image.Caption, image.Attribution, image.SourcePageUrl,
                    SortOrder = sortOrder++
                },
                transaction);
        }
    }

    private sealed class TranslationListRow
    {
        public int Id { get; set; }
        public string? CommonName { get; set; }
        public string? Habitat { get; set; }
        public string? RangeRegion { get; set; }
        public string? Profile { get; set; }
        public int? DeAnimalId { get; set; }
        public string? DeCommonName { get; set; }
        public string? DeHabitat { get; set; }
        public string? DeRangeRegion { get; set; }
        public string? DeProfile { get; set; }
        public int? FrAnimalId { get; set; }
        public string? FrCommonName { get; set; }
        public string? FrHabitat { get; set; }
        public string? FrRangeRegion { get; set; }
        public string? FrProfile { get; set; }
    }
}