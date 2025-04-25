using System.ComponentModel.DataAnnotations;

namespace Repository.DTO
{
    public class LoginDto
    {
        [EmailAddress]
        [Required]
        public   string email { get; set; }
        [Required]
        public string password {  get; set; }
    }
}
