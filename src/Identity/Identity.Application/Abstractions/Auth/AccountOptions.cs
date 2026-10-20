namespace Identity.Application.Abstractions.Auth;

public sealed class AccountOptions
{
    public const string SectionName = "Account";

    /// <summary>Base URL of the frontend; emailed links point to its /confirm-email and /reset-password pages.</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>When true, users can't log in until they confirm their email address.</summary>
    public bool RequireConfirmedEmail { get; set; }

    public int RefreshTokenLifetimeDays { get; set; } = 7;
    public int EmailConfirmationTokenLifetimeHours { get; set; } = 24;
    public int PasswordResetTokenLifetimeMinutes { get; set; } = 60;
}
