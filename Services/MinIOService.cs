using System.IO;
using Minio;
using Minio.DataModel;
using System;
using WEBAPI_m1IL_1.Utils;
using WEBAPI_m1IL_1.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
using Minio.DataModel.Args;
using Minio.Credentials;
using Minio.Exceptions;
using OpenAI.Responses;
using System.Text;
using WEBAPI_m1IL_1.Helpers;
using System.Security.Policy;
using DocumentFormat.OpenXml.Drawing.Diagrams;
namespace WEBAPI_m1IL_1.Services
{

    public class ResponseAskAiImage
    {
        public string Url { get; set; }
        public string Description { get; set; }
    } 

    public class MinIoService
    {
        private readonly IMinioClient _minio;
        private readonly string _bucketName;
        private AIService _aiService;
        private readonly IConfiguration _config;

        private string bucketName;
        private string serviceUrl;
        private string accessKey;
        private string secretKey;
        private string publicUrl;
        private int port;

        public MinIoService(AIService aiService, IConfiguration configuration)
        {
            _aiService = aiService;
            _config = configuration;
            _bucketName = _config["MinIo:bucket"];
            serviceUrl = _config["MinIo:endpoint"];
            accessKey = _config["MinIo:accessKey"];
            secretKey = _config["MinIo:secretkey"];
            port = int.Parse(_config["MinIo:portApi"]);
            publicUrl = _config["MinIo:publicBaseUrl"];
            _minio = new MinioClient()
               .WithEndpoint(serviceUrl,port)
               .WithCredentials(accessKey, secretKey)
               .Build();


        }

        public async Task InitializeBucketAsync()
        {
            bool exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName));
            if (!exists)
            {
                await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName));
            }
        }

        /// <summary>
        /// Upload une image avec tags et description
        /// </summary>
        public async Task<string> UploadImageAsync(DocumentationFile documentFile, byte[] content)
        {
            await InitializeBucketAsync();
            using var ms = new MemoryStream(content); // imageBytes = ton byte[]


            var metadata = new Dictionary<string, string> {
                { "x-amz-meta-description", FilesUtils.ToAscii(documentFile.Description) },
                {"x-amz-meta-tags", FilesUtils.ToAscii(documentFile.Tags) }
            };
            // Upload avec métadonnée description
            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(documentFile.FullPath)
                .WithFileName(documentFile.Id.ToString())
                .WithStreamData(ms)
                .WithObjectSize(ms.Length)
                .WithContentType("image/png") // à adapter si JPG
                .WithHeaders(metadata)
            );

            return $"{serviceUrl}/{_bucketName}/{Uri.EscapeDataString(documentFile.FullPath)}";
        }

        public async Task<ResponseAskAiImage> UploadImageAskAiAsync(string path,string ext,byte[] content)
        {
            await InitializeBucketAsync();
            using var ms = new MemoryStream(content); // imageBytes = ton byte[]
            string text = System.Text.Encoding.UTF8.GetString(content);
            var images = JsonHelper.ExtractMetadata(await _aiService.AskDescriptionImageToAi(text, SampleUtils.GenerateUUID()));
            var metadata = new Dictionary<string, string> {
                { "x-amz-meta-description", images.Description },
                {"x-amz-meta-tags", images.Tags }
            };
            // Upload avec métadonnée description
            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path)
                .WithFileName(SampleUtils.GenerateUUID())
                .WithStreamData(ms)
                .WithObjectSize(ms.Length)
                .WithContentType($"image/{ext}") // à adapter si JPG
                .WithHeaders(metadata)
            );

            return new ResponseAskAiImage() { Url = $"{serviceUrl}/{_bucketName}/{Uri.EscapeDataString(path)}", Description = images.Description };
        }

        public async Task<string> UploadDocumentFileAsync(DocumentationFile documentFile,string content)
        {
            await InitializeBucketAsync();
            // Métadonnées associées
            var metadata = new Dictionary<string, string> {
            { "x-amz-meta-description", FilesUtils.ToAscii(documentFile.Description) },
            { "x-amz-meta-tags", FilesUtils.ToAscii(documentFile.Tags) }
        };
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);
            using var memoryStream = new MemoryStream(contentBytes);
            try
            {
                await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(documentFile.FullPath)
                .WithStreamData(memoryStream)
                .WithObjectSize(memoryStream.Length)
                .WithContentType("text/markdown")
                .WithHeaders(metadata)
            );
                return $"{serviceUrl}/{_bucketName}/{Uri.EscapeDataString(documentFile.FullPath)}";

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l’upload vers MinIO : {ex.Message}");
                throw;
            }
            
        }

        public async Task<string> CreateDirectory(string path)
        {
            await InitializeBucketAsync();
            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path) // slash final
                .WithStreamData(new MemoryStream())
                .WithObjectSize(0)
                );
            return $"{serviceUrl}/{_bucketName}/{Uri.EscapeDataString(path)}/";
        }
    }
}