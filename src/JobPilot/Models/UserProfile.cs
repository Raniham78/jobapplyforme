namespace JobPilot.Models;

/// <summary>
/// Personal data used to pre-fill common job application fields.
/// The real profile is stored locally and must never be committed.
/// </summary>
public sealed class UserProfile
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = "Italy";
    public string CurrentTitle { get; set; } = "Senior Software Developer";
    public int YearsOfExperience { get; set; } = 20;
    public string LinkedInUrl { get; set; } = string.Empty;
    public string GitHubUrl { get; set; } = string.Empty;
    public string PortfolioUrl { get; set; } = string.Empty;
    public string ResumePath { get; set; } = string.Empty;

    public string FullName => string.Join(' ', new[] { FirstName, LastName }
        .Where(value => !string.IsNullOrWhiteSpace(value)));
}
