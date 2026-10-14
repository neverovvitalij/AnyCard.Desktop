namespace AnyCard.Desktop.Models;

public record PagedResult<T>(List<T> Items, int TotalCount, int PageNumber, int PageSize)
{
    public int TotalPages => TotalCount % PageSize == 0 ? TotalCount / PageSize : (TotalCount / PageSize) +1;
}
