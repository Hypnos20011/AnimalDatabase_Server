using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Security;

namespace AnimalDatabase.Api.Tests;

public class AnimalVisibilityTests
{
    [Fact]
    public void Returns_statuses_for_visitors_testers_and_admins()
    {
        Assert.Equal([AnimalStatus.Approved], AnimalVisibility.StatusesFor(null));
        Assert.Equal([AnimalStatus.Approved, AnimalStatus.Pending], AnimalVisibility.StatusesFor(Role.Tester));
        Assert.Equal(
            [AnimalStatus.Approved, AnimalStatus.Pending, AnimalStatus.Rejected],
            AnimalVisibility.StatusesFor(Role.Admin));
    }
}