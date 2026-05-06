using System;
using System.Collections.Concurrent;

namespace MeetingAI.Core.Audio
{
    public class AudioBufferPool
    {
        private readonly ConcurrentBag<byte[]> _pool;
        private readonly int _bufferSize;
        private readonly int _maxPoolSize;

        public AudioBufferPool(int bufferSize = 4096, int maxPoolSize = 100)
        {
            _bufferSize = bufferSize;
            _maxPoolSize = maxPoolSize;
            _pool = new ConcurrentBag<byte[]>();
        }

        public byte[] Rent()
        {
            if (_pool.TryTake(out var buffer)) return buffer;
            return new byte[_bufferSize];
        }

        public void Return(byte[] buffer)
        {
            if (buffer == null || buffer.Length != _bufferSize) return;
            if (_pool.Count < _maxPoolSize)
            {
                Array.Clear(buffer, 0, buffer.Length);
                _pool.Add(buffer);
            }
        }

        public void Clear()
        {
            while (_pool.TryTake(out _)) { }
        }

        public int Count => _pool.Count;
    }
}