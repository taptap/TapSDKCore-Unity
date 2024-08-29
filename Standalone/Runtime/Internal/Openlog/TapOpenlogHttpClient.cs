#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TapSDK.Core.Internal.Log;
using K4os.Compression.LZ4;

namespace TapSDK.Core.Standalone.Internal
{
    public class TapOpenlogHttpClient
    {

        private const string HOST_CN = "openlog.xdrnd.cn";
        private const string HOST_IO = "openlog.xdrnd.cn";

        private string GetHost()
        {
            if (TapCoreStandalone.coreOptions.region == TapTapRegionType.CN)
            {
                return HOST_CN;
            }
            else if (TapCoreStandalone.coreOptions.region == TapTapRegionType.Overseas)
            {
                return HOST_IO;
            }
            else
            {
                return HOST_CN;
            }
        }

        private static TapLog log = new TapLog(module: "Openlog.HttpClient");

        private static System.Net.Http.HttpClient client;

        public TapOpenlogHttpClient()
        {
            // var ip = "http://172.26.200.194:8888";
            // ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) =>
            // {
            //     return true;
            // };
            // HttpClientHandler clientHandler = new HttpClientHandler
            // {
            //     Proxy = new WebProxy(ip)
            // };
            // client = new System.Net.Http.HttpClient(clientHandler);
            client = new System.Net.Http.HttpClient();
        }

        public async Task<bool> Post(string path, byte[] content)
        {
            string url = BuildUrl(path);
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("User-Agent", "TapSDK-Unity/" + TapTapSDK.Version);
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("x-log-apiversion", "0.6.0");
            request.Headers.Add("x-log-compresstype", "lz4");
            request.Headers.Add("x-log-signaturemethod", "hmac-sha1");
            request.Headers.Add("x-log-timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
            request.Headers.Add("x-log-bodyrawsize", content.Length.ToString());

            byte[] compressContent = LZ4Compress(content);
            // byte[] compressContent = content;

            var contentMD5 = EncryptString(compressContent);
            request.Headers.Add("x-content-md5", contentMD5);

            string methodPart = request.Method.Method;
            string urlPath = request.RequestUri.LocalPath;
            string headersPart = GetHeadersPart(request.Headers);
            string signParts = methodPart + "\n" + contentMD5 + "\n" + "application/x-protobuf" + "\n" + headersPart + "\n" + urlPath;
            string sign;
            using (var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(TapCoreStandalone.coreOptions.clientToken)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signParts));
                sign = Convert.ToBase64String(hash);
            }

            request.Headers.Add("Authorization", "LOG " + TapCoreStandalone.coreOptions.clientId + ":" + sign);

            ByteArrayContent requestContent = new ByteArrayContent(compressContent);
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-protobuf");
            request.Content = requestContent;

            return await SendRequest(request);
        }

        private static string EncryptString(byte[] str)
        {
            var md5 = MD5.Create();
            byte[] byteOld = str;
            byte[] byteNew = md5.ComputeHash(byteOld);
            var sb = new StringBuilder();
            foreach (byte b in byteNew)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        private static byte[] LZ4Compress(byte[] data)
        {
            int maxCompressedLength = LZ4Codec.MaximumOutputSize(data.Length);
            byte[] compressedData = new byte[maxCompressedLength];
            int compressedLength = LZ4Codec.Encode(data, 0, data.Length, compressedData, 0, compressedData.Length);

            byte[] result = new byte[compressedLength];
            Array.Copy(compressedData, result, compressedLength);
            return result;
        }

        private static string GetHeadersPart(HttpRequestHeaders headers)
        {
            var headerKeys = headers
                .Where(h => h.Key.StartsWith("x-log-", StringComparison.OrdinalIgnoreCase))
                .OrderBy(h => h.Key.ToLowerInvariant())
                .Select(h => $"{h.Key.ToLowerInvariant()}:{string.Join(",", h.Value)}")
                .ToList();

            return string.Join("\n", headerKeys);
        }

        private async Task<bool> SendRequest(HttpRequestMessage request)
        {
            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                request.Dispose();
            }
            catch (HttpRequestException e)
            {
                log.Warning($"Request error", e.ToString());
                return false;
            }

            if (response.IsSuccessStatusCode || (response.StatusCode >= HttpStatusCode.BadRequest && response.StatusCode < HttpStatusCode.InternalServerError))
            {
                response.Dispose();
                return true;
            }
            else
            {
                log.Warning($"SendOpenlog failed", response.StatusCode.ToString());
                response.Dispose();
                return false;
            }
        }

        private string BuildUrl(string path)
        {
            string url = $"https://{GetHost()}/{path}?client_id={TapCoreStandalone.coreOptions.clientId}";
            return url;
        }
    }
}
#endif