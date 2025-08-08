using Microsoft.AspNetCore.Mvc;
namespace WEBAPI_m1IL_1.DTO
{
    public class UploadModel
    {
        public required IFormFile ZipFile { get; set; }

        public bool IsPublic { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Tags { get; set; }
    }
}