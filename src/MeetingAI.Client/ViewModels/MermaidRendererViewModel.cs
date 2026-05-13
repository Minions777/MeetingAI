using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Client.ViewModels;

public partial class MermaidRendererViewModel : ObservableObject
{
    private readonly IMermaidRendererService _mermaidRenderer;

    [ObservableProperty] private string _mermaidSyntax = string.Empty;
    [ObservableProperty] private string _htmlContent = string.Empty;
    [ObservableProperty] private MermaidChartType _currentChartType = MermaidChartType.MindMap;
    [ObservableProperty] private string _chartTypeLabel = "思维导图";
    [ObservableProperty] private bool _hasContent;
    [ObservableProperty] private bool _isRendering;

    public MermaidRendererViewModel(IMermaidRendererService mermaidRenderer)
    {
        _mermaidRenderer = mermaidRenderer;
    }

    [RelayCommand]
    public void Render(Summary summary)
    {
        if (summary == null) return;

        IsRendering = true;
        try
        {
            var result = _mermaidRenderer.Render(summary);
            MermaidSyntax = result.MermaidSyntax;
            HtmlContent = result.HtmlContent;
            CurrentChartType = result.ChartType;
            ChartTypeLabel = result.ChartType switch
            {
                MermaidChartType.MindMap => "思维导图",
                MermaidChartType.Flowchart => "流程图",
                MermaidChartType.Gantt => "甘特图",
                MermaidChartType.Pie => "饼图",
                _ => "思维导图"
            };
            HasContent = !string.IsNullOrWhiteSpace(MermaidSyntax);
            LoggerService.Info($"Mermaid rendered: {CurrentChartType}");
        }
        catch (Exception ex)
        {
            LoggerService.Error("Mermaid rendering failed", ex);
            MermaidSyntax = string.Empty;
            HtmlContent = string.Empty;
            HasContent = false;
        }
        finally
        {
            IsRendering = false;
        }
    }

    [RelayCommand]
    public void SetChartType(MermaidChartType chartType)
    {
        if (!HasContent) return;

        CurrentChartType = chartType;
        ChartTypeLabel = chartType switch
        {
            MermaidChartType.MindMap => "思维导图",
            MermaidChartType.Flowchart => "流程图",
            MermaidChartType.Gantt => "甘特图",
            MermaidChartType.Pie => "饼图",
            _ => "思维导图"
        };

        // Re-render with new type - we need a Summary object for this
        // This command is for switching display mode, not re-rendering from scratch
        LoggerService.Info($"Chart type switched to: {chartType}");
    }

    [RelayCommand]
    public async Task ExportToPngAsync()
    {
        if (!HasContent || string.IsNullOrEmpty(HtmlContent)) return;

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"MeetingAI_MindMap_{DateTime.Now:yyyyMMddHHmmss}.html");
            await File.WriteAllTextAsync(tempPath, HtmlContent);

            // Note: Actual PNG export would require a headless browser or WebView control
            // For now, we save the HTML which can be opened in a browser and printed to PDF/PNG
            LoggerService.Info($"Exported to: {tempPath}");

            // Try to open with default browser
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = tempPath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            LoggerService.Error("Export failed", ex);
        }
    }

    [RelayCommand]
    public async Task ExportToMermaidAsync()
    {
        if (!HasContent || string.IsNullOrEmpty(MermaidSyntax)) return;

        try
        {
            var savePath = Path.Combine(Path.GetTempPath(), $"MeetingAI_Diagram_{DateTime.Now:yyyyMMddHHmmss}.mmd");
            await File.WriteAllTextAsync(savePath, MermaidSyntax);

            LoggerService.Info($"Mermaid syntax saved to: {savePath}");

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = savePath,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            LoggerService.Error("Mermaid export failed", ex);
        }
    }

    public void Clear()
    {
        MermaidSyntax = string.Empty;
        HtmlContent = string.Empty;
        HasContent = false;
        CurrentChartType = MermaidChartType.MindMap;
        ChartTypeLabel = "思维导图";
    }
}