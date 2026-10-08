using AnyCard.Desktop.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace AnyCard.Desktop.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;
    private string? _refreshToken;
    public ApiClient()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7098/api/"),
            Timeout = TimeSpan.FromSeconds(30)
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
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return new ApiResult<AuthResponseDto>(null, ApiError.Conflict);
            }
            if(response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return new ApiResult<AuthResponseDto>(null, ApiError.InvalidInput);
            }
            return new ApiResult<AuthResponseDto>(null, ApiError.ServerError);

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
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiResult<AuthResponseDto>(null, ApiError.Unauthorized);
            }
            return new ApiResult<AuthResponseDto>(null, ApiError.ServerError);
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
        try
        {
            var response = await SendAsync(() => _httpClient.GetAsync(url));
            if (response.IsSuccessStatusCode)
            {
                var dueCardsRequest = await response.Content.ReadFromJsonAsync<List<CardDto>>();
                return new ApiResult<List<CardDto>>(dueCardsRequest, ApiError.None);
            }
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiResult<List<CardDto>>(null, ApiError.Unauthorized);
            }
            return new ApiResult<List<CardDto>>(null, ApiError.ServerError);
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return new ApiResult<List<CardDto>>(null, ApiError.NetworkUnavailable);
        }
        catch (System.Text.Json.JsonException)
        {
            return new ApiResult<List<CardDto>>(null, ApiError.ServerError);
        }
    }
    public async Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync()
    {
        try
        {
            var response = await SendAsync(() => _httpClient.GetAsync("categories"));
            if (response.IsSuccessStatusCode)
            {
                var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
                return new ApiResult<List<CategoryDto>>(categories, ApiError.None);
            }
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiResult<List<CategoryDto>>(null, ApiError.Unauthorized);
            }
            return new ApiResult<List<CategoryDto>>(null, ApiError.ServerError);
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return new ApiResult<List<CategoryDto>>(null, ApiError.NetworkUnavailable);
        }
        catch (System.Text.Json.JsonException)
        {
            return new ApiResult<List<CategoryDto>>(null, ApiError.ServerError);
        }
    }

    public async Task<ApiResult<bool>> ReviewCardAsync(int cardId, UserRating userRating)
    {
        try
        {
            var reviewCardDto = new ReviewCardDto(cardId, userRating);
            var reviewCardRequest = await SendAsync(() => _httpClient.PutAsJsonAsync("progress", reviewCardDto));
            if (reviewCardRequest.IsSuccessStatusCode == true)
            {
                return new ApiResult<bool>(true, ApiError.None);
            }
            if (reviewCardRequest.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiResult<bool>(false, ApiError.Unauthorized);
            }
            return new ApiResult<bool>(false, ApiError.ServerError);
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
            var apiResponse = await SendAsync(() => _httpClient.PostAsJsonAsync("cards", createCardDto));
            if (apiResponse.IsSuccessStatusCode)
            {
                return new ApiResult<bool>(true, ApiError.None);
            }
            if (apiResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiResult<bool>(false, ApiError.Unauthorized);
            }
            return new ApiResult<bool>(false, ApiError.ServerError);

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
            if (apiResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiResult<CategoryDto>(null, ApiError.Unauthorized);
            }
            if (apiResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return new ApiResult<CategoryDto>(null, ApiError.Conflict);
            }
            return new ApiResult<CategoryDto>(null, ApiError.ServerError);
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
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return new ApiResult<bool>(false, ApiError.InvalidInput);
            }
            return new ApiResult<bool>(false, ApiError.ServerError);
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
            var apiResponse = await _httpClient.PostAsJsonAsync("auth/reset-password", resetPasswordDto);
            if (apiResponse.IsSuccessStatusCode)
            {
                return new ApiResult<bool>(true, ApiError.None);
            }
            if (apiResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                return new ApiResult<bool>(false, ApiError.InvalidInput);
            }
            return new ApiResult<bool>(false, ApiError.ServerError);
        }
        catch (Exception ex) when (IsNetworkError(ex))
        {
            return new ApiResult<bool>(false, ApiError.NetworkUnavailable);
        }
    }
}
