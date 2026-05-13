namespace MeetingAI.Core.Services;

/// <summary>
/// Represents a translation result with original and translated text.
/// </summary>
public sealed record TranslationResult(
    string Original,
    string Translation
);

/// <summary>
/// Provides translation services with terminology protection.
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translates text with terminology protection.
    /// </summary>
    /// <param name="text">Text to translate.</param>
    /// <param name="sourceLanguage">Detected source language.</param>
    /// <param name="terminologyList">Formatted terminology list for protection.</param>
    /// <param name="providerId">Optional AI provider to use.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Translation result with original and translated text.</returns>
    Task<TranslationResult> TranslateAsync(
        string text,
        LanguageType sourceLanguage,
        string terminologyList,
        string? providerId = null,
        CancellationToken ct = default);
}