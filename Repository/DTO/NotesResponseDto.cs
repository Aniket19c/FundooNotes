using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.DTO
{
    public class NotesResponseDto
    {
        public int NoteId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? Reminder { get; set; }
        public string BackgroundColor { get; set; }
        public string Image { get; set; }
        public bool Pin { get; set; }
        public bool Archieve { get; set; }
        public bool Trash { get; set; }

    }
}
