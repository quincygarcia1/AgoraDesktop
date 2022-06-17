using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AgoraDesktop.Pages.Models
{
    internal class LoginModel
    {
        [Required]
        [MaxLength(20, ErrorMessage = "Username too long.")]
        public string? Username { get; set; }

        [Required]
        [MaxLength(30, ErrorMessage = "Password too long.")]
        [MinLength(8, ErrorMessage = "Password too short.")]
        public string? Password { get; set; }
    }
}
