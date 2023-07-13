using System.Text.Json.Serialization;

namespace ArmBasedVideoIndexer.Models
{
    public class Account
    {
        [JsonPropertyName("properties")]
        public AccountProperties Properties { get; set; }

        [JsonPropertyName("location")]
        public string Location { get; set; }
    }
}
