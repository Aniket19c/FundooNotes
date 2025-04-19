using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Repository.DTO
{
    public class UserRequest
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
