namespace Repository.DTO
{
    public class CreateNoteDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? Reminder { get; set; }
        public string Backgroundcolor { get; set; }
        public bool Pin { get; set; }
        public bool Trash { get; set; }
        public bool Archieve { get; set; }
    }
}
