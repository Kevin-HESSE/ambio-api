namespace Ambio.Api.Abstractions;

public record PaginationResponse<T>(
    IEnumerable<T> Data,
    PaginationMeta Meta
    );
