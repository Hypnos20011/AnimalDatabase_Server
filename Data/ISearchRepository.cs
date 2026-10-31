using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Data;

public interface ISearchRepository
{
    Task<bool> AddAsync(CreateSearchRequest request);

    Task<IReadOnlyList<TopSearch>> GetTopAsync(int limit, int? days);
}