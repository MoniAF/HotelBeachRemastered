using System.ComponentModel.DataAnnotations;

namespace APIHotelBeach.DTOs
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Enter your current password.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your new password.")]
        [StringLength(128, MinimumLength = 12, ErrorMessage = "Use a password containing between 12 and 128 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm your new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}