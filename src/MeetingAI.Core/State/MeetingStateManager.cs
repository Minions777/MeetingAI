using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeetingAI.Core.Models;

namespace MeetingAI.Core.State
{
    public class MeetingStateManager
    {
        private readonly Dictionary<string, MeetingState> _meetings = new();
        private readonly object _lock = new();

        public event EventHandler<MeetingStateChangedEventArgs>? StateChanged;

        public MeetingState CreateMeeting(string title)
        {
            lock (_lock)
            {
                var meeting = new MeetingState
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = title,
                    CreatedAt = DateTime.UtcNow,
                    Status = MeetingStatus.Created
                };
                _meetings[meeting.Id] = meeting;
                OnStateChanged(meeting, MeetingAction.Created);
                return meeting;
            }
        }

        public MeetingState? GetMeeting(string meetingId)
        {
            lock (_lock)
            {
                return _meetings.TryGetValue(meetingId, out var meeting) ? meeting : null;
            }
        }

        public List<MeetingState> GetAllMeetings()
        {
            lock (_lock)
            {
                return _meetings.Values.OrderByDescending(m => m.CreatedAt).ToList();
            }
        }

        public bool StartRecording(string meetingId)
        {
            lock (_lock)
            {
                var meeting = GetMeeting(meetingId);
                if (meeting == null || meeting.Status != MeetingStatus.Created)
                    return false;
                meeting.Status = MeetingStatus.Recording;
                meeting.RecordingStartedAt = DateTime.UtcNow;
                OnStateChanged(meeting, MeetingAction.RecordingStarted);
                return true;
            }
        }

        public bool StopRecording(string meetingId)
        {
            lock (_lock)
            {
                var meeting = GetMeeting(meetingId);
                if (meeting == null || meeting.Status != MeetingStatus.Recording)
                    return false;
                meeting.Status = MeetingStatus.Recorded;
                meeting.RecordingStoppedAt = DateTime.UtcNow;
                OnStateChanged(meeting, MeetingAction.RecordingStopped);
                return true;
            }
        }

        public void AddTranscription(string meetingId, TranscriptSegment segment)
        {
            lock (_lock)
            {
                var meeting = GetMeeting(meetingId);
                if (meeting == null) return;
                meeting.Transcriptions.Add(segment);
                OnStateChanged(meeting, MeetingAction.TranscriptionAdded);
            }
        }

        public void AddAnalysis(string meetingId, AiAnalysisResult analysis)
        {
            lock (_lock)
            {
                var meeting = GetMeeting(meetingId);
                if (meeting == null) return;
                analysis.MeetingId = meetingId;
                analysis.CreatedAt = DateTime.UtcNow;
                meeting.Analyses.Add(analysis);
                OnStateChanged(meeting, MeetingAction.AnalysisAdded);
            }
        }

        public bool CompleteMeeting(string meetingId)
        {
            lock (_lock)
            {
                var meeting = GetMeeting(meetingId);
                if (meeting == null) return false;
                meeting.Status = MeetingStatus.Completed;
                meeting.CompletedAt = DateTime.UtcNow;
                OnStateChanged(meeting, MeetingAction.Completed);
                return true;
            }
        }

        protected virtual void OnStateChanged(MeetingState meeting, MeetingAction action)
        {
            StateChanged?.Invoke(this, new MeetingStateChangedEventArgs(meeting, action));
        }
    }

    public class MeetingStateChangedEventArgs : EventArgs
    {
        public MeetingState Meeting { get; }
        public MeetingAction Action { get; }
        public MeetingStateChangedEventArgs(MeetingState meeting, MeetingAction action)
        {
            Meeting = meeting;
            Action = action;
        }
    }

    public enum MeetingAction
    {
        Created,
        RecordingStarted,
        RecordingStopped,
        TranscriptionAdded,
        AnalysisAdded,
        Completed
    }
}