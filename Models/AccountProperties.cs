using System.Text.Json.Serialization;

namespace ArmBasedVideoIndexer.Models
{
    public class AccountProperties
    {
        [JsonPropertyName("accountId")]
        public string Id { get; set; }
    }
}
