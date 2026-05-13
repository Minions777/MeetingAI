namespace MeetingAI.Core.Models;

/// <summary>
/// Represents a terminology entry for translation protection.
/// </summary>
public sealed class Term
{
    /// <summary>
    /// English term.
    /// </summary>
    public required string En { get; init; }

    /// <summary>
    /// Chinese translation or same term if protected.
    /// </summary>
    public required string Zh { get; init; }

    /// <summary>
    /// Whether this term should be protected from translation.
    /// </summary>
    public bool Protect { get; init; }
}