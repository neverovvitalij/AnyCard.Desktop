namespace AnyCard.Desktop.Models;

public record ResetPasswordDto
(
    string Email,
    string Code,
    string NewPassword
);
