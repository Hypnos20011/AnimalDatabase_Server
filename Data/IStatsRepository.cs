using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Data;

public interface IStatsRepository
{
    Task<AnimalStats> GetStatsAsync();
}