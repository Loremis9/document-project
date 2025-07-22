using Microsoft.AspNetCore.Mvc;

namespace WEBAPI_m1IL_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentationController : ControllerBase
    {
        [HttpPost("upload-zip")]
        public async Task<IActionResult> UploadZip([FromForm] IFormFile zipFile)
        {
            if (zipFile == null || zipFile.Length == 0)
                return BadRequest("Aucun fichier envoyé.");

            // Chemin où tu veux extraire le zip
            var extractPath = Path.Combine("C:/docs", Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            Directory.CreateDirectory(extractPath);

            // Sauvegarde temporaire du zip
            var tempZipPath = Path.GetTempFileName();
            using (var stream = System.IO.File.Create(tempZipPath))
            {
                await zipFile.CopyToAsync(stream);
            }

            // Extraction
            System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, extractPath);

            // (Optionnel) Indexe les fichiers extraits dans la base

            // Supprime le zip temporaire
            System.IO.File.Delete(tempZipPath);

            return Ok(new { message = "Fichiers extraits avec succès", path = extractPath });
        }

        [HttpGet("download-md")]
        public IActionResult DownloadMarkdown([FromQuery] string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                return NotFound();

            var fileName = Path.GetFileName(filePath);
            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Content-Type pour Markdown
            return File(fileBytes, "text/markdown", fileName);
        }

        [HttpGet("search/download-md")]
        public IActionResult FindAllPrivateMarkdownByGroup([FromQuery] string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                return NotFound();

            var fileName = Path.GetFileName(filePath);
            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Content-Type pour Markdown
            return File(fileBytes, "text/markdown", fileName);
        }

    }
}
