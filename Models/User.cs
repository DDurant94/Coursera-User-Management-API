using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UserManagementAPI.Models;

public class User
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Id { get; set; }

    [Required(ErrorMessage = "UserName is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "UserName must be between 2 and 100 characters.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [StringLength(200, ErrorMessage = "Email must not exceed 200 characters.")]
    public string Email { get; set; } = string.Empty;
}
