using Microsoft.AspNetCore.Mvc;
namespace WEBAPI_m1IL_1.Models
{
    public class UploadModel
    {
        public IFormFile File { get; set; }
        public string Description { get; set; }
    }
}