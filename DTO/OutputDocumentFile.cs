namespace WEBAPI_m1IL_1.DTO
{
    public class OutputDocumentFile
    {
        public int Id { get; set; }
        public int DocumentationId { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public byte[] FileContent { get; set; }
    }
}
