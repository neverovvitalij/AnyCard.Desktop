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
            BaseAddress = new Uri("https://localhost:7098/api/")
        };
    }

    public async Task<AuthResponseDto?> LoginAsync(string username, string password)
    {
        var loginRequest = await _httpClient.PostAsJsonAsync("auth/login", new { username, password });
        if (loginRequest.IsSuccessStatusCode)
        {
            var authResponseDto = await loginRequest.Content.ReadFromJsonAsync<AuthResponseDto>();

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponseDto?.AccessToken);

            _accessToken = authResponseDto?.AccessToken;
            _refreshToken = authResponseDto?.RefreshToken;
            return authResponseDto;
        }
        return null;
    }

    public async Task<ApiResult<List<CardDto>>> GetDueCardsAsync()
    {
        try
        {
        var dueCardsRequest = await _httpClient.GetFromJsonAsync<List<CardDto>?>("progress");
            return new ApiResult<List<CardDto>>(dueCardsRequest, ApiError.None);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiResult<List<CardDto>>(null, ApiError.Unauthorized);
            }
            if(ex.StatusCode == null)
            {
                return new ApiResult<List<CardDto>>(null, ApiError.NetworkUnavailable);
            }
                return new ApiResult<List<CardDto>>(null, ApiError.ServerError);
        }
    }

    public async Task<ApiResult<bool>> ReviewCardAsync(int cardId, UserRating userRating)
    {
        try
        {
            var reviewCardDto = new ReviewCardDto(cardId, userRating);
            var reviewCardRequest = await _httpClient.PutAsJsonAsync("progress", reviewCardDto);
            if(reviewCardRequest.IsSuccessStatusCode == true)
            {
                return new ApiResult<bool>(true, ApiError.None);
            }
            if (reviewCardRequest.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new ApiResult<bool>(false, ApiError.Unauthorized);
            }
                return new ApiResult<bool>(false, ApiError.ServerError);
        }
        catch(HttpRequestException)
        {
            return new ApiResult<bool>(false, ApiError.NetworkUnavailable);
        }
    }
}
