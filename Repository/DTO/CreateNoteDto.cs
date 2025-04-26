using System.ComponentModel;

namespace Repository.DTO
{
    public class CreateNoteDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? Reminder { get; set; }
        public string Backgroundcolor { get; set; }

        [DefaultValue(false)]
        public bool Pin { get; set; }

        [DefaultValue(false)]
        public bool Trash { get; set; }

        [DefaultValue(false)]
        public bool Archieve { get; set; }
    }
}
