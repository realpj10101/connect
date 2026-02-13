using System.ComponentModel.DataAnnotations;

namespace api.DTOs;

public record RegisterDto(
    [MaxLength(PropLength.EmailMaxLength)]
    [RegularExpression(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,5})+)$", ErrorMessage = "Bad Email Format.")]
    string Email,
    [Length(PropLength.UserNameMinLength, PropLength.UserNameMaxLength)]
    string UserName,
    [DataType(DataType.Password)]
    [Length(PropLength.PasswordMinLength, PropLength.PasswordMaxLength)]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Needs: 8 to 50 characters. An uppercase character(ABC). A number(123)")]
    string Password,
    string ConfirmPassword
);