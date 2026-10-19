namespace AnyCard.Desktop.Models;

public record UserSettingsDto
(
    TimeSpan Again,
    TimeSpan Hard,
    TimeSpan Good,
    TimeSpan Easy
);

