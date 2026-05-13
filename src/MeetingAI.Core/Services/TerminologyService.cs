using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public sealed class TerminologyService : ITerminologyService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<TerminologyDb?> LoadFromFileAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            LoggerService.Warning($"Terminology file not found: {filePath}");
            return Task.FromResult<TerminologyDb?>(null);
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var db = JsonSerializer.Deserialize<TerminologyDb>(json, JsonOptions);
            LoggerService.Info($"Loaded {db?.Terms.Count ?? 0} terminology entries from {filePath}");
            return Task.FromResult(db);
        }
        catch (Exception ex)
        {
            LoggerService.Error($"Failed to load terminology from {filePath}", ex);
            return Task.FromResult<TerminologyDb?>(null);
        }
    }

    public TerminologyDb GetDefault()
    {
        var defaultTerms = new[]
        {
            "Kubernetes", "API", "REST", "GraphQL", "OAuth", "SDK", "CLI",
            "IDE", "CI/CD", "DevOps", "Git", "PR", "MR", "CPU", "GPU",
            "RAM", "SQL", "NoSQL", "DNS", "TCP", "UDP", "HTTP", "HTTPS",
            "TLS", "SSH", "VPN"
        };

        var db = new TerminologyDb();
        foreach (var term in defaultTerms)
        {
            db.Terms.Add(new Term { En = term, Zh = term, Protect = true });
        }

        return db;
    }

    public string FormatTerminologyList(TerminologyDb db)
    {
        if (db.Terms.Count == 0)
            return string.Empty;

        var lines = db.Terms.Select(t => $"{t.En} = {t.Zh}");
        return string.Join("\n", lines);
    }
}