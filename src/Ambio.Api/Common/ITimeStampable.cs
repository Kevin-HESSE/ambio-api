namespace Ambio.Api.Common;

public interface ITimeStampable
{
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
