using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Entity;

namespace Repository.Entity
{
    public class LabelEntity
    {
        [Key]
        public int LabelId { get; set; }

        [Required]
        public string LabelName { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public UserEntity User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        
        public ICollection<NoteLabelEntity> NoteLabels { get; set; }
    }
}
