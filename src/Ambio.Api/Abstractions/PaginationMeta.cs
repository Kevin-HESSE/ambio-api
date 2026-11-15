namespace Ambio.Api.Abstractions;

public sealed record PaginationMeta(
    int ItemsPerPage,
    int CurrentPage,
    int TotalItems,
    string First,
    string Last,
    string? Previous,
    string? Next
);
