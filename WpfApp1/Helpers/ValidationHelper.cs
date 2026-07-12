using System.Net.Mail;

namespace RalseiWarehouse.Helpers;

/// <summary>Contains reusable application-layer validation required by the SRS.</summary>
public static class ValidationHelper
{
    /// <summary>Validates partner contact fields.</summary>
    /// <param name="displayName">The required display name.</param>
    /// <param name="email">The optional email address.</param>
    /// <param name="phone">The optional phone number.</param>
    public static void ValidatePartner(string? displayName, string? email, string? phone)
    {
        Require(displayName, "Display name");
        if (!string.IsNullOrWhiteSpace(email) && (email.Length > 200 || !MailAddress.TryCreate(email, out _))) throw new InvalidOperationException("Enter a valid email address.");
        if (!string.IsNullOrWhiteSpace(phone) && (phone.Length > 20 || phone.Any(c => !char.IsDigit(c) && !" +-()".Contains(c)))) throw new InvalidOperationException("Enter a valid phone number (maximum 20 characters).");
    }

    /// <summary>Requires a nonblank text value.</summary>
    /// <param name="value">The value to inspect.</param>
    /// <param name="field">The user-facing field name.</param>
    public static void Require(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"{field} is required.");
    }
}
