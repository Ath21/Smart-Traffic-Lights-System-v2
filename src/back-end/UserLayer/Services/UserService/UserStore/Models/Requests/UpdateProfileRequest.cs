using System.ComponentModel.DataAnnotations;

namespace UserStore.Models.Requests;

public class UpdateProfileRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? Password { get; set; }

    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }
}
