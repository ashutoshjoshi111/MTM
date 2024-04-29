using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace ClientWrapper
{
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _client;        

        public ApiClient(string baseUrl)
        {                             
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromMinutes(25);
            _client.BaseAddress = new Uri(baseUrl);
        }

        public ApiClient()
        {
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromMinutes(25);
        }

        // Asynchronous version of GET method
        public async Task<string> GetAsyncFromURL(string resource)
        {
            HttpResponseMessage response = await _client.GetAsync(resource);
            return await HandleResponse(response);
        }

        public async Task<string> GetAsync(string resource, params (string key, string value)[] queryParams)
        {
            string queryString = BuildQueryString(queryParams);
            HttpResponseMessage response = await _client.GetAsync($"{resource}?{queryString}");
            return await HandleResponse(response);
        }

        public async Task<HttpResponseMessage> GetAsyncHttpResponse(string resource, params (string key, string value)[] queryParams)
        {
            string queryString = BuildQueryString(queryParams);
            HttpResponseMessage response = await _client.GetAsync($"{resource}?{queryString}");
            return response;
        }

        public async Task<HttpResponseMessage> PostAsync(string resource, params (string key, string value)[] queryParams)
        {
            string queryString = BuildQueryString(queryParams);
            HttpResponseMessage response = await _client.PostAsync($"{resource}?{queryString}", null);
            return response;
        }

        // Synchronous version of GET method
        public string GetFromURL(string resource)
        {
            HttpResponseMessage response = _client.GetAsync(resource).Result;
            return HandleResponse(response).Result;
        }

        public string Get(string resource, params (string key, string value)[] queryParams)
        {
            string queryString = BuildQueryString(queryParams);
            HttpResponseMessage response = _client.GetAsync($"{resource}?{queryString}").Result;
            return HandleResponse(response).Result;
        }

        private string BuildQueryString(params (string key, string value)[] queryParams)
        {
            var queryParamStrings = queryParams.Select(param => $"{param.key}={param.value}");
            return string.Join("&", queryParamStrings);
        }

        public async Task<string> PutAsync(string resource, string data)
        {
            HttpResponseMessage response = await _client.PutAsync(resource, new StringContent(data));
            return await HandleResponse(response);
        }

        public string Put(string resource, string data)
        {
            HttpResponseMessage response = _client.PutAsync(resource, new StringContent(data)).Result;
            return HandleResponse(response).Result;
        }

        public async Task<string> PostAsync(string resource, string data)
        {
            HttpResponseMessage response = await _client.PostAsync(resource, new StringContent(data));
            return await HandleResponse(response);
        }

        public string Post(string resource, string data)
        {
            HttpResponseMessage response = _client.PostAsync(resource, new StringContent(data)).Result;
            return HandleResponse(response).Result;
        }

        public async Task<string> DeleteAsync(string resource)
        {
            HttpResponseMessage response = await _client.DeleteAsync(resource);
            return await HandleResponse(response);
        }

        public string Delete(string resource)
        {
            HttpResponseMessage response = _client.DeleteAsync(resource).Result;
            return HandleResponse(response).Result;
        }

        private async Task<string> HandleResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return $"Request failed: {response.StatusCode}";
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
