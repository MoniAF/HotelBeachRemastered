using System.ComponentModel.DataAnnotations;

namespace AppWebBeachSA.ViewModels
{
    public class EmployeeCreateViewModel
    {
        [Required(ErrorMessage = "Enter the employee's full name.")]
        [StringLength(150, ErrorMessage = "The full name cannot exceed 150 characters.")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(50, ErrorMessage = "The email address cannot exceed 50 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a password.")]
        [StringLength(128, MinimumLength = 12, ErrorMessage = "The password must contain between 12 and 128 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm the password.")]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        [RegularExpression("^[AI]$", ErrorMessage = "Select a valid account status.")]
        public char Estado { get; set; } = 'A';
    }
}