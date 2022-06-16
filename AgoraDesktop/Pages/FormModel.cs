using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AgoraDesktop.Pages
{
    internal class FormModel
    {
        [Required]
        [StringLength(20, ErrorMessage = "Username too long.")]
        public string? Username { get; set; }

        [Required]
        [StringLength(30, ErrorMessage = "Password too long.")]
        public string? Password { get; set; }
    }
}
