using System.Text.Json.Serialization;

namespace ArmBasedVideoIndexer.Models
{
    public class GenerateAccessTokenResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; }
    } 
}
