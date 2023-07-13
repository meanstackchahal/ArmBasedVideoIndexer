using ArmBasedVideoIndexer.Helpers;
using ArmBasedVideoIndexer.Models;
using ArmBasedVideoIndexer.Services;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text.Json;
using static ArmBasedVideoIndexer.Helpers.GlobalConstants;

namespace ArmBasedVideoIndexer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        private readonly IConfiguration configuration;
        const string ApiVersion = "2022-08-01";
        const string AzureResourceManager = "https://management.azure.com";
        const string SubscriptionId = "Your creds";
        const string ResourceGroup = "Your creds";
        const string AccountName = "Your creds";
        const string VideoUrl = "<Your Video Url Here>";
        const string ApiUrl = "https://api.videoindexer.ai";
        const string ExcludedAI = "";
        public VideoController(IConfiguration configuration)
        {
            this.configuration = configuration; 
        }
        //configuration["ApplicationSettings:VersionType"].ToString();
         

        [HttpPost]
        [Route("UploadVideo")]
        public async Task<string> UploadVideo(string accountId, string accountLocation, string acountAccessToken, string apiUrl)
        {
            // Create the http client
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            var client = new HttpClient(handler);

            Console.WriteLine($"Video for account {accountId} is starting to upload.");
            var content = new MultipartFormDataContent();

            try
            {
                // Get the video from URL
                var queryParams = CommonFunctions.CreateQueryString(
                new Dictionary<string, string>()
                {
                    {"accessToken", acountAccessToken},
                    {"name", "video sample"},
                    {"description", "video_description"},
                    {"privacy", "private"},
                    {"partition", "partition"},
                    {"videoUrl", VideoUrl}, // Do not Include VideoURL when uploading Video using StreamContent
                });

                queryParams += CommonFunctions.AddExcludedAIs(ExcludedAI);

                // As an alternative to specifying video URL, you can upload a file.
                // Remove the videoUrl parameter from the query params below and add the following lines:
                //FileStream video = File.OpenRead(VideoFilePath);
                //var streamContent = new StreamContent(video);
                //streamContent.Headers.Add("Content-Type", "application/octet-stream");
                //streamContent.Headers.Add("Content-Length", video.Length.ToString());
                //content.Add(streamContent, "video", Path.GetFileName(VideoFilePath));

                var uploadRequestResult = await client.PostAsync($"{apiUrl}/{accountLocation}/Accounts/{accountId}/Videos?{queryParams}", content);
                CommonFunctions.VerifyStatus(uploadRequestResult, System.Net.HttpStatusCode.OK);
                var uploadResult = await uploadRequestResult.Content.ReadAsStringAsync();

                // Get the video ID from the upload result
                var videoId = JsonSerializer.Deserialize<Video>(uploadResult).Id;
                Console.WriteLine($"\nVideo ID {videoId} was uploaded successfully");
                return videoId;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }


        /// <summary>
        /// Calls getVideoIndex API in 10 second intervals until the indexing state is 'processed'(https://api-portal.videoindexer.ai/api-details#api=Operations&operation=Get-Video-Index)
        /// </summary>
        /// <param name="accountId"> The account ID</param>
        /// <param name="accountLocation"> The account location </param>
        /// <param name="acountAccessToken"> The access token </param>
        /// <param name="apiUrl"> The video indexer api url </param>
        /// <param name="client"> The http client </param>
        /// <param name="videoId"> The video id </param>
        /// <returns> Prints video index when the index is complete, otherwise throws exception </returns>
        [HttpPost]
        [Route("WaitForIndex")]
        public async Task WaitForIndex(string accountId, string accountLocation, string acountAccessToken, string apiUrl, string videoId)
        {
            // Create the http client
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            var client = new HttpClient(handler);

            Console.WriteLine($"\nWaiting for video {videoId} to finish indexing.");
            string queryParams;
            while (true)
            {
                queryParams = CommonFunctions.CreateQueryString(
                    new Dictionary<string, string>()
                    {
                            {"accessToken", acountAccessToken},
                            {"language", "English"},
                    });

                var videoGetIndexRequestResult = await client.GetAsync($"{apiUrl}/{accountLocation}/Accounts/{accountId}/Videos/{videoId}/Index?{queryParams}");

                CommonFunctions.VerifyStatus(videoGetIndexRequestResult, System.Net.HttpStatusCode.OK);
                var videoGetIndexResult = await videoGetIndexRequestResult.Content.ReadAsStringAsync();
                string processingState = JsonSerializer.Deserialize<Video>(videoGetIndexResult).State;

                // If job is finished
                if (processingState == ProcessingState.Processed.ToString())
                {
                    Console.WriteLine($"The video index has completed. Here is the full JSON of the index for video ID {videoId}: \n{videoGetIndexResult}");
                    return;
                }
                else if (processingState == ProcessingState.Failed.ToString())
                {
                    Console.WriteLine($"\nThe video index failed for video ID {videoId}.");
                    throw new Exception(videoGetIndexResult);
                }

                // Job hasn't finished
                Console.WriteLine($"\nThe video index state is {processingState}");
                await Task.Delay(10000);
            }
        }

        // Get video level access token for Azure Video Indexer 
        //var videoAccessToken = VideoIndexerResourceProviderClient.GetAccessToken(ArmAccessTokenPermission.Contributor, ArmAccessTokenScope.Video, videoId);

        /// <summary>
        /// Searches for the video in the account. Calls the searchVideo API (https://api-portal.videoindexer.ai/api-details#api=Operations&operation=Search-Videos)
        /// </summary>
        /// <param name="accountId"> The account ID</param>
        /// <param name="accountLocation"> The account location </param>
        /// <param name="videoAccessToken"> The access token </param>
        /// <param name="apiUrl"> The video indexer api url </param>
        /// <param name="client"> The http client </param>
        /// <param name="videoId"> The video id </param>
        /// <returns> Prints the video metadata, otherwise throws excpetion</returns>
        [HttpGet]
        [Route("GetVideo")]
        public async Task GetVideo(string accountId, string accountLocation, string videoAccessToken, string apiUrl, string videoId)
        {
            // Create the http client
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            var client = new HttpClient(handler);

            Console.WriteLine($"\nSearching videos in account {AccountName} for video ID {videoId}.");
            var queryParams = CommonFunctions.CreateQueryString(
                new Dictionary<string, string>()
                {
                        {"accessToken", videoAccessToken},
                        {"id", videoId},
                });

            try
            {
                var searchRequestResult = await client.GetAsync($"{apiUrl}/{accountLocation}/Accounts/{accountId}/Videos/Search?{queryParams}");

                CommonFunctions.VerifyStatus(searchRequestResult, System.Net.HttpStatusCode.OK);
                var searchResult = await searchRequestResult.Content.ReadAsStringAsync();
                Console.WriteLine($"Here are the search results: \n{searchResult}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Calls the getVideoInsightsWidget API (https://api-portal.videoindexer.ai/api-details#api=Operations&operation=Get-Video-Insights-Widget)
        /// </summary>
        /// <param name="accountId"> The account ID</param>
        /// <param name="accountLocation"> The account location </param>
        /// <param name="videoAccessToken"> The access token </param>
        /// <param name="apiUrl"> The video indexer api url </param>
        /// <param name="client"> The http client </param>
        /// <param name="videoId"> The video id </param>
        /// <returns> Prints the VideoInsightsWidget URL, otherwise throws exception</returns>
        [HttpGet]
        [Route("GetInsightsWidgetUrl")]
        public async Task GetInsightsWidgetUrl(string accountId, string accountLocation, string videoAccessToken, string apiUrl, string videoId)
        {
            // Create the http client
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            var client = new HttpClient(handler);

            Console.WriteLine($"\nGetting the insights widget URL for video {videoId}");
            var queryParams = CommonFunctions.CreateQueryString(
                new Dictionary<string, string>()
                {
                    {"accessToken", videoAccessToken},
                    {"widgetType", "Keywords"},
                    {"allowEdit", "true"},
                });
            try
            {
                var insightsWidgetRequestResult = await client.GetAsync($"{apiUrl}/{accountLocation}/Accounts/{accountId}/Videos/{videoId}/InsightsWidget?{queryParams}");

                CommonFunctions.VerifyStatus(insightsWidgetRequestResult, System.Net.HttpStatusCode.MovedPermanently);
                var insightsWidgetLink = insightsWidgetRequestResult.Headers.Location;
                Console.WriteLine($"Got the insights widget URL: \n{insightsWidgetLink}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Calls the getVideoPlayerWidget API (https://api-portal.videoindexer.ai/api-details#api=Operations&operation=Get-Video-Player-Widget)
        /// </summary>
        /// <param name="accountId"> The account ID</param>
        /// <param name="accountLocation"> The account location </param>
        /// <param name="videoAccessToken"> The access token </param>
        /// <param name="apiUrl"> The video indexer api url </param>
        /// <param name="client"> The http client </param>
        /// <param name="videoId"> The video id </param>
        /// <returns> Prints the VideoPlayerWidget URL, otherwise throws exception</returns>
        [HttpGet]
        [Route("GetPlayerWidgetUrl")]
        public async Task GetPlayerWidgetUrl(string accountId, string accountLocation, string videoAccessToken, string apiUrl, string videoId)
        {
            // Create the http client
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            var client = new HttpClient(handler);

            Console.WriteLine($"\nGetting the player widget URL for video {videoId}");
            var queryParams = CommonFunctions.CreateQueryString(
                new Dictionary<string, string>()
                {
                    {"accessToken", videoAccessToken},
                });

            try
            {
                var playerWidgetRequestResult = await client.GetAsync($"{apiUrl}/{accountLocation}/Accounts/{accountId}/Videos/{videoId}/PlayerWidget?{queryParams}");

                var playerWidgetLink = playerWidgetRequestResult.Headers.Location;
                CommonFunctions.VerifyStatus(playerWidgetRequestResult, System.Net.HttpStatusCode.MovedPermanently);
                Console.WriteLine($"Got the player widget URL: \n{playerWidgetLink}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
