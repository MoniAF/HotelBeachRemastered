using System.ComponentModel.DataAnnotations;

namespace AppWebBeachSA.ViewModels
{
    public class CustomerRegisterViewModel
    {
        [Required(ErrorMessage = "Customer ID is required.")]
        [RegularExpression(@"^[0-9]{9}$", ErrorMessage = "Enter an identification number containing exactly 9 digits.")]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "Identification type is required.")]
        [StringLength(20)]
        public string TipoCedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a phone number.")]
        [StringLength(15)]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter an address.")]
        [StringLength(150)]
        public string Direccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter an email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose a password.")]
        [StringLength(128, MinimumLength = 12, ErrorMessage = "Use a password containing between 12 and 128 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm the password.")]
        [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}