using HR_Payroll.CommonCases.Utility;
using HR_Payroll.Core.Response;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace HR_Payroll.Web.CommonClients
{
    public class CommonAPI_Client
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private HttpClient _httpClient;
        private string _accessToken;
        private string _refreshToken;

        public CommonAPI_Client(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _httpClient = _httpClientFactory.CreateClient("AuthClient");
        }

        public void SetTokens(string accessToken, string refreshToken)
        {
            _accessToken = accessToken;
            _refreshToken = refreshToken;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
        #region----------------Common-------------------------------

        public async Task<DataResponse<T>> GetAsync<T>(string endpoint, Dictionary<string, string> queryParams = null)
        {
            var query = queryParams is { Count: > 0 }
                ? "?" + string.Join("&",
                    queryParams
                        .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                        .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"))
                : string.Empty;


            var fullUrl = $"{endpoint.TrimStart('/')}{query}";
            var response = await _httpClient.GetAsync(fullUrl);

            if (response.StatusCode == HttpStatusCode.Unauthorized && await TryRefreshTokenAsync())
            {
                _httpClient = _httpClientFactory.CreateClient("AuthClient");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                response = await _httpClient.GetAsync(fullUrl);
            }

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new DataResponse<T>
                {
                    status = false,
                    message = $"API call failed with status code {response.StatusCode}",
                    data = default
                };
            }

            return JsonConvert.DeserializeObject<DataResponse<T>>(json);
        }

        public async Task<DataResponse<T>> PostAsync<T>(string endpoint, object body)
        {
            HttpContent content;

            // Check if the object contains file(s)
            bool containsFile = body.GetType()
                .GetProperties()
                .Any(p => p.PropertyType == typeof(IFormFile) ||
                         p.PropertyType == typeof(byte[]) ||
                         (p.GetValue(body) != null && p.GetValue(body).GetType().Name.Contains("File")));

            if (containsFile)
            {
                var multipart = new MultipartFormDataContent();
                await MultipartFormDataContentExtensions.AddPropertiesAsync(multipart, body);
                content = multipart;
            }
            else
            {               
                var jsonBody = JsonConvert.SerializeObject(body);
                content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }
            
            var fullUrl = endpoint.TrimStart('/');

            var response = await _httpClient.PostAsync(fullUrl, content);

            // Handle unauthorized & auto-refresh token
            if (response.StatusCode == HttpStatusCode.Unauthorized && await TryRefreshTokenAsync())
            {
                _httpClient = _httpClientFactory.CreateClient("AuthClient");
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                response = await _httpClient.PostAsync(fullUrl, content);
            }

            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new DataResponse<T>
                {
                    status = false,
                    message = $"API call failed with status code {response.StatusCode}",
                    data = default
                };
            }

            return JsonConvert.DeserializeObject<DataResponse<T>>(json);
        }

        private async Task<bool> TryRefreshTokenAsync()
        {
            var refreshModel = new { RefreshToken = _refreshToken };
            var json = JsonConvert.SerializeObject(refreshModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var refreshClient = _httpClientFactory.CreateClient("AuthClient");
            var refreshResponse = await refreshClient.PostAsync("Auth/RefreshToken", content);

            if (!refreshResponse.IsSuccessStatusCode) return false;

            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(
                await refreshResponse.Content.ReadAsStringAsync());

            _accessToken = tokenResponse.AccessToken;
            _refreshToken = tokenResponse.RefreshToken;
            return true;
        }

        #endregion
    }

}
