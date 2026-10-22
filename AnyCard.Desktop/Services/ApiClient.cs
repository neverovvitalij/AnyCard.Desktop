using AnyCard.Desktop.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;

namespace AnyCard.Desktop.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;
    private string? _refreshToken;

#if DEBUG
    private const string BaseUrl = "https://localhost:7098/api/";
#else
    private const string BaseUrl = "https://api.any-card.de/api/";
#endif
    public ApiClient()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private async Task<ApiResult<T>> GetAsync<T>(string url)
    {

        try
        {
            var response = await SendAsync(() => _httpClient.GetAsync(url));

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>();
                return new ApiResult<T>(data, ApiError.None);
            }
            return new ApiResult<T>(default, MapError(response.StatusCode));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return new ApiResult<T>(default, ApiError.NetworkUnavailable);
        }
        catch (System.Text.Json.JsonException)
        {
            return new ApiResult<T>(default, ApiError.ServerError);
        }
    }

    private static ApiError MapError(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => ApiError.Unauthorized,
            HttpStatusCode.TooManyRequests => ApiError.TooManyRequests,
            HttpStatusCode.Conflict => ApiError.Conflict,
            HttpStatusCode.BadRequest => ApiError.InvalidInput,
            _ => ApiError.ServerError
        };
    }

    private static bool IsNetworkError(Exception ex) => ex is HttpRequestException or TaskCanceledException;
    public async Task<ApiResult<AuthResponseDto>> RegisterAsync(string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/register", new RegisterDto(email, password));
            if (response.IsSuccessStatusCode)
            {
                var authResponseDto = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (authResponseDto == null)
                {
                    return new ApiResult<AuthResponseDto>(null, ApiError.ServerError);
                }
                _accessToken = authResponseDto.AccessToken;
                _refreshToken = authResponseDto.RefreshToken;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponseDto.AccessToken);
                return new ApiResult<AuthResponseDto>(authResponseDto, ApiError.None);

            }
            return new ApiResult<AuthResponseDto>(null, MapError(response.StatusCode));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return new ApiResult<AuthResponseDto>(null, ApiError.NetworkUnavailable);
        }
        catch (System.Text.Json.JsonException)
        {
            return new ApiResult<AuthResponseDto>(null, ApiError.ServerError);
        }
    }
    public async Task<ApiResult<AuthResponseDto>> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/login", new LoginDto(email, password));
            if (response.IsSuccessStatusCode)
            {
                var authResponseDto = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (authResponseDto == null)
                {
                    return new ApiResult<AuthResponseDto>(null, ApiError.ServerError);
                }

                _accessToken = authResponseDto.AccessToken;
                _refreshToken = authResponseDto.RefreshToken;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponseDto.AccessToken);
                return new ApiResult<AuthResponseDto>(authResponseDto, ApiError.None);

            }
            return new ApiResult<AuthResponseDto>(null, MapError(response.StatusCode));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return new ApiResult<AuthResponseDto>(null, ApiError.NetworkUnavailable);
        }
        catch (System.Text.Json.JsonException)
        {
            return new ApiResult<AuthResponseDto>(null, ApiError.ServerError);
        }

    }

    private async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken))
        {
            return false;
        }

        var response = await _httpClient.PostAsJsonAsync("auth/refresh", new RefreshDto(_refreshToken));
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        var authResponseDto = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (authResponseDto == null)
        {
            return false;
        }

        _accessToken = authResponseDto.AccessToken;
        _refreshToken = authResponseDto.RefreshToken;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponseDto.AccessToken);
        return true;
    }

    public async Task<ApiResult<List<CardDto>>> GetDueCardsAsync(int? categoryId)
    {
        var url = categoryId.HasValue ? $"progress?categoryId={categoryId}" : "progress";
        return await GetAsync<List<CardDto>>(url);
    }
    public Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync() => GetAsync<List<CategoryDto>>("categories");

    public Task<ApiResult<List<CardDto>>> GetAllCardsAsync() => GetAsync<List<CardDto>>("cards");

    public async Task<ApiResult<bool>> ReviewCardAsync(int cardId, UserRating userRating)
    {
        try
        {
            var reviewCardDto = new ReviewCardDto(cardId, userRating);
            var response = await SendAsync(() => _httpClient.PutAsJsonAsync("progress", reviewCardDto));
            if (response.IsSuccessStatusCode)
            {
                return new ApiResult<bool>(true, ApiError.None);
            }
            return new ApiResult<bool>(false, MapError(response.StatusCode));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return new ApiResult<bool>(false, ApiError.NetworkUnavailable);
        }
        catch (System.Text.Json.JsonException)
        {
            return new ApiResult<bool>(false, ApiError.ServerError);
        }
    }

    public async Task<ApiResult<bool>> CreateCardAsync(string question, string answer, int categoryId)
    {
        try
        {
            var createCardDto = new CreateCardDto(question, answer, categoryId);
            var response = await SendAsync(() => _httpClient.PostAsJsonAsync("cards", createCardDto));
            if (response.IsSuccessStatusCode)
            {
                return new ApiResult<bool>(true, ApiError.None);
            }
            return new ApiResult<bool>(false, MapError(response.StatusCode));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return new ApiResult<bool>(false, ApiError.NetworkUnavailable);
        }
        catch (System.Text.Json.JsonException)
        {
            return new ApiResult<bool>(false, ApiError.ServerError);
        }
    }

    public async Task<ApiResult<CategoryDto>> CreateCategoryAsync(string categoryName)
    {
        try
        {
            var createCategoryDto = new CreateCategoryDto(categoryName);
            var apiResponse = await SendAsync(() => _httpClient.PostAsJsonAsync("categories", createCategoryDto));
            if (apiResponse.IsSuccessStatusCode)
            {
                var categoryDto = await apiResponse.Content.ReadFromJsonAsync<CategoryDto>();
                return new ApiResult<CategoryDto>(categoryDto, ApiError.None);
            }
            return new ApiResult<CategoryDto>(null, MapError(apiResponse.StatusCode));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return new ApiResult<CategoryDto>(null, ApiError.NetworkUnavailable);

        }
        catch (System.Text.Json.JsonException)
        {
            return new ApiResult<CategoryDto>(null, ApiError.ServerError);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> request)
    {
        var response = await request();
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
        {
            return response;
        }
        else
        {
            var refreshResult = await RefreshTokenAsync();
            if (!refreshResult)
            {
                return response;
            }
            response.Dispose();
            return await request();
        }
    }

    public async Task LogoutAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken))
        {
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _httpClient.PostAsJsonAsync("auth/logout", new RefreshDto(_refreshToken), cts.Token);
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
        }

        finally
        {
            _accessToken = null;
            _refreshToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public async Task<ApiResult<bool>> ForgotPasswordAsync(string email)
    {
        var forgotPasswordDto = new ForgotPasswordDto(email);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/forgot-password", forgotPasswordDto);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResult<bool>(true, ApiError.None);
            }
            return new ApiResult<bool>(false, MapError(response.StatusCode));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return new ApiResult<bool>(false, ApiError.NetworkUnavailable);

        }
    }

    public async Task<ApiResult<bool>> ResetPasswordAsync(string email, string code, string newPassword)
    {
        try
        {
            var resetPasswordDto = new ResetPasswordDto(email, code, newPassword);
            var response = await _httpClient.PostAsJsonAsync("auth/reset-password", resetPasswordDto);
            if (response.IsSuccessStatusCode)
            {
                return new ApiResult<bool>(true, ApiError.None);
            }
            return new ApiResult<bool>(false, MapError(response.StatusCode));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return new ApiResult<bool>(false, ApiError.NetworkUnavailable);
        }
    }

    public Task<ApiResult<ProgressSummaryDto>> GetProgressSummaryAsync() => GetAsync<ProgressSummaryDto>("progress/summary");

    public async Task<ApiResult<PagedResult<CardOverviewDto>>> GetCardsOverviewAsync(int? categoryId, UserRating? userRating, int pageNumber)
    {
        var url = $"progress/overview?pageNumber={pageNumber}";
        if (categoryId.HasValue)
        {
            url += $"&categoryId={categoryId}";
        }
        if (userRating.HasValue)
        {
            url += $"&userRating={userRating}";
        }
        return await GetAsync<PagedResult<CardOverviewDto>>(url);
    }

    public Task<ApiResult<UserSettingsDto>> GetUserSettingsAsync() => GetAsync<UserSettingsDto>("settings");

    public async Task<ApiResult<UserSettingsDto>> UpdateUserSettingsAsync(TimeSpan again, TimeSpan hard, TimeSpan good, TimeSpan easy)
    {
        var userSettingsDto = new UserSettingsDto(again, hard, good, easy);

        try
        {
            var response = await SendAsync(() => _httpClient.PutAsJsonAsync("settings", userSettingsDto));

            if (response.IsSuccessStatusCode)
            {
                return new ApiResult<UserSettingsDto>(userSettingsDto, ApiError.None);
            }
            return new ApiResult<UserSettingsDto>(null, MapError(response.StatusCode));
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return new ApiResult<UserSettingsDto>(null, ApiError.NetworkUnavailable);
        }
    }

    public Task<ApiResult<UserSettingsDto>> GetDefaultSettingsAsync() => GetAsync<UserSettingsDto>("settings/defaults");

    public Task<ApiResult<CardDto>> GetCardByIdAsync(int cardId) => GetAsync<CardDto>($"cards/{cardId}");
}