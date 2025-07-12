 #nullable disable
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NodaTime;
using TidySyncDB.Sync;
using TidyUtility;
using TidyUtility.Extensions;
using TidyUtility.Serializer;

namespace TidySyncDB.Azure.Sync
{
    public class HttpDBClientSync : IDBClientSync
    {
        private readonly Uri _baseApiUri;
        private readonly string _syncApiKey;
        private readonly ISerializer _serializer;

        private static readonly HttpClient HttpClient = new HttpClient();


        public HttpDBClientSync(Uri baseApiUri, string syncApiKey, ISerializer serializer)
        {
            this._baseApiUri = baseApiUri;
            this._syncApiKey = syncApiKey;
            this._serializer = serializer;
        }

        public async Task<GetSyncServiceStatusResponse> GetSyncServiceStatus(GetSyncServiceStatusRequest request)
        {
            return await this.Post<GetSyncServiceStatusRequest, GetSyncServiceStatusResponse>(
                request, "GetSyncServiceStatus");
        }

        public async Task<GetLastDeviceSyncTimeResponse> GetLastDeviceSyncTimeAsync(GetLastDeviceSyncTimeRequest request)
        {
            return await this.Post<GetLastDeviceSyncTimeRequest, GetLastDeviceSyncTimeResponse>(
                request, "GetLastDeviceSyncTime");
        }

        public async Task<SynchronizeResponse> SynchronizeAsync(SynchronizeRequest request)
        {
            return await this.Post<SynchronizeRequest, SynchronizeResponse>(
                request, "Synchronize");
        }

        public async Task<ListRegisteredDevicesResponse> ListRegisteredDevicesAsync(ListRegisteredDevicesRequest request)
        {
            return await this.Post<ListRegisteredDevicesRequest, ListRegisteredDevicesResponse>(
                request, "ListRegisteredDevices");
        }

        public async Task<DeleteRegisteredDeviceResponse> DeleteRegisteredDeviceAsync(DeleteRegisteredDeviceRequest request)
        {
            return await this.Post<DeleteRegisteredDeviceRequest, DeleteRegisteredDeviceResponse>(
                request, "DeleteRegisteredDevice");
        }

        private async Task<TResponse> Post<TRequest, TResponse>(TRequest request, string uriMethodName)
        {
            string serializedRequest = this._serializer.Serialize(request);
            var httpClient = new HttpClient();
            Uri url = this._baseApiUri.Combine(uriMethodName);
            url = new UriBuilder(url.Scheme, url.Host, url.Port, url.AbsolutePath, $"?code={this._syncApiKey}").Uri;
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = new StringContent(serializedRequest);
            //if (!string.IsNullOrWhiteSpace(request.AccessBearerToken))
            //    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessBearerToken);

            HttpResponseMessage response = await httpClient.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
                throw new DBSyncHttpStatusErrorException(response);

            string responseBody = await response.Content.ReadAsStringAsync();
            return this._serializer.Deserialize<TResponse>(responseBody);
        }
    }
}