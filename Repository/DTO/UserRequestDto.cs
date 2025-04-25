using System.ComponentModel.DataAnnotations;

namespace Repository.DTO
{
    public class UserRequestDto
    {
        [Required]
        public string firstName { get; set; }
        public string lastName { get; set; }
        [Required]
        [EmailAddress]

        public string email { get; set; }

        [Required]

        public string password { get; set; }
    }
}
