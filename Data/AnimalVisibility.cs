using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Security;

namespace AnimalDatabase.Api.Data;

public static class AnimalVisibility
{
    public static IReadOnlyList<string> StatusesFor(Role? viewer) => viewer switch
    {
        Role.Admin => [AnimalStatus.Approved, AnimalStatus.Pending, AnimalStatus.Rejected],
        Role.Tester => [AnimalStatus.Approved, AnimalStatus.Pending],
        _ => [AnimalStatus.Approved]
    };
}