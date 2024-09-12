using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;

namespace TapSDK.Core.Standalone.Internal {
    // 网络请求类，使用示例
    /*
    HttpClientConfig config = new HttpClientConfig("https://api.example.com");
    config.DefaultHeaders.Add("Authorization", "Bearer your_token");

    HttpClient client = new HttpClient(config);

    // GET request
    Dictionary<string, string> queryParams = new Dictionary<string, string>
    {
        { "query1", "value1" },
        { "query2", "value2" }
    };
    string getResponse = await client.GetAsync("/path/to/resource", queryParams);
    UnityEngine.Debug.Log("GET Response: " + getResponse);

    // POST request
    string postBody = "{\"key\":\"value\"}";
    Dictionary<string, string> postHeaders = new Dictionary<string, string>
    {
        { "Content-Type", "application/json" }
    };
    string postResponse = await client.PostAsync("/path/to/resource", postBody, null, postHeaders);
    UnityEngine.Debug.Log("POST Response: " + postResponse);
    */
    public class HttpClientConfig {
        public string Host;
        public Dictionary<string, string> DefaultHeaders;
        public bool needSign = false;

        public HttpClientConfig(string host, Dictionary<string, string> defaultHeaders = null, bool needSign = false) {
            Host = host;
            if (defaultHeaders == null) {
                DefaultHeaders = new Dictionary<string, string>();
            } else {
                DefaultHeaders = defaultHeaders;
            }
            this.needSign = needSign;
        }
    }

    public class HttpClient {
        private readonly HttpClientConfig Config;
        private static readonly System.Net.Http.HttpClientHandler handler = new System.Net.Http.HttpClientHandler
        {
            Proxy = new WebProxy(GetProxyAddress(), true), // Charles 代理地址
            UseProxy = ShouldUseProxy()
        };
        private static readonly System.Net.Http.HttpClient client = new System.Net.Http.HttpClient(handler);

        private static bool ShouldUseProxy()
        {
            string useProxyEnv = Environment.GetEnvironmentVariable("TAPSDK_USE_PROXY");
            return !string.IsNullOrEmpty(useProxyEnv) && useProxyEnv.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetProxyAddress()
        {
            // 获取代理地址，默认值为 "http://localhost:8888"
            string proxyAddress = Environment.GetEnvironmentVariable("TAPSDK_PROXY_ADDRESS");
            return !string.IsNullOrEmpty(proxyAddress) ? proxyAddress : "http://localhost:8888";
        }

        public HttpClient(HttpClientConfig config)
        {
            this.Config = config;
            foreach (var header in this.Config.DefaultHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        public async Task<string> Get(string path, Dictionary<string, string> queryParameters = null, Dictionary<string, string> headers = null)
        {
            string url = BuildUrl(path, queryParameters);
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            AddHeaders(request, headers);

            return await SendRequest(request);
        }

        public async Task<string> Post(string path, Dictionary<string, object> body, Dictionary<string, string> queryParameters = null, Dictionary<string, string> headers = null)
        {
            string url = BuildUrl(path, queryParameters);
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            AddHeaders(request, headers);

            if (body != null)
            {
                string jsonBody = Json.Serialize(body);
                StringContent requestContent = new StringContent(jsonBody);
                requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = requestContent;
            }

            return await SendRequest(request);
        }

        private void AddHeaders(HttpRequestMessage request, Dictionary<string, string> headers)
        {
            // Add request-specific headers
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
        }

        public static async Task<string> Sign(HttpRequestMessage req, string secret)
        {
            string methodPart = req.Method.Method;
            string urlPathAndQueryPart = req.RequestUri.PathAndQuery;

            string headersPart = GetHeadersPart(req.Headers);

            string bodyPart = string.Empty;
            if (req.Content != null)
            {
                bodyPart = await req.Content.ReadAsStringAsync();
            }

            string signParts = methodPart + "\n" + urlPathAndQueryPart + "\n" + headersPart + "\n" + bodyPart + "\n";

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signParts));
                return Convert.ToBase64String(hash);
            }
        }

        private static string GetHeadersPart(HttpRequestHeaders headers)
        {
            var headerKeys = headers
                .Where(h => h.Key.StartsWith("x-tap-", StringComparison.OrdinalIgnoreCase))
                .OrderBy(h => h.Key.ToLowerInvariant())
                .Select(h => $"{h.Key.ToLowerInvariant()}:{string.Join(",", h.Value)}")
                .ToList();

            return string.Join("\n", headerKeys);
        }
        private async Task<string> SendRequest(HttpRequestMessage request)
        {
            if (this.Config.needSign)
            {
                string sign = await Sign(request, TapCoreStandalone.coreOptions.clientToken);
                request.Headers.Add("X-Tap-Sign", sign);
            }

            HttpResponseMessage response;

            try
            {
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                request.Dispose();
            }
            catch (HttpRequestException e)
            {
                Debug.Log($"Request error: {e}");
                return null;
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            var responseJson = Json.Deserialize(responseBody) as Dictionary<string, object>;
            if (responseJson != null)
            {
                int now = (int)Convert.ToInt64(responseJson["now"]);
                if (now > 0)
                {
                    TimeUtil.FixTime(now);
                }
            }

            Debug.Log($"Response Code: {response.StatusCode}, Body: {responseBody}");

            if (!response.IsSuccessStatusCode)
            {
                Debug.Log($"Error: {response}");
                return null;
            }

            response.Dispose();
            return responseBody;
        }

        private string BuildUrl(string path, Dictionary<string, string> queryParameters) {
            string url = $"{this.Config.Host}/{path}";
            if (queryParameters != null) {
                IEnumerable<string> queryPairs = queryParameters.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value.ToString())}");
                string queries = string.Join("&", queryPairs);
                url = $"{url}?{queries}";
            }
            return url;
        }
    }
}
