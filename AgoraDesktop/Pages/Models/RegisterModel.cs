using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgoraDesktop.Pages.Models
{
    public class RegisterModel
    {
        [Required]
        [MaxLength(20, ErrorMessage = "Username too long.")]
        public string? Username { get; set; }

        [Required]
        [MaxLength(30, ErrorMessage = "Password too long.")]
        [MinLength(8, ErrorMessage = "Password too short.")]
        public string? Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Must be the same as the Password field.")]
        public string? ConfirmPassword { get; set; }
    }
}
