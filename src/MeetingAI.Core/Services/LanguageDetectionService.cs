using System.Text.RegularExpressions;

namespace MeetingAI.Core.Services;

public sealed class LanguageDetectionService : ILanguageDetectionService
{
    // Chinese Unicode range: 一-鿿 (CJK Unified Ideographs)
    private static readonly Regex ChineseCharsRegex = new(@"一-鿿", RegexOptions.Compiled);
    // Latin alphabet range
    private static readonly Regex LatinCharsRegex = new(@"[a-zA-Z]", RegexOptions.Compiled);

    private const double Threshold = 0.30;

    public LanguageType Detect(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return LanguageType.Mixed;

        var chineseCount = ChineseCharsRegex.Matches(text).Count;
        var latinCount = LatinCharsRegex.Matches(text).Count;
        var totalChars = chineseCount + latinCount;

        if (totalChars == 0)
            return LanguageType.Mixed;

        var chineseRatio = (double)chineseCount / totalChars;
        var latinRatio = (double)latinCount / totalChars;

        if (chineseRatio > Threshold)
            return LanguageType.ZhCn;

        if (latinRatio > Threshold)
            return LanguageType.EnUs;

        return LanguageType.Mixed;
    }
}