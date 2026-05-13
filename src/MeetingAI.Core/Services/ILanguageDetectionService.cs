namespace MeetingAI.Core.Services;

/// <summary>
/// Supported language types for bilingual output.
/// </summary>
public enum LanguageType
{
    /// <summary>
    /// Chinese content.
    /// </summary>
    ZhCn,

    /// <summary>
    /// English content.
    /// </summary>
    EnUs,

    /// <summary>
    /// Mixed Chinese and English content.
    /// </summary>
    Mixed
}

/// <summary>
/// Detects the language type of given text.
/// </summary>
public interface ILanguageDetectionService
{
    /// <summary>
    /// Detects the language type of the given text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>Detected language type.</returns>
    LanguageType Detect(string text);
}