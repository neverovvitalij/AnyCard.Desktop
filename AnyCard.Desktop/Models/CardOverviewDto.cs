namespace AnyCard.Desktop.Models;

public record CardOverviewDto
(
    int CardId,
    string CategoryName,
    string Question,
    UserRating UserRating,
    int CurrentStreak,
    DateTime NextShowtime
);
