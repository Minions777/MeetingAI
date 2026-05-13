using System.Text.Json.Serialization;

namespace MeetingAI.Core.Models;

/// <summary>
/// Container for terminology entries loaded from JSON.
/// </summary>
public sealed class TerminologyDb
{
    [JsonPropertyName("terms")]
    public List<Term> Terms { get; set; } = new();
}