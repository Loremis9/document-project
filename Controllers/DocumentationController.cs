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
using WEBAPI_m1IL_1.DTO;
using System.IO.Compression;
using NuGet.Protocol.Core.Types;
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
        public DocumentationController(RigthAccessService rigthAccessService, DocumentService documentService, UserService userService, DocumentFilesService documentationFileService)
        {
            this.rigthAccessService = rigthAccessService;
            this.documentService = documentService;
            this.userService = userService;
            this.documentationFileService = documentationFileService;
        }

        [HttpPost("Upload")]
        [Consumes("multipart/form-data")]
        [Authorize]
        public async Task<IActionResult> UploadZip([FromForm] UploadModel zipFile)
        {
            try
            {
                if (zipFile.ZipFile == null || zipFile.ZipFile.Length == 0)
                    return BadRequest("Le fichier ZIP est requis");
                // Validation des paramètres
                if (string.IsNullOrEmpty(zipFile.Title))
                    return BadRequest("Le titre est requis");
                if (string.IsNullOrEmpty(zipFile.Description))
                    return BadRequest("La description est requise");
                // Récupération de l'utilisateur connecté
                var user = await userService.GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized("Utilisateur non authentifié");
                // Import du document
                var result = await documentService.ImportDocument(user.Id, zipFile.Title, zipFile.Description, zipFile.IsPublic, zipFile.Tags, zipFile.ZipFile);

                return Ok(new { documentId = result.Id, message = "Document importé avec succès" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized($"Erreur d'accès : {ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                return BadRequest($"Répertoire introuvable : {ex.Message}");
            }
            catch (IOException ex)
            {
                return BadRequest($"Erreur d'entrée/sortie : {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"Données invalides : {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne : {ex.Message}");
            }
        }

        [HttpPost("UploadFile")]
        [Authorize]
        public async Task<IActionResult> UploadSingleFile(IFormFile file,int DocumentId,string path)
        {
            try
            {
                var user = await userService.GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized("Utilisateur non authentifié.");

               // CreateDocumentFile(int documentId, string path, bool isFolder, int userId, Stream fileStream, string ext)
                using var zipStream = file.OpenReadStream();
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        var ext = Path.GetExtension(entry.FullName).ToLowerInvariant();
                        using var entryStream = entry.Open();
                        var objectName = Path.Combine(path, entry.FullName)
                        .Replace("\\", "/");
                        await documentationFileService.CreateDocumentFile(DocumentId, path, false, user.Id, entryStream, ext);
                    }
                }

                return Ok(new { message = "Fichier uploadé avec succès"});
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { message = "Accès refusé." });
            }
            catch (DirectoryNotFoundException ex)
            {
                return StatusCode(500, new { message = "Erreur de répertoire.", detail = ex.Message });
            }
            catch (IOException ex)
            {
                return StatusCode(500, new { message = "Erreur d'entrée/sortie.", detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(400, new { message = "Données invalides.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Une erreur est survenue lors du traitement du fichier.", detail = ex.Message });
            }
        }

        [HttpGet("download-documentFile")]
        [Authorize]
        public async Task<string> DownloadMarkdown(int documentId, int documentFileId)
        {
            var user = await userService.GetCurrentUserAsync();
            var documentFile = await documentationFileService.FindDocumentFileByDocumentIdAndDocumentFileId(user.Id, documentId, documentFileId);
            return documentFile.FullPath;
        }

        [HttpGet("download-documentation")]
        [Authorize]
        public async Task<ActionResult<List<string>>> DownloadMarkdown(int documentId)
        {
            var user = await userService.GetCurrentUserAsync();
            var documentFiles = await documentationFileService.GetAllFilesByDocumentId(user.Id,documentId);
            List<string> filePaths = new List<string>();
            try
            {
                foreach (var item in documentFiles)
                {
                    filePaths.Add(item.FullPath);
                }
                return filePaths;
            }
            catch (Exception ex)
            {
                return BadRequest($"Erreur lors de la lecture du fichier : {ex.Message}");
            }
        }

        [HttpGet("SearchByPrompt")]
        [Authorize]
        public async Task<string> FindInDocumentationByPrompt(InputFindInPrompt inputFindInPrompt)
        {
            var user = await userService.GetCurrentUserAsync();
            return await documentService.SearchByPrompt(user.Id, inputFindInPrompt.Prompt, inputFindInPrompt.Model);
        }

        [HttpGet("SearchByTag")]
        [Authorize]
        public async Task<List<OutputDocumentFile>> FindDocumentFileByTags(string tag)
        {
            var user = await userService.GetCurrentUserAsync();
            return await documentationFileService.GetByTagAsync(tag, user.Id);
        }
    }
}
