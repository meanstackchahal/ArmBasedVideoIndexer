using System.Text.Json.Serialization;
using static ArmBasedVideoIndexer.Helpers.GlobalConstants;

namespace ArmBasedVideoIndexer.Models
{
    public class AccessTokenRequest
    {
        [JsonPropertyName("permissionType")]
        public ArmAccessTokenPermission PermissionType { get; set; }

        [JsonPropertyName("scope")]
        public ArmAccessTokenScope Scope { get; set; }

        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; }

        [JsonPropertyName("videoId")]
        public string VideoId { get; set; }
    }
}
