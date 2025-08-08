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

                // Création des répertoires nécessaires
                var pathGuid = SampleUtils.GenerateUUID();
                var docsPath = "C:/docs/" + pathGuid.ToString();
                // Extraction et traitement
                await FilesUtils.DezipFolderAndFilter(docsPath, zipFile.ZipFile);
                // Import du document
                var result = await documentService.ImportDocument(user.Id, docsPath, zipFile.Title, zipFile.Description, zipFile.IsPublic, zipFile.Tags);

                // Ajout des permissions
                await rigthAccessService.AddFirstUserToDocumentation(user.Id, result.Id);

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
        public async Task<IActionResult> UploadSingleFile(IFormFile file, bool isPublic, string title, string description, string tags)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Aucun fichier envoyé.");

            if (string.IsNullOrWhiteSpace(title))
                return BadRequest("Le titre est requis.");

            if (string.IsNullOrWhiteSpace(description))
                return BadRequest("La description est requise.");

            try
            {
                var user = await userService.GetCurrentUserAsync();
                if (user == null)
                    return Unauthorized("Utilisateur non authentifié.");

                // Créer les répertoires s'ils n'existent pas
                var docsPath = "C:/docs";
                if (!Directory.Exists(docsPath))
                    Directory.CreateDirectory(docsPath);

                var path = Path.Combine(docsPath, SampleUtils.GenerateUUID());
                Directory.CreateDirectory(path);

                // Sauvegarder le fichier
                var filePath = Path.Combine(path, file.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var result = await documentService.ImportDocument(user.Id, path, title, description, isPublic, tags);

                return Ok(new { message = "Fichier uploadé avec succès", path = filePath, documentId = result.Id });
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
        public async Task<IActionResult> DownloadMarkdown(int documentId, int documentFileId)
        {
            var user = await userService.GetCurrentUserAsync();
            var documentFile = await documentationFileService.FindDocumentFileByDocumentIdAndDocumentFileId(user.Id, documentId, documentFileId);
            var fileName = Path.GetFileName(documentFile.FullPath);
            var content = await System.IO.File.ReadAllTextAsync(documentFile.FullPath);
            return File(System.Text.Encoding.UTF8.GetBytes(content), "text/markdown", fileName);
        }

        [HttpGet("download-documentation")]
        [Authorize]
        public async Task<IActionResult> DownloadMarkdown(int documentId)
        {
            var user = await userService.GetCurrentUserAsync();
            var document = await documentService.FindDocumentById(user.Id, documentId);
            try
            {
                var content = await System.IO.File.ReadAllTextAsync(document.RootPath);
                return File(System.Text.Encoding.UTF8.GetBytes(content), "text/markdown", "document.md");
            }
            catch (Exception ex)
            {
                return BadRequest($"Erreur lors de la lecture du fichier : {ex.Message}");
            }
        }

        [HttpGet("SearchByPrompt")]
        [Authorize]
        public async Task<string> FindInDocumentationByPrompt(string prompt, string? model)
        {
            var user = await userService.GetCurrentUserAsync();
            return await documentService.SearchByPrompt(user.Id, prompt, model);
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
