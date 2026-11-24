namespace JobPilot.Models;

public sealed record JobApplication(
    long Id,
    string Company,
    string Role,
    string Url,
    string Status,
    DateTime CreatedUtc);
