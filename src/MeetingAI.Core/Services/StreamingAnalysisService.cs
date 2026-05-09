using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MeetingAI.Core.Models;
using MeetingAI.Core.State;

namespace MeetingAI.Core.Services
{
    public class StreamingAnalysisService
    {
        private readonly MeetingStateManager _stateManager;
        private readonly Dictionary<string, StringBuilder> _activeStreams = new();
        private readonly Dictionary<string, List<string>> _streamChunks = new();
        private readonly object _lock = new();

        public event EventHandler<StreamProgressEventArgs>? ProgressChanged;

        public StreamingAnalysisService(MeetingStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        public async Task<AiAnalysisResult> StreamAnalysisAsync(
            string meetingId,
            AnalysisType analysisType,
            IAsyncEnumerable<string> stream,
            CancellationToken cancellationToken = default)
        {
            var streamId = Guid.NewGuid().ToString();
            var result = new AiAnalysisResult
            {
                Id = streamId,
                MeetingId = meetingId,
                Type = analysisType,
                ContentType = "markdown",
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                lock (_lock)
                {
                    _activeStreams[streamId] = new StringBuilder();
                    _streamChunks[streamId] = new List<string>();
                }

                var startTime = DateTime.UtcNow;
                var chunkCount = 0;

                await foreach (var chunk in stream.WithCancellation(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    lock (_lock)
                    {
                        _activeStreams[streamId].Append(chunk);
                        _streamChunks[streamId].Add(chunk);
                        chunkCount++;
                    }

                    if (chunkCount % 10 == 0)
                    {
                        OnProgressChanged(new StreamProgressEventArgs
                        {
                            StreamId = streamId,
                            MeetingId = meetingId,
                            ChunksProcessed = chunkCount,
                            CurrentLength = _activeStreams[streamId].Length
                        });
                    }
                }

                result.ProcessingDuration = DateTime.UtcNow - startTime;
                result.IsSuccess = true;
                lock (_lock)
                {
                    result.Content = _activeStreams[streamId].ToString();
                }
                _stateManager.AddAnalysis(meetingId, result);
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
            finally
            {
                lock (_lock)
                {
                    _activeStreams.Remove(streamId);
                    _streamChunks.Remove(streamId);
                }
            }
        }

        public string? GetCurrentContent(string streamId)
        {
            lock (_lock)
            {
                return _activeStreams.TryGetValue(streamId, out var sb) ? sb.ToString() : null;
            }
        }

        public List<string> GetStreamChunks(string streamId)
        {
            lock (_lock)
            {
                return _streamChunks.TryGetValue(streamId, out var chunks)
                    ? new List<string>(chunks)
                    : new List<string>();
            }
        }

        protected virtual void OnProgressChanged(StreamProgressEventArgs e)
        {
            ProgressChanged?.Invoke(this, e);
        }
    }

    public class StreamProgressEventArgs : EventArgs
    {
        public string StreamId { get; set; } = string.Empty;
        public string MeetingId { get; set; } = string.Empty;
        public int ChunksProcessed { get; set; }
        public int CurrentLength { get; set; }
    }
}