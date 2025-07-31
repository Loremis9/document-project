namespace WEBAPI_m1IL_1.DTO
{
    public class OutputDocumentFiles
    {
        public int DocumentationId { get; set; }
        public int UserId { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanDelete { get; set; }
        public bool IsAdmin { get; set; }
    }
}