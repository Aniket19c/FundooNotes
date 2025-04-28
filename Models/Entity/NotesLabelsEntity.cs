using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Entity;

namespace Repository.Entity
{
    public class NoteLabelEntity
    {
        [Key]
        public int NoteLabelId { get; set; }

        public int NoteId { get; set; }
        [ForeignKey("NoteId")]
        public NotesEntity Note { get; set; }

        public int LabelId { get; set; }
        [ForeignKey("LabelId")]
        public LabelEntity Label { get; set; }
    }
}
