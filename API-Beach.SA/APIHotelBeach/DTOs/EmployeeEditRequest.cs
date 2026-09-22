using System.ComponentModel.DataAnnotations;

namespace APIHotelBeach.DTOs
{
    public class EmployeeEditRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "A valid employee ID is required.")]
        public int ID { get; set; }

        [Required(ErrorMessage = "Enter the employee's full name.")]
        [StringLength(150, ErrorMessage = "The full name cannot exceed 150 characters.")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(50, ErrorMessage = "The email address cannot exceed 50 characters.")]
        public string Email { get; set; } = string.Empty;

        [StringLength(128, MinimumLength = 12, ErrorMessage = "The password must contain between 12 and 128 characters.")]
        public string? NewPassword { get; set; }

        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
        public string? ConfirmPassword { get; set; }

        [RegularExpression("^[AI]$", ErrorMessage = "Select a valid account status.")]
        public char Estado { get; set; } = 'A';
    }
}