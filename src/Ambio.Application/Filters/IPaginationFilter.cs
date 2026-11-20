namespace Ambio.Application.Filters;

public interface IPaginationFilter
{
    /// <summary>
    /// Page number to return
    /// </summary>
    public int? Page { get; init; }
    /// <summary>
    /// Number of items per page returned
    /// </summary>
    public int? ItemsPerPage { get; init; }
}
