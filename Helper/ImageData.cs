using System.Text.Json;
using System.Text.Json.Serialization;
namespace WEBAPI_m1IL_1.Helpers
{

    public class ImageMetadata
    {
        public string Description { get; set; }
        public string Tags { get; set; }
        [JsonIgnore]
        public string Url { get; set; }
    }
}