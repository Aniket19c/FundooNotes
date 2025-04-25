using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Entity
{
    public class NotesEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NoteId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? Reminder { get; set; }
        public string? Backgroundcolor { get; set; }
        public string? Image { get; set; }
        public bool Pin { get; set; }
        public DateTime Created { get; set; }
        public DateTime Edited { get; set; }
        public bool Trash { get; set; }
        public bool Archieve { get; set; }
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public UserEntity? User { get; set; }

       
    }
}
