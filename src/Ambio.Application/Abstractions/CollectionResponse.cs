namespace Ambio.Application.Abstractions;

public record CollectionResponse<T>(
    IEnumerable<T> Items,
    int Total,
    int ItemsPerPage,
    int CurrentPage,
    int MaxPage);
