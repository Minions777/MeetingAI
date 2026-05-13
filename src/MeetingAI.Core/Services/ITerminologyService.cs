using MeetingAI.Core.Models;

namespace MeetingAI.Core.Services;

/// <summary>
/// Provides terminology management for translation protection.
/// </summary>
public interface ITerminologyService
{
    /// <summary>
    /// Loads terminology from the specified JSON file path.
    /// </summary>
    /// <param name="filePath">Path to the terminology JSON file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Loaded terminology database or null if file not found.</returns>
    Task<TerminologyDb?> LoadFromFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Gets the built-in default terminology.
    /// </summary>
    /// <returns>Terminology database with hardcoded technical terms.</returns>
    TerminologyDb GetDefault();

    /// <summary>
    /// Gets a formatted string list of protected terms for prompt injection.
    /// </summary>
    /// <param name="db">The terminology database.</param>
    /// <returns>Formatted string of term pairs.</returns>
    string FormatTerminologyList(TerminologyDb db);
}