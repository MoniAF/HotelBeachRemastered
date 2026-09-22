using System.ComponentModel.DataAnnotations;

namespace APIHotelBeach.DTOs
{
    public class CustomerRegisterRequest
    {
        [Required]
        [RegularExpression(@"^[0-9]{9}$", ErrorMessage = "Enter a national identification number containing exactly 9 digits.")]
        public string Cedula { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string TipoCedula { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string Telefono { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Direccion { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 12, ErrorMessage = "Use a password containing between 12 and 128 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}