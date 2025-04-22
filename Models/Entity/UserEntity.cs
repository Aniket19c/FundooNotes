using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Entity
{
    public class UserEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
      public  int ID { get; set; }
        [Required]
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [Required]
        [EmailAddress]

        public string Email { get; set; }

        [Required]

        public string Password { get; set; }
        public ICollection<NotesEntity> Notes { get; set; }
    }
}
