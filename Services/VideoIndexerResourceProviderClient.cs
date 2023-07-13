using ArmBasedVideoIndexer.Models;
using static ArmBasedVideoIndexer.Helpers.GlobalConstants;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using ArmBasedVideoIndexer.Helpers;

namespace ArmBasedVideoIndexer.Services
{
    public class VideoIndexerResourceProviderClient
    {
        private readonly string armAccessToken;
        const string ApiVersion = "2022-08-01";
        const string AzureResourceManager = "https://management.azure.com";
        const string SubscriptionId = "Your creds";
        const string ResourceGroup = "Your creds";
        const string AccountName = "Your creds";
        async public static Task<VideoIndexerResourceProviderClient> BuildVideoIndexerResourceProviderClient()
        {
            var tokenRequestContext = new TokenRequestContext(new[] { $"{AzureResourceManager}/.default" });
            var tokenRequestResult = await new DefaultAzureCredential().GetTokenAsync(tokenRequestContext);
            return new VideoIndexerResourceProviderClient(tokenRequestResult.Token);
        }
        public VideoIndexerResourceProviderClient(string armAaccessToken)
        {
            this.armAccessToken = armAaccessToken;
        }

        /// <summary>
        /// Generates an access token. Calls the generateAccessToken API  (https://github.com/Azure/azure-rest-api-specs/blob/main/specification/vi/resource-manager/Microsoft.VideoIndexer/stable/2022-08-01/vi.json#:~:text=%22/subscriptions/%7BsubscriptionId%7D/resourceGroups/%7BresourceGroupName%7D/providers/Microsoft.VideoIndexer/accounts/%7BaccountName%7D/generateAccessToken%22%3A%20%7B)
        /// </summary>
        /// <param name="permission"> The permission for the access token</param>
        /// <param name="scope"> The scope of the access token </param>
        /// <param name="videoId"> if the scope is video, this is the video Id </param>
        /// <param name="projectId"> If the scope is project, this is the project Id </param>
        /// <returns> The access token, otherwise throws an exception</returns>
        public async Task<string> GetAccessToken(ArmAccessTokenPermission permission, ArmAccessTokenScope scope, string videoId = null, string projectId = null)
        {
            var accessTokenRequest = new AccessTokenRequest
            {
                PermissionType = permission,
                Scope = scope,
                VideoId = videoId,
                ProjectId = projectId
            };

            Console.WriteLine($"\nGetting access token: {JsonSerializer.Serialize(accessTokenRequest)}");

            // Set the generateAccessToken (from video indexer) http request content
            try
            {
                var jsonRequestBody = JsonSerializer.Serialize(accessTokenRequest);
                var httpContent = new StringContent(jsonRequestBody, System.Text.Encoding.UTF8, "application/json");

                // Set request uri
                var requestUri = $"{AzureResourceManager}/subscriptions/{SubscriptionId}/resourcegroups/{ResourceGroup}/providers/Microsoft.VideoIndexer/accounts/{AccountName}/generateAccessToken?api-version={ApiVersion}";
                var client = new HttpClient(new HttpClientHandler());
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", armAccessToken);

                var result = await client.PostAsync(requestUri, httpContent);

                CommonFunctions.VerifyStatus(result, System.Net.HttpStatusCode.OK);
                var jsonResponseBody = await result.Content.ReadAsStringAsync();
                Console.WriteLine($"Got access token: {scope} {videoId}, {permission}");
                return JsonSerializer.Deserialize<GenerateAccessTokenResponse>(jsonResponseBody).AccessToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        /// <summary>
        /// Gets an account. Calls the getAccount API (https://github.com/Azure/azure-rest-api-specs/blob/main/specification/vi/resource-manager/Microsoft.VideoIndexer/stable/2022-08-01/vi.json#:~:text=%22/subscriptions/%7BsubscriptionId%7D/resourceGroups/%7BresourceGroupName%7D/providers/Microsoft.VideoIndexer/accounts/%7BaccountName%7D%22%3A%20%7B)
        /// </summary>
        /// <returns> The Account, otherwise throws an exception</returns>
        public async Task<Account> GetAccount()
        {
            Console.WriteLine($"Getting account {AccountName}.");
            Account account;
            try
            {
                // Set request uri
                var requestUri = $"{AzureResourceManager}/subscriptions/{SubscriptionId}/resourcegroups/{ResourceGroup}/providers/Microsoft.VideoIndexer/accounts/{AccountName}?api-version={ApiVersion}";
                var client = new HttpClient(new HttpClientHandler());
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", armAccessToken);

                var result = await client.GetAsync(requestUri);

                CommonFunctions.VerifyStatus(result, System.Net.HttpStatusCode.OK);
                var jsonResponseBody = await result.Content.ReadAsStringAsync();
                account = JsonSerializer.Deserialize<Account>(jsonResponseBody);
                VerifyValidAccount(account);
                Console.WriteLine($"The account ID is {account.Properties.Id}");
                Console.WriteLine($"The account location is {account.Location}");
                return account;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        private static void VerifyValidAccount(Account account)
        {
            if (string.IsNullOrWhiteSpace(account.Location) || account.Properties == null || string.IsNullOrWhiteSpace(account.Properties.Id))
            {
                Console.WriteLine($"{nameof(AccountName)} {AccountName} not found. Check {nameof(SubscriptionId)}, {nameof(ResourceGroup)}, {nameof(AccountName)} ar valid.");
                throw new Exception($"Account {AccountName} not found.");
            }
        }
    }
}
