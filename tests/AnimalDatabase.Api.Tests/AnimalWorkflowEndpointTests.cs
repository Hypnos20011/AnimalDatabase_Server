using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalDatabase.Api.Tests;

public class AnimalWorkflowEndpointTests
{
    [Fact]
    public async Task Create_and_update_round_trip_canonical_measurements()
    {
        var repository = new FakeAnimalRepository();
        using var host = new TestHost(repository);
        var client = host.CreateClient();
        var create = new CreateAnimalRequest
        {
            CommonName = "Canonical animal",
            LengthMmMin = 12.34m,
            LengthMmMax = 56.78m,
            HeightMmMin = 9.01m,
            HeightMmMax = 10.02m,
            WeightGramsMin = 2m,
            WeightGramsMax = 136000000m,
            LifespanMonthsMin = 0.1m,
            LifespanMonthsMax = 99999.9m
        };
        using var createRequest = CreateRequest(HttpMethod.Post, "/api/animals", ApiFactory.TestKey,
            JsonContent.Create(create));
        using var created = await client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdDocument = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var createdAnimal = createdDocument.RootElement;
        AssertCanonicalMeasurements(createdAnimal, 12.34m, 56.78m, 9.01m, 10.02m, 2m, 136000000m, 0.1m, 99999.9m);
        var id = createdAnimal.GetProperty("id").GetInt32();

        var update = new CreateAnimalRequest
        {
            CommonName = "Updated canonical animal",
            LengthMmMin = 87.65m,
            LengthMmMax = 98.76m,
            HeightMmMin = 1.23m,
            HeightMmMax = 4.56m,
            WeightGramsMin = 0.001m,
            WeightGramsMax = 2000.005m,
            LifespanMonthsMin = 1.1m,
            LifespanMonthsMax = 12.9m
        };
        using var updateRequest = CreateRequest(HttpMethod.Put, $"/api/animals/{id}", ApiFactory.TestKey,
            JsonContent.Create(update));
        using var updated = await client.SendAsync(updateRequest);

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var updatedDocument = JsonDocument.Parse(await updated.Content.ReadAsStringAsync());
        AssertCanonicalMeasurements(updatedDocument.RootElement, 87.65m, 98.76m, 1.23m, 4.56m, 0.001m,
            2000.005m, 1.1m, 12.9m);
    }

    [Fact]
    public async Task Get_all_passes_the_viewer_role_and_treats_invalid_keys_as_visitors()
    {
        var repository = new FakeAnimalRepository
        {
            AllResult = [new Animal { Id = 1, CommonName = "Visible", Status = AnimalStatus.Approved }]
        };
        using var host = new TestHost(repository);
        var client = host.CreateClient();
        (string? Key, Role? Role)[] callers =
        [
            (null, null),
            ("stale-key", null),
            (ApiFactory.TesterKey, Role.Tester),
            (ApiFactory.TestKey, Role.Admin)
        ];

        foreach (var caller in callers)
        {
            using var request = CreateRequest(HttpMethod.Get, "/api/animals", caller.Key);
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(caller.Role, repository.GetAllViewers.Last());
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Visible", Assert.Single(document.RootElement.EnumerateArray()).GetProperty("commonName").GetString());
        }
    }

    [Fact]
    public async Task Pending_and_rejected_animals_are_visible_only_to_the_correct_callers_on_all_public_reads()
    {
        (string? Key, Role? Role)[] callers =
        [
            (null, null),
            ("stale-key", null),
            (ApiFactory.TesterKey, Role.Tester),
            (ApiFactory.TestKey, Role.Admin)
        ];
        string[] paths =
        [
            "/api/animals/1",
            "/api/animals/1/images",
            "/api/animals/1/translations/de"
        ];

        foreach (var status in new[] { AnimalStatus.Pending, AnimalStatus.Rejected })
        {
            var repository = new FakeAnimalRepository();
            repository.AddAnimal(1, status);
            using var host = new TestHost(repository);
            var client = host.CreateClient();

            foreach (var caller in callers)
            {
                var expected = status == AnimalStatus.Pending
                    ? caller.Role is null ? HttpStatusCode.NotFound : HttpStatusCode.OK
                    : caller.Role == Role.Admin ? HttpStatusCode.OK : HttpStatusCode.NotFound;

                foreach (var path in paths)
                {
                    using var request = CreateRequest(HttpMethod.Get, path, caller.Key);
                    using var response = await client.SendAsync(request);
                    Assert.Equal(expected, response.StatusCode);
                    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
                }
            }
        }
    }

    [Fact]
    public async Task Admin_create_is_approved_and_fills_translations()
    {
        var repository = new FakeAnimalRepository();
        var translationStore = new StubTranslationStore();
        using var host = new TestHost(repository, translationStore);
        var client = host.CreateClient();
        using var request = CreateRequest(HttpMethod.Post, "/api/animals", ApiFactory.TestKey,
            JsonContent.Create(new CreateAnimalRequest { CommonName = "Admin animal" }));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal([Role.Admin], repository.CreateRoles);
        Assert.Equal(AnimalStatus.Approved, repository.Animals.Single().Value.Status);
        Assert.Equal(1, translationStore.EnglishReadCount);
    }

    [Fact]
    public async Task Tester_create_is_pending_and_does_not_fill_translations()
    {
        var repository = new FakeAnimalRepository();
        var translationStore = new StubTranslationStore();
        using var host = new TestHost(repository, translationStore);
        var client = host.CreateClient();
        using var request = CreateRequest(HttpMethod.Post, "/api/animals", ApiFactory.TesterKey,
            JsonContent.Create(new CreateAnimalRequest { CommonName = "Tester animal" }));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal([Role.Tester], repository.CreateRoles);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(AnimalStatus.Pending, document.RootElement.GetProperty("status").GetString());
        Assert.Equal(0, translationStore.EnglishReadCount);
    }

    [Fact]
    public async Task Tester_cannot_update_approved_animals_but_can_update_pending_animals_without_filling()
    {
        var repository = new FakeAnimalRepository();
        repository.AddAnimal(1, AnimalStatus.Approved);
        repository.AddAnimal(2, AnimalStatus.Pending, "tester");
        var translationStore = new StubTranslationStore();
        using var host = new TestHost(repository, translationStore);
        var client = host.CreateClient();
        using var forbiddenRequest = CreateRequest(HttpMethod.Put, "/api/animals/1", ApiFactory.TesterKey,
            JsonContent.Create(new CreateAnimalRequest { CommonName = "Updated" }));
        using var forbidden = await client.SendAsync(forbiddenRequest);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal([Role.Tester], repository.UpdateRoles);
        Assert.Empty(repository.UpdatedIds);

        using var pendingRequest = CreateRequest(HttpMethod.Put, "/api/animals/2", ApiFactory.TesterKey,
            JsonContent.Create(new CreateAnimalRequest { CommonName = "Updated" }));
        using var pending = await client.SendAsync(pendingRequest);
        Assert.Equal(HttpStatusCode.OK, pending.StatusCode);
        Assert.Equal([2], repository.UpdatedIds);
        Assert.Equal(0, translationStore.EnglishReadCount);

        using var unknownRequest = CreateRequest(HttpMethod.Put, "/api/animals/99", ApiFactory.TesterKey,
            JsonContent.Create(new CreateAnimalRequest { CommonName = "Updated" }));
        using var unknown = await client.SendAsync(unknownRequest);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task Admin_can_update_any_status_and_fills_only_approved_animals()
    {
        foreach (var status in new[] { AnimalStatus.Approved, AnimalStatus.Pending, AnimalStatus.Rejected })
        {
            var repository = new FakeAnimalRepository();
            repository.AddAnimal(1, status);
            var translationStore = new StubTranslationStore();
            using var host = new TestHost(repository, translationStore);
            var client = host.CreateClient();
            using var request = CreateRequest(HttpMethod.Put, "/api/animals/1", ApiFactory.TestKey,
                JsonContent.Create(new CreateAnimalRequest { CommonName = "Updated" }));

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal([1], repository.UpdatedIds);
            Assert.Equal([Role.Admin], repository.UpdateRoles);
            Assert.Equal(status == AnimalStatus.Approved ? 1 : 0, translationStore.EnglishReadCount);
        }
    }

    [Fact]
    public async Task Admin_can_delete_every_status_and_tester_can_delete_only_their_pending_animal()
    {
        foreach (var status in new[] { AnimalStatus.Approved, AnimalStatus.Pending, AnimalStatus.Rejected })
        {
            var repository = new FakeAnimalRepository();
            repository.AddAnimal(1, status);
            using var host = new TestHost(repository);
            var client = host.CreateClient();
            using var request = CreateRequest(HttpMethod.Delete, "/api/animals/1", ApiFactory.TestKey);

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal([false], repository.DeleteTesterOnlyFlags);
        }

        var testerRepository = new FakeAnimalRepository();
        testerRepository.AddAnimal(1, AnimalStatus.Pending, "tester");
        using (var host = new TestHost(testerRepository))
        {
            var client = host.CreateClient();
            using var request = CreateRequest(HttpMethod.Delete, "/api/animals/1", ApiFactory.TesterKey);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal([true], testerRepository.DeleteTesterOnlyFlags);
        }

        foreach (var status in new[] { AnimalStatus.Approved, AnimalStatus.Rejected })
        {
            var repository = new FakeAnimalRepository();
            repository.AddAnimal(1, status, "tester");
            using var host = new TestHost(repository);
            var client = host.CreateClient();
            using var request = CreateRequest(HttpMethod.Delete, "/api/animals/1", ApiFactory.TesterKey);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        var unknownRepository = new FakeAnimalRepository();
        using (var host = new TestHost(unknownRepository))
        {
            var client = host.CreateClient();
            using var request = CreateRequest(HttpMethod.Delete, "/api/animals/99", ApiFactory.TesterKey);
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        using (var host = new TestHost(new FakeAnimalRepository()))
        {
            var client = host.CreateClient();
            using var response = await client.DeleteAsync("/api/animals/1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task Review_list_requires_admin_access()
    {
        var repository = new FakeAnimalRepository();
        repository.AddAnimal(1, AnimalStatus.Pending);
        using var host = new TestHost(repository);
        var client = host.CreateClient();

        using var adminRequest = CreateRequest(HttpMethod.Get, "/api/admin/review/animals", ApiFactory.TestKey);
        using var adminResponse = await client.SendAsync(adminRequest);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        using var testerRequest = CreateRequest(HttpMethod.Get, "/api/admin/review/animals", ApiFactory.TesterKey);
        using var testerResponse = await client.SendAsync(testerRequest);
        Assert.Equal(HttpStatusCode.Forbidden, testerResponse.StatusCode);

        using var noKeyResponse = await client.GetAsync("/api/admin/review/animals");
        Assert.Equal(HttpStatusCode.Unauthorized, noKeyResponse.StatusCode);
    }

    [Fact]
    public async Task Approve_changes_status_and_fills_once_only_for_pending_animals()
    {
        var repository = new FakeAnimalRepository();
        repository.AddAnimal(1, AnimalStatus.Pending);
        var translationStore = new StubTranslationStore();
        using var host = new TestHost(repository, translationStore);
        var client = host.CreateClient();

        using var approveRequest = CreateRequest(HttpMethod.Post, "/api/admin/review/animals/1/approve",
            ApiFactory.TestKey);
        using var approved = await client.SendAsync(approveRequest);

        Assert.Equal(HttpStatusCode.NoContent, approved.StatusCode);
        Assert.Equal(AnimalStatus.Approved, repository.Animals[1].Status);
        Assert.Equal(1, translationStore.EnglishReadCount);

        using var conflictRequest = CreateRequest(HttpMethod.Post, "/api/admin/review/animals/1/approve",
            ApiFactory.TestKey);
        using var conflict = await client.SendAsync(conflictRequest);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(1, translationStore.EnglishReadCount);

        using var missingRequest = CreateRequest(HttpMethod.Post, "/api/admin/review/animals/99/approve",
            ApiFactory.TestKey);
        using var missing = await client.SendAsync(missingRequest);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(1, translationStore.EnglishReadCount);
    }

    [Fact]
    public async Task Reject_accepts_an_empty_body_trims_notes_validates_length_and_never_fills()
    {
        var repository = new FakeAnimalRepository();
        repository.AddAnimal(1, AnimalStatus.Pending);
        repository.AddAnimal(2, AnimalStatus.Pending);
        repository.AddAnimal(3, AnimalStatus.Pending);
        repository.AddAnimal(4, AnimalStatus.Approved);
        var translationStore = new StubTranslationStore();
        using var host = new TestHost(repository, translationStore);
        var client = host.CreateClient();

        using var emptyBodyRequest = CreateRequest(HttpMethod.Post, "/api/admin/review/animals/1/reject",
            ApiFactory.TestKey);
        using var emptyBody = await client.SendAsync(emptyBodyRequest);
        Assert.Equal(HttpStatusCode.NoContent, emptyBody.StatusCode);
        Assert.Equal(AnimalStatus.Rejected, repository.Animals[1].Status);
        Assert.Null(repository.RejectionNotes[0]);

        using var noteRequest = CreateRequest(HttpMethod.Post, "/api/admin/review/animals/2/reject",
            ApiFactory.TestKey, JsonContent.Create(new RejectAnimalRequest { Note = "  needs review  " }));
        using var noteResponse = await client.SendAsync(noteRequest);
        Assert.Equal(HttpStatusCode.NoContent, noteResponse.StatusCode);
        Assert.Equal("needs review", repository.RejectionNotes[1]);

        using var longNoteRequest = CreateRequest(HttpMethod.Post, "/api/admin/review/animals/3/reject",
            ApiFactory.TestKey, JsonContent.Create(new RejectAnimalRequest { Note = new string('n', 501) }));
        using var longNoteResponse = await client.SendAsync(longNoteRequest);
        Assert.Equal(HttpStatusCode.BadRequest, longNoteResponse.StatusCode);
        Assert.Equal(2, repository.RejectionNotes.Count);

        using var conflictRequest = CreateRequest(HttpMethod.Post, "/api/admin/review/animals/4/reject",
            ApiFactory.TestKey);
        using var conflict = await client.SendAsync(conflictRequest);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(0, translationStore.EnglishReadCount);
    }

    [Fact]
    public async Task Review_post_routes_return_unauthorized_without_a_key()
    {
        using var host = new TestHost(new FakeAnimalRepository());
        var client = host.CreateClient();

        using var approve = await client.PostAsync("/api/admin/review/animals/1/approve", null);
        using var reject = await client.PostAsync("/api/admin/review/animals/1/reject", null);

        Assert.Equal(HttpStatusCode.Unauthorized, approve.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reject.StatusCode);
    }

    [Fact]
    public async Task Search_logging_returns_not_found_when_the_repository_rejects_a_pending_animal()
    {
        using var host = new TestHost(new FakeAnimalRepository(), searchRepository: new FakeSearchRepository(false));
        var client = host.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/searches", new CreateSearchRequest
        {
            QueryText = "pending animal",
            AnimalId = 12
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, string? key,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = content };
        if (key is not null)
        {
            request.Headers.Add(AdminKey.HeaderName, key);
        }

        return request;
    }

    private static void AssertCanonicalMeasurements(JsonElement animal, decimal lengthMin, decimal lengthMax,
        decimal heightMin, decimal heightMax, decimal weightMin, decimal weightMax, decimal lifespanMin,
        decimal lifespanMax)
    {
        Assert.Equal(lengthMin, animal.GetProperty("lengthMmMin").GetDecimal());
        Assert.Equal(lengthMax, animal.GetProperty("lengthMmMax").GetDecimal());
        Assert.Equal(heightMin, animal.GetProperty("heightMmMin").GetDecimal());
        Assert.Equal(heightMax, animal.GetProperty("heightMmMax").GetDecimal());
        Assert.Equal(weightMin, animal.GetProperty("weightGramsMin").GetDecimal());
        Assert.Equal(weightMax, animal.GetProperty("weightGramsMax").GetDecimal());
        Assert.Equal(lifespanMin, animal.GetProperty("lifespanMonthsMin").GetDecimal());
        Assert.Equal(lifespanMax, animal.GetProperty("lifespanMonthsMax").GetDecimal());
    }

    private sealed class TestHost : IDisposable
    {
        private readonly ApiFactory _parent;
        private readonly WebApplicationFactory<Program> _factory;

        public TestHost(FakeAnimalRepository repository, StubTranslationStore? translationStore = null,
            ISearchRepository? searchRepository = null)
        {
            _parent = new ApiFactory();
            _factory = _parent.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IAnimalRepository>(repository);
                services.AddSingleton<IAnimalTranslationStore>(translationStore ?? new StubTranslationStore());
                if (searchRepository is not null)
                {
                    services.AddSingleton(searchRepository);
                }
            }));
        }

        public HttpClient CreateClient() => _factory.CreateClient();

        public void Dispose()
        {
            _factory.Dispose();
            _parent.Dispose();
        }
    }

    private sealed class FakeAnimalRepository : IAnimalRepository
    {
        private int _nextId = 100;

        public Dictionary<int, Animal> Animals { get; } = [];
        public IReadOnlyList<Animal> AllResult { get; set; } = [];
        public List<Role?> GetAllViewers { get; } = [];
        public List<Role?> GetByIdViewers { get; } = [];
        public List<Role?> ImageViewers { get; } = [];
        public List<Role?> VisibilityViewers { get; } = [];
        public List<Role> CreateRoles { get; } = [];
        public List<Role> UpdateRoles { get; } = [];
        public List<int> UpdatedIds { get; } = [];
        public List<bool> DeleteTesterOnlyFlags { get; } = [];
        public List<string?> RejectionNotes { get; } = [];
        public List<string> PendingLanguages { get; } = [];

        public void AddAnimal(int id, string status, string submittedBy = "admin")
        {
            Animals[id] = new Animal
            {
                Id = id,
                CommonName = "Animal " + id,
                Status = status,
                SubmittedBy = submittedBy
            };
        }

        public Task<IReadOnlyList<Animal>> GetAllAsync(string language, Role? viewer)
        {
            GetAllViewers.Add(viewer);
            return Task.FromResult(AllResult);
        }

        public Task<Animal?> GetByIdAsync(int id, string language, Role? viewer)
        {
            GetByIdViewers.Add(viewer);
            return Task.FromResult(FindVisible(id, viewer));
        }

        public Task<IReadOnlyList<AnimalImage>> GetImagesByAnimalIdAsync(int animalId, Role? viewer)
        {
            ImageViewers.Add(viewer);
            IReadOnlyList<AnimalImage> images = FindVisible(animalId, viewer) is null
                ? Array.Empty<AnimalImage>()
                : [new AnimalImage { Id = 1, AnimalId = animalId, Url = "https://example.test/image.jpg" }];
            return Task.FromResult(images);
        }

        public Task<bool> IsVisibleAsync(int id, Role? viewer)
        {
            VisibilityViewers.Add(viewer);
            return Task.FromResult(FindVisible(id, viewer) is not null);
        }

        public Task<Animal> CreateAsync(CreateAnimalRequest request, Role submitter)
        {
            CreateRoles.Add(submitter);
            var id = _nextId++;
            var animal = new Animal
            {
                Id = id,
                CommonName = request.CommonName,
                LengthMmMin = request.LengthMmMin,
                LengthMmMax = request.LengthMmMax,
                HeightMmMin = request.HeightMmMin,
                HeightMmMax = request.HeightMmMax,
                WeightGramsMin = request.WeightGramsMin,
                WeightGramsMax = request.WeightGramsMax,
                LifespanMonthsMin = request.LifespanMonthsMin,
                LifespanMonthsMax = request.LifespanMonthsMax,
                Status = submitter == Role.Admin ? AnimalStatus.Approved : AnimalStatus.Pending,
                SubmittedBy = submitter == Role.Admin ? "admin" : "tester"
            };
            Animals.Add(id, animal);
            return Task.FromResult(animal);
        }

        public Task<AnimalWriteResult> UpdateAsync(int id, CreateAnimalRequest request, Role editor)
        {
            UpdateRoles.Add(editor);
            if (!Animals.TryGetValue(id, out var animal))
            {
                return Task.FromResult(new AnimalWriteResult(AnimalWriteStatus.NotFound, null));
            }

            if (editor == Role.Tester && animal.Status != AnimalStatus.Pending)
            {
                return Task.FromResult(new AnimalWriteResult(AnimalWriteStatus.Forbidden, null));
            }

            animal.CommonName = request.CommonName;
            animal.LengthMmMin = request.LengthMmMin;
            animal.LengthMmMax = request.LengthMmMax;
            animal.HeightMmMin = request.HeightMmMin;
            animal.HeightMmMax = request.HeightMmMax;
            animal.WeightGramsMin = request.WeightGramsMin;
            animal.WeightGramsMax = request.WeightGramsMax;
            animal.LifespanMonthsMin = request.LifespanMonthsMin;
            animal.LifespanMonthsMax = request.LifespanMonthsMax;
            UpdatedIds.Add(id);
            return Task.FromResult(new AnimalWriteResult(AnimalWriteStatus.Updated, animal));
        }

        public Task<AnimalTexts?> GetTranslationAsync(int id, string language) =>
            Task.FromResult(Animals.ContainsKey(id) ? new AnimalTexts { CommonName = "Translated" } : null);

        public Task<bool> SaveTranslationAsync(int id, string language, AnimalTexts texts) =>
            Task.FromResult(Animals.ContainsKey(id));

        public Task<AnimalDeleteResult> DeleteAsync(int id, bool pendingTesterOnly)
        {
            DeleteTesterOnlyFlags.Add(pendingTesterOnly);
            if (!Animals.TryGetValue(id, out var animal))
            {
                return Task.FromResult(new AnimalDeleteResult(AnimalDeleteStatus.NotFound));
            }

            if (pendingTesterOnly && (animal.Status != AnimalStatus.Pending || animal.SubmittedBy != "tester"))
            {
                return Task.FromResult(new AnimalDeleteResult(AnimalDeleteStatus.Forbidden));
            }

            Animals.Remove(id);
            return Task.FromResult(new AnimalDeleteResult(AnimalDeleteStatus.Deleted));
        }

        public Task<IReadOnlyList<Animal>> GetPendingAsync(string language)
        {
            PendingLanguages.Add(language);
            IReadOnlyList<Animal> animals = Animals.Values.Where(animal => animal.Status == AnimalStatus.Pending).ToList();
            return Task.FromResult(animals);
        }

        public Task<AnimalReviewResult> ApproveAsync(int id)
        {
            if (!Animals.TryGetValue(id, out var animal))
            {
                return Task.FromResult(new AnimalReviewResult(AnimalReviewStatus.NotFound));
            }

            if (animal.Status != AnimalStatus.Pending)
            {
                return Task.FromResult(new AnimalReviewResult(AnimalReviewStatus.NotPending));
            }

            animal.Status = AnimalStatus.Approved;
            animal.ReviewedAt = DateTime.UtcNow;
            animal.ReviewNote = null;
            return Task.FromResult(new AnimalReviewResult(AnimalReviewStatus.Done));
        }

        public Task<AnimalReviewResult> RejectAsync(int id, string? note)
        {
            RejectionNotes.Add(note);
            if (!Animals.TryGetValue(id, out var animal))
            {
                return Task.FromResult(new AnimalReviewResult(AnimalReviewStatus.NotFound));
            }

            if (animal.Status != AnimalStatus.Pending)
            {
                return Task.FromResult(new AnimalReviewResult(AnimalReviewStatus.NotPending));
            }

            animal.Status = AnimalStatus.Rejected;
            animal.ReviewedAt = DateTime.UtcNow;
            animal.ReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            return Task.FromResult(new AnimalReviewResult(AnimalReviewStatus.Done));
        }

        private Animal? FindVisible(int id, Role? viewer)
        {
            return Animals.TryGetValue(id, out var animal) && AnimalVisibility.StatusesFor(viewer).Contains(animal.Status)
                ? animal
                : null;
        }
    }

    private sealed class StubTranslationStore : IAnimalTranslationStore
    {
        public int EnglishReadCount { get; private set; }

        public Task<AnimalTexts?> GetTranslationAsync(int id, string language) => Task.FromResult<AnimalTexts?>(null);

        public Task<bool> SaveTranslationAsync(int id, string language, AnimalTexts texts) => Task.FromResult(true);

        public Task<EnglishAnimal?> GetEnglishAsync(int id)
        {
            EnglishReadCount++;
            return Task.FromResult<EnglishAnimal?>(null);
        }

        public Task<IReadOnlyList<EnglishAnimalRecord>> ListEnglishAsync() =>
            Task.FromResult<IReadOnlyList<EnglishAnimalRecord>>([]);
    }

    private sealed class FakeSearchRepository(bool result) : ISearchRepository
    {
        public Task<bool> AddAsync(CreateSearchRequest request) => Task.FromResult(result);

        public Task<IReadOnlyList<TopSearch>> GetTopAsync(int limit, int? days) =>
            Task.FromResult<IReadOnlyList<TopSearch>>([]);
    }
}