using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MeetingAI.Core.Models;

namespace MeetingAI.Core.Repositories
{
    public class MeetingRepository
    {
        private readonly string _storagePath;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public MeetingRepository(string storagePath = null)
        {
            _storagePath = storagePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MeetingAI",
                "Data");
            Directory.CreateDirectory(_storagePath);
        }

        public async Task SaveMeetingAsync(MeetingState meeting)
        {
            await _semaphore.WaitAsync();
            try
            {
                var filePath = GetMeetingFilePath(meeting.Id);
                var json = JsonSerializer.Serialize(meeting, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<MeetingState> LoadMeetingAsync(string meetingId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var filePath = GetMeetingFilePath(meetingId);
                if (!File.Exists(filePath)) return null;
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<MeetingState>(json);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<MeetingState>> LoadAllMeetingsAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var meetings = new List<MeetingState>();
                var files = Directory.GetFiles(_storagePath, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var meeting = JsonSerializer.Deserialize<MeetingState>(json);
                        if (meeting != null) meetings.Add(meeting);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading {file}: {ex.Message}");
                    }
                }
                return meetings;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DeleteMeetingAsync(string meetingId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var filePath = GetMeetingFilePath(meetingId);
                if (File.Exists(filePath)) File.Delete(filePath);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private string GetMeetingFilePath(string meetingId)
        {
            var safeFileName = string.Join("_", meetingId.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_storagePath, $"{safeFileName}.json");
        }
    }
}