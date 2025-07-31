using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using CsvHelper;
using WEBAPI_m1IL_1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WEBAPI_m1IL_1.Services;
using WEBAPI_m1IL_1.Utils;
namespace WEBAPI_m1IL_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DocumentationController : ControllerBase
    {
        private RigthAccessService rigthAccessService;
        private DocumentService documentService;
        private UserService userService;
        private DocumentFilesService documentationFileService;
        private LuceneSearchService luceneService;
        public DocumentationController(RigthAccessService rigthAccessService,DocumentService documentService, UserService userService,DocumentFilesService documentationFileService, LuceneSearchService luceneService) {
            this.rigthAccessService = rigthAccessService;
            this.documentService = documentService;
            this.userService = userService;
            this.documentationFileService = documentationFileService;
            this.luceneService = luceneService;
        }
        [HttpPost("Upload")]
        [Consumes("application/x-www-form-urlencoded")]
        [Authorize]

        public async Task<IActionResult> UploadZip(IFormFile zipFile, bool isPublic, string title,string description)
        {
            if (zipFile == null || zipFile.Length == 0)
                return BadRequest("Aucun fichier envoyé.");
            var user = userService.GetCurrentUser();
            var path ="C:/docs/" + SampleUtils.GenerateUUID();
            var zipPath ="C:/docs/zip/" + SampleUtils.GenerateUUID();
            var extractPath = Path.Combine(zipPath, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));

            await DezipFolderAndFilter(zipPath,path,zipFile,extractPath);
            documentService.ImportDocument(user.Id,path,title,description,isPublic);
            return Ok(new { message = "Fichiers extraits avec succès", path = extractPath });
        }

        [HttpGet("download-md")]
        [Authorize]
        public async Task<IActionResult> DownloadMarkdown(int documentFileId)
        {
            var user = userService.GetCurrentUser();
            var documentFile = await  documentationFileService.FindDocumentFileByDocumentIdAndDocumentFileId(user.Id,documentFileId);
            var fileName = Path.GetFileName(documentFile.FullPath);
            var fileBytes = System.IO.File.ReadAllBytes(documentFile.FullPath);

            // Content-Type pour Markdown
            return File(fileBytes, "text/markdown", fileName);
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<string> FindInDocumentationByPrompt(string prompt)
        {
            var user = userService.GetCurrentUser();
            return await documentService.SearchByPrompt(user.Id,prompt);
        }

        public async Task DezipFolderAndFilter(string zipPath,string path,IFormFile zipFile,string extractPath){

            Directory.CreateDirectory(path);
            // Sauvegarde temporaire du zip
            var tempZipPath = Path.GetTempFileName();
            using (var stream = System.IO.File.Create(tempZipPath))
            {
                await zipFile.CopyToAsync(stream);
            }
            // Extraction
            System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, extractPath);
            // Suppression des fichiers non autorisés
            var deletedFiles = FilesUtils.DeleteFileUnauthorized(extractPath);
            // Supprime le zip temporaire
            System.IO.File.Delete(tempZipPath);
        }
        // cherche un document par documentID
        //chercher un documentFile par son ID
        //chercher un dossier dans un docuementFile
    }
}
