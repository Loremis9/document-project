using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using CsvHelper;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using WEBAPI_m1IL_1.Models;
using System.IO.Compression;
using System.Threading.Tasks;
using Humanizer.Bytes;
using Microsoft.AspNetCore.Http;
using DocumentFormat.OpenXml.Packaging;
using NPOI.XSSF.UserModel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Drawing;


namespace WEBAPI_m1IL_1.Utils
{
    public enum ElementType
    {
        Text,
        Image
    }

    public class DocumentElement
    {
        public ElementType Type { get; set; }
        public string Content { get; set; } // Pour le texte
        public string ImagePath { get; set; } // Pour l'image
    }

    public class DocumentContent
    {
        public List<DocumentElement> Elements { get; set; } = new();
    }

    public class FilesUtils
    {

        public static bool IsImage(string ext)
        {
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff" };
            return imageExtensions.Contains(ext);
        }

        public static string GetImageExtension(string contentType)
        {
            return contentType switch
            {
                "image/png" => ".png",
                "image/jpeg" => ".jpg",
                "image/gif" => ".gif",
                "image/bmp" => ".bmp",
                "image/tiff" => ".tiff",
                _ => ".img"
            };
        }

        public static string ReadFile(string path)
        {
            return System.IO.File.ReadAllText(path);
        }
        public static async Task<byte[]> ReadFileAsync(string path)
        {
            return await File.ReadAllBytesAsync(path);
        }
        public static void OverwriteFile(string filePath, string content)
        {
            File.WriteAllText(filePath, content);
        }

        public static void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Console.WriteLine("Fichier supprimé avec succès.");
            }
            else
            {
                Console.WriteLine("Le fichier n'existe pas.");
            }
        }


        public static List<String> DeleteFileUnauthorized(string path)
        {
            var deletedFiles = new List<String>();
            var forbiddenExtensions = new[] { ".exe", ".bat", ".cmd", ".js", ".ps1", ".vbs", ".com", ".scr", ".pif", ".jar", ".msi", ".dll", ".sys" };

            try
            {
                // Parcourt récursivement tous les fichiers dans le répertoire
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
                    if (forbiddenExtensions.Contains(ext))
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                            deletedFiles.Add(file);
                        }
                        catch (Exception ex)
                        {
                            // Log l'erreur mais continue avec les autres fichiers
                            Console.WriteLine($"Erreur lors de la suppression de {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du parcours du répertoire {path}: {ex.Message}");
            }
            return deletedFiles;
        }

        public static void CopyDirectory(string sourceDir, string destDir, bool overwrite = true)
        {
            foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
            }

            foreach (var filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(filePath, filePath.Replace(sourceDir, destDir), overwrite);
            }
        }
        public static async Task<byte[]> CreateZipFromDirectoryAsync(string sourceDir)
        {
            if (!Directory.Exists(sourceDir))
                throw new DirectoryNotFoundException($"Le répertoire {sourceDir} n'existe pas.");

            using var memoryStream = new MemoryStream();

            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
                {
                    string entryName = System.IO.Path.GetRelativePath(sourceDir, filePath);
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

                    using var entryStream = entry.Open();
                    using var fileStream = File.OpenRead(filePath);
                    await fileStream.CopyToAsync(entryStream);
                }
            }

            return memoryStream.ToArray();
        }

        public static async Task DezipFolderAndFilter(string destinationPath, IFormFile zipFile)
        {
            try
            {
                // 1. Créer le dossier de destination
                Directory.CreateDirectory(destinationPath);

                // 2. Sauvegarde temporaire du zip
                var tempZipPath = System.IO.Path.GetTempFileName();
                using (var stream = System.IO.File.Create(tempZipPath))
                {
                    await zipFile.CopyToAsync(stream);
                }

                // 3. Vérifier que le fichier zip est valide
                if (!System.IO.File.Exists(tempZipPath) || new FileInfo(tempZipPath).Length == 0)
                {
                    throw new InvalidOperationException("Le fichier zip est vide ou corrompu.");
                }

                // 4. Extraction du contenu dans le dossier destination
                System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, destinationPath);

                // 5. Suppression des fichiers non autorisés
                var deletedFiles = FilesUtils.DeleteFileUnauthorized(destinationPath);

                // 6. Supprime le zip temporaire
                System.IO.File.Delete(tempZipPath);
            }
            catch (InvalidDataException ex)
            {
                throw new InvalidOperationException("Le fichier zip est corrompu ou n'est pas un fichier zip valide.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException("Accès refusé lors du traitement du fichier.", ex);
            }
            catch (IOException ex)
            {
                throw new IOException("Erreur d'entrée/sortie lors du traitement du fichier.", ex);
            }
        }
    }
}
