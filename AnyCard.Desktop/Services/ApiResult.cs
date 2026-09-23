
namespace AnyCard.Desktop.Services;

public record ApiResult<T>(T? Data, ApiError Error);
