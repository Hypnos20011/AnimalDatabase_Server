using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Data;

public enum AnimalWriteStatus
{
    NotFound,
    Forbidden,
    Updated
}

public sealed record AnimalWriteResult(AnimalWriteStatus Status, Animal? Animal);

public enum AnimalDeleteStatus
{
    NotFound,
    Forbidden,
    Deleted
}

public sealed record AnimalDeleteResult(AnimalDeleteStatus Status);

public enum AnimalReviewStatus
{
    NotFound,
    NotPending,
    Done
}

public sealed record AnimalReviewResult(AnimalReviewStatus Status);