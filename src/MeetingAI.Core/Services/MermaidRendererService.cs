using MeetingAI.Core.Models;

namespace MeetingAI.Core.Services;

public sealed class MermaidRendererService : IMermaidRendererService
{
    private static readonly string MermaidHtmlTemplate = """
        <!DOCTYPE html>
        <html lang="zh-CN">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>会议摘要 - 思维导图</title>
            <script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
            <style>
                * {
                    margin: 0;
                    padding: 0;
                    box-sizing: border-box;
                }
                body {
                    font-family: 'Segoe UI', 'Microsoft YaHei', sans-serif;
                    background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
                    min-height: 100vh;
                    padding: 20px;
                }
                .container {
                    max-width: 1200px;
                    margin: 0 auto;
                    background: white;
                    border-radius: 16px;
                    box-shadow: 0 20px 60px rgba(0,0,0,0.3);
                    overflow: hidden;
                }
                .header {
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    padding: 24px 32px;
                }
                .header h1 {
                    font-size: 24px;
                    font-weight: 600;
                    margin-bottom: 4px;
                }
                .header .chart-type {
                    font-size: 14px;
                    opacity: 0.9;
                }
                .content {
                    padding: 32px;
                    background: #fafbfc;
                }
                .mermaid {
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    min-height: 400px;
                }
                .mermaid svg {
                    max-width: 100%;
                    height: auto;
                }
                .fallback {
                    padding: 20px;
                    background: #fff3cd;
                    border: 1px solid #ffc107;
                    border-radius: 8px;
                    color: #856404;
                }
                .footer {
                    padding: 16px 32px;
                    background: #f8f9fa;
                    border-top: 1px solid #e9ecef;
                    text-align: center;
                    color: #6c757d;
                    font-size: 13px;
                }
            </style>
        </head>
        <body>
            <div class="container">
                <div class="header">
                    <h1>会议摘要</h1>
                    <div class="chart-type">{chartType}</div>
                </div>
                <div class="content">
                    <div class="mermaid">
                        {mermaidCode}
                    </div>
                </div>
                <div class="footer">
                    由 MeetingAI 生成
                </div>
            </div>
            <script>
                mermaid.initialize({
                    startOnLoad: true,
                    theme: 'base',
                    themeVariables: {
                        fontFamily: 'Segoe UI, Microsoft YaHei, sans-serif',
                        fontSize: '16px',
                        primaryColor: '#667eea',
                        primaryTextColor: '#fff',
                        primaryBorderColor: '#764ba2',
                        lineColor: '#6366f1',
                        secondaryColor: '#f1f5f9',
                        tertiaryColor: '#fff'
                    },
                    flowchart: {
                        curve: 'basis',
                        padding: 20
                    },
                    mindmap: {
                        padding: 18,
                        useMaxWidth: true
                    }
                });
            </script>
        </body>
        </html>
        """;

    public MermaidRenderResult Render(Summary summary)
    {
        var chartType = DetectChartType(summary);
        return Render(summary, chartType);
    }

    public MermaidRenderResult Render(Summary summary, MermaidChartType chartType)
    {
        var syntax = chartType switch
        {
            MermaidChartType.MindMap => MermaidTemplateGenerator.GenerateMindMap(summary),
            MermaidChartType.Flowchart => MermaidTemplateGenerator.GenerateFlowchart(summary),
            MermaidChartType.Gantt => MermaidTemplateGenerator.GenerateGantt(summary),
            MermaidChartType.Pie => MermaidTemplateGenerator.GeneratePie(summary),
            _ => MermaidTemplateGenerator.GenerateMindMap(summary)
        };

        var chartTypeLabel = chartType switch
        {
            MermaidChartType.MindMap => "思维导图",
            MermaidChartType.Flowchart => "流程图",
            MermaidChartType.Gantt => "甘特图",
            MermaidChartType.Pie => "饼图",
            _ => "思维导图"
        };

        return new MermaidRenderResult(syntax, chartType, GenerateHtml(syntax, chartTypeLabel));
    }

    public MermaidChartType DetectChartType(Summary summary)
    {
        return MermaidTemplateGenerator.DetectFromContent(summary);
    }

    public string GenerateHtml(string mermaidSyntax)
    {
        return GenerateHtml(mermaidSyntax, "思维导图");
    }

    private string GenerateHtml(string mermaidSyntax, string chartTypeLabel)
    {
        return MermaidHtmlTemplate
            .Replace("{mermaidCode}", mermaidSyntax)
            .Replace("{chartType}", chartTypeLabel);
    }
}