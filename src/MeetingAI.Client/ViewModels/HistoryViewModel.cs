using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Client.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly MeetingHistoryService _historyService;

    [ObservableProperty] private ObservableCollection<MeetingRecord> _meetingHistory = new();

    public HistoryViewModel(MeetingHistoryService historyService)
    {
        _historyService = historyService;
    }

    public async Task LoadRecentAsync(int count = 10)
    {
        try
        {
            var history = await _historyService.GetRecentAsync(count);
            MeetingHistory.Clear();
            foreach (var record in history)
                MeetingHistory.Add(record);
            LoggerService.Info($"Loaded {MeetingHistory.Count} meeting records from history");
        }
        catch (Exception ex)
        {
            LoggerService.Error("Failed to load meeting history", ex);
        }
    }

    public async Task DeleteAsync(MeetingRecord? record)
    {
        if (record == null) return;

        try
        {
            await _historyService.DeleteAsync(record.Id);
            MeetingHistory.Remove(record);
            LoggerService.Info($"Meeting deleted from history: {record.Title}");
        }
        catch (Exception ex)
        {
            LoggerService.Error("Failed to delete meeting", ex);
        }
    }

    public async Task SaveAsync(MeetingRecord record)
    {
        try
        {
            await _historyService.SaveAsync(record);
            await LoadRecentAsync();
            LoggerService.Info($"Meeting saved to history: {record.Title}");
        }
        catch (Exception ex)
        {
            LoggerService.Error("Failed to save meeting", ex);
        }
    }
}