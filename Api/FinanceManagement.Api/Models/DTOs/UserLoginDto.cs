using System.ComponentModel.DataAnnotations;

namespace FinanceManagement.Api.Models.DTOs
{
    public class UserLoginDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
} 