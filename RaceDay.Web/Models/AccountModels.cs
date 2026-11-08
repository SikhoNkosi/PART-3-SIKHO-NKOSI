using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    // What the API sends back after a successful login
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    // What the API sends back for a profile
    public class ProfileDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please enter your full name.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "That doesn't look like a valid phone number.")]
        [Display(Name = "Phone number (optional)")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Please choose how you will use RaceDay.")]
        [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose Participant or Organiser.")]
        [Display(Name = "I am a")]
        public string Role { get; set; } = "Participant";

        [Required(ErrorMessage = "Please choose a password.")]
        [MinLength(6, ErrorMessage = "Your password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please type your password again.")]
        [Compare("Password", ErrorMessage = "The two passwords don't match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        // Shown on the page but not edited, so they are optional when the form is posted back
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? ProfilePictureUrl { get; set; }

        [Required(ErrorMessage = "Please enter your full name.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "That doesn't look like a valid phone number.")]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }
    }
}
