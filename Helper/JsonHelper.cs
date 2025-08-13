using System.Text.Json;
using System;
using System.Collections.Generic;

namespace WEBAPI_m1IL_1.Helpers
{
    public class JsonHelper
    {
        public static ImageMetadata ExtractMetadata(string json)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<ImageMetadata>(json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur de parsing JSON : " + ex.Message);
                return null;
            }
        }
    }
}