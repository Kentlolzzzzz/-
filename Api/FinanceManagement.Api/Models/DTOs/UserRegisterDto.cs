using System.ComponentModel.DataAnnotations;

namespace FinanceManagement.Api.Models.DTOs
{
    public class UserRegisterDto
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        public string RealName { get; set; }

        [Required]
        public string Role { get; set; }
    }
} 