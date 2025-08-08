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

        public static bool IsImage(string path)
        {
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff" };
            return imageExtensions.Contains(System.IO.Path.GetExtension(path).ToLowerInvariant());
        }
        public static string ExtractFromFile(string filePath, string imagesOutputDir)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            Directory.CreateDirectory(imagesOutputDir);

            return extension switch
            {
                ".pdf" => ExtractFromPdf(filePath, imagesOutputDir),
                ".docx" => ExtractFromDocx(filePath, imagesOutputDir),
                ".xlsx" => ExtractFromExcel(filePath),
                ".csv" => ExtractFromCsv(filePath),
                _ => ConvertToMarkdown(new DocumentContent
                {
                    Elements = { new DocumentElement { Type = ElementType.Text, Content = File.ReadAllText(filePath) } }
                })
            };
        }
        public static string ConvertToMarkdown(DocumentContent content)
        {
            var sb = new StringBuilder();
            foreach (var element in content.Elements)
            {
                if (element.Type == ElementType.Text)
                {
                    sb.AppendLine(element.Content);
                }
                else if (element.Type == ElementType.Image)
                {
                    sb.AppendLine($"![Image]({element.ImagePath})");
                }
            }
            return sb.ToString();
        }
        private static string ExtractFromPdf(string filePath, string imagesOutputDir)
        {
            var content = new DocumentContent();
            using var pdf = PdfDocument.Open(filePath);
            int imgCount = 0;

            foreach (var page in pdf.GetPages())
            {
                content.Elements.Add(new DocumentElement
                {
                    Type = ElementType.Text,
                    Content = page.Text
                });

                foreach (var img in page.GetImages())
                {
                    imgCount++;
                    string imgPath = System.IO.Path.Combine(imagesOutputDir, $"pdf_image_{imgCount}.png");
                    File.WriteAllBytes(imgPath, img.RawBytes.ToArray());
                    content.Elements.Add(new DocumentElement
                    {
                        Type = ElementType.Image,
                        ImagePath = imgPath
                    });
                }
            }

            return ConvertToMarkdown(content);
        }

        private static string GetImageExtension(string contentType)
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

        private static string ExtractFromDocx(string filePath, string imagesOutputDir)
        {
            var content = new DocumentContent();
            int imgCount = 0;

            Directory.CreateDirectory(imagesOutputDir);

            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart.Document.Body;

            foreach (var element in body.Elements())
            {
                if (element is DocumentFormat.OpenXml.Wordprocessing.Paragraph para)
                {
                    // Un paragraphe peut contenir plusieurs Runs (texte + images)
                    foreach (var run in para.Elements<DocumentFormat.OpenXml.Wordprocessing.Run>())
                    {
                        // 1. Texte dans le run (s'il y en a)
                        var text = run.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.Text>();
                        if (text != null)
                        {
                            content.Elements.Add(new DocumentElement
                            {
                                Type = ElementType.Text,
                                Content = text.Text
                            });
                        }

                        // 2. Chercher une image (Drawing) dans le run
                        var drawing = run.GetFirstChild<Drawing>();
                        if (drawing != null)
                        {
                            // Chercher l'image dans le Drawing
                            var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                            if (blip != null)
                            {
                                var embedId = blip.Embed?.Value;
                                if (embedId != null)
                                {
                                    var imagePart = (ImagePart)doc.MainDocumentPart.GetPartById(embedId);
                                    if (imagePart != null)
                                    {
                                        imgCount++;
                                        string ext = GetImageExtension(imagePart.ContentType); // ex: ".png"
                                        string imgPath = System.IO.Path.Combine(imagesOutputDir, $"docx_image_{imgCount}{ext}");

                                        using var stream = imagePart.GetStream();
                                        using var fileStream = new FileStream(imgPath, FileMode.Create, FileAccess.Write);
                                        stream.CopyTo(fileStream);

                                        content.Elements.Add(new DocumentElement
                                        {
                                            Type = ElementType.Image,
                                            ImagePath = imgPath
                                        });
                                    }
                                }
                            }
                        }
                    }
                    // Ajouter une nouvelle ligne après chaque paragraphe
                    content.Elements.Add(new DocumentElement
                    {
                        Type = ElementType.Text,
                        Content = "\n"
                    });
                }
                else if (element is DocumentFormat.OpenXml.Wordprocessing.Table)
                {
                    // Optionnel : gérer les tableaux si besoin
                }
            }

            return ConvertToMarkdown(content);
        }

        // Crée un fichier .md avec un nom donné et un contenu optionnel
        public static void CreateMarkdownFile(string directory, string fileName, string content = "")
        {
            if (!fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                fileName += ".md";
            string filePath = System.IO.Path.Combine(directory, fileName);
            File.WriteAllText(filePath, content);
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

        private static string ExtractFromExcel(string filePath)
        {
            var content = new DocumentContent();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var workbook = new XSSFWorkbook(fs);

            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                for (int r = 0; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row != null)
                    {
                        var rowText = string.Join(" | ", row.Cells.Select(c => c.ToString()));
                        content.Elements.Add(new DocumentElement
                        {
                            Type = ElementType.Text,
                            Content = rowText
                        });
                    }
                }
            }
            return ConvertToMarkdown(content);
        }

        private static string ExtractFromCsv(string filePath)
        {
            var content = new DocumentContent();
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<dynamic>();
            var sb = new StringBuilder();
            bool headerWritten = false;

            foreach (var record in records)
            {
                var dict = (IDictionary<string, object>)record;

                if (!headerWritten)
                {
                    sb.Append("| ");
                    sb.Append(string.Join(" | ", dict.Keys));
                    sb.AppendLine(" |");

                    sb.Append("| ");
                    sb.Append(string.Join(" | ", dict.Keys.Select(k => "---")));
                    sb.AppendLine(" |");

                    headerWritten = true;
                }

                sb.Append("| ");
                sb.Append(string.Join(" | ", dict.Values.Select(v => v?.ToString()?.Replace("|", "\\|") ?? "")));
                sb.AppendLine(" |");
            }

            content.Elements.Add(new DocumentElement
            {
                Type = ElementType.Text,
                Content = sb.ToString()
            });

            return ConvertToMarkdown(content);
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
