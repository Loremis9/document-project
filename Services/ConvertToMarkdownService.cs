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
using WEBAPI_m1IL_1.Utils;
using System.IO.Compression;

// il faudrait prendre en compte les vidéos 
namespace WEBAPI_m1IL_1.Services
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
        public string Url { get; set; } // Pour l'image
    }

    public class DocumentContent
    {
        public List<DocumentElement> Elements { get; set; } = new();
    }

    public class ConvertToMarkdownService
    {
        MinIoService _minIoService;
        public ConvertToMarkdownService(MinIoService minIoService)
        {
            _minIoService = minIoService;
        }

        public async Task<string> ExtractFromFile(Stream file, int documentId,string ext,string imagePath)
        {
            using var reader = new StreamReader(file);
            string content = reader.ReadToEnd();
            return ext switch
                {
                    ".pdf" => await ConvertToMarkdown(await ExtractFromPdf(content), documentId),
                    ".docx" => await ConvertToMarkdown(await ExtractFromDocx(content), documentId),
                    ".xlsx" => await ConvertToMarkdown(ExtractFromExcel(content)),
                    ".csv" => await ConvertToMarkdown(ExtractFromCsv(content)),
                    ".one" => await ConvertToMarkdown(await OneToPdf(content,imagePath)),
                    _ => await ConvertToMarkdown(new DocumentContent
                    {
                        Elements = { new DocumentElement { Type = ElementType.Text, Content = File.ReadAllText(content) } }
                    })
                };
        }

        public async Task<string> ConvertToMarkdown(DocumentContent content, int? documentId=null, string? imagePath = null)
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
                    if (documentId.HasValue)
                    {
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            sb.AppendLine($"![Image]({element.Url}) \n descriptiond de l'image : {element.Content}");
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private async Task<DocumentContent> OneToPdf(string file,string path)
        {
            // Charger le .one depuis le fichier
            var doc = new Aspose.Note.Document(file);

            // Créer un MemoryStream pour recevoir le PDF
            using var pdfStream = new MemoryStream();

            // Sauvegarder en PDF dans le MemoryStream
            doc.Save(pdfStream, Aspose.Note.SaveFormat.Pdf);

            // Repositionner le curseur au début du stream
            pdfStream.Position = 0;

            // Appeler ta méthode ExtractFromPdf adaptée pour accepter un Stream
            return await ExtractFromPdf(pdfStream,path);
        }
        private async Task<DocumentContent> ExtractFromPdf(Stream pdfStream, string fileNameWithoutExt)
        {
            var content = new DocumentContent();
            using var pdf = PdfDocument.Open(pdfStream);
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
                    string objectName = $"{fileNameWithoutExt}/image_{imgCount}.png";
                    byte[] imageBytes = img.RawBytes.ToArray();

                    var image = await _minIoService.UploadImageAskAiAsync(objectName, "png", imageBytes);
                    content.Elements.Add(new DocumentElement
                    {
                        Type = ElementType.Image,
                        Content=image.Description,
                        Url = image.Url,
                    });
                }
            }
            return content;
        }

        private async Task<DocumentContent> ExtractFromPdf(string file)
        {
            var content = new DocumentContent();
            using var pdf = PdfDocument.Open(file);
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
                    //var image = await _minIoService.UploadImageAskAiAsync(element.ImagePath, imagePath);
                    string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(file);
                    string objectName = $"{fileNameWithoutExt}/image_{imgCount}.png";
                    byte[] imageBytes = img.RawBytes.ToArray();

                    var image = await _minIoService.UploadImageAskAiAsync(objectName, "png", imageBytes);
                    content.Elements.Add(new DocumentElement
                    {
                        Type = ElementType.Image,
                        Content = image.Description,
                        Url = image.Url,
                    });
                }
            }
            return content;
        }
        public async Task<DocumentContent> ExtractFromDocx(string file)
        {
            var content = new DocumentContent();
            int imgCount = 0;

            using var doc = WordprocessingDocument.Open(file, false);
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
                                        string extension = imagePart.ContentType switch
                                        {
                                            "image/png" => "png",
                                            "image/jpeg" => "jpg",
                                            "image/gif" => "gif",
                                            _ => "bin"
                                        };

                                        // Nom unique pour MinIO
                                        string fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(file);
                                        string objectName = $"{fileNameWithoutExt}/image_{imgCount}.{extension}";

                                        using var imgStream = imagePart.GetStream();
                                        using var ms = new MemoryStream();
                                        await imgStream.CopyToAsync(ms);
                                        byte[] imageBytes = ms.ToArray();
                                        var image = await _minIoService.UploadImageAskAiAsync(objectName, extension, imageBytes);
                                        content.Elements.Add(new DocumentElement
                                        {
                                            Type = ElementType.Image,
                                            Content = image.Description,
                                            Url = image.Url,
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
            }

            return content;
        }

        // Crée un fichier .md avec un nom donné et un contenu optionnel
        public static void CreateMarkdownFile(string directory, string fileName, string content = "")
        {
            if (!fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                fileName += ".md";
            string filePath = System.IO.Path.Combine(directory, fileName);
            File.WriteAllText(filePath, content);
        }

        private static DocumentContent ExtractFromExcel(string filePath)
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
            return content;
        }

        private static DocumentContent ExtractFromCsv(string file)
        {
            var content = new DocumentContent();
            using var reader = new StreamReader(file);
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

            return content;
        }
    }
}