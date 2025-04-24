using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Entity;

namespace Repository.Entity
{
    public class CollaboratorEntity
    {
        [Key]
        public int CollaboratorId { get; set; }

        [Required(ErrorMessage = "Collaborator email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string CollaboratorEmail { get; set; } 

        [ForeignKey("Notes")]
        public int NoteId { get; set; }

        public virtual NotesEntity Note { get; set; }

        [ForeignKey("Users")]
        public int UserId { get; set; }

        public virtual UserEntity User { get; set; }
    }
}
