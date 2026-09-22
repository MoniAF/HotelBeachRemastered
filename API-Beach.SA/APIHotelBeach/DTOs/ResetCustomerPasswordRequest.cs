using System.ComponentModel.DataAnnotations;

namespace APIHotelBeach.DTOs
{
    public class ResetCustomerPasswordRequest
    {
        [Required(ErrorMessage = "The customer identification number is required.")]
        [StringLength(25)]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the new password.")]
        [StringLength(128, MinimumLength = 12, ErrorMessage = "The password must contain between 12 and 128 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm the new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public bool IdentityVerified { get; set; }
    }
}