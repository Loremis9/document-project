namespace WEBAPI_m1IL_1.DTO
{//int documentationId, int UserToChange, bool read, bool write, bool delete, bool admin
    public class InputRigth
    {
        public int DocumentationId { get; set; }
        public int UserToChange { get; set; }
        public bool Read { get; set; }
        public bool Write { get; set; }
        public bool Delete { get; set; }
        public bool Admin { get; set; }
    }
}
