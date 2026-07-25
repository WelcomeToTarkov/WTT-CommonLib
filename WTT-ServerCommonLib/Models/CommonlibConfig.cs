using System.Text.Json.Serialization;

namespace WTTServerCommonLib.Models
{
    internal class CommonlibConfig
    {
        [JsonPropertyName("itemValidationLoggingEnabled")]
        public bool ItemValidationLoggingEnabled { get; set; }

    }
}
