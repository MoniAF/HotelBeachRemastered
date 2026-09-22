using System.ComponentModel.DataAnnotations;

namespace AppWebBeachSA.ViewModels
{
    public class CustomerEditViewModel
    {
        [Required(ErrorMessage = "Customer ID is required.")]
        [StringLength(25)]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the customer's full name.")]
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
    }
}