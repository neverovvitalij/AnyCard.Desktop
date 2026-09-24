
namespace AnyCard.Desktop.Models;

public record CreateCardDto
(
    string Question,
    string Answer,
    int CategoryId
);
