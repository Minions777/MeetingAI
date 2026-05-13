#if MACOS
using System.Runtime.InteropServices;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

/// <summary>
/// macOS audio capture using CoreAudio AudioQueue API via P/Invoke.
/// Supports microphone input. System audio loopback requires BlackHole or similar virtual device.
/// </summary>
public class MacAudioCapture : IAudioCapture
{
    private const int kAudioFormatLinearPCM = 0x6C70636D; // 'lpcm'
    private const int kAudioFormatFlagIsSignedInteger = 0x4;
    private const int kAudioFormatFlagIsPacked = 0x8;
    private const int kAudioFormatFlagsNativeEndian = 0x2;
    private const int kAudioQueueProperty_IsRunning = 0x6171726E; // 'aqrn'

    private IntPtr _queue;
    private IntPtr[] _buffers = new IntPtr[3];
    private GCHandle _gcHandle;
    private bool _isRecording;
    private bool _disposed;

    public bool IsRecording => _isRecording;
    public int SampleRate => 44100;
    public int Channels => 1;

    public event EventHandler<byte[]>? DataAvailable;
    public event EventHandler<Exception>? RecordingStopped;

    public void StartRecording()
    {
        if (_isRecording)
            throw new InvalidOperationException("Already recording");

        _gcHandle = GCHandle.Alloc(this);

        var format = new AudioStreamBasicDescription
        {
            SampleRate = SampleRate,
            FormatID = kAudioFormatLinearPCM,
            FormatFlags = kAudioFormatFlagIsSignedInteger | kAudioFormatFlagIsPacked | kAudioFormatFlagsNativeEndian,
            BitsPerChannel = 16,
            ChannelsPerFrame = (uint)Channels,
            FramesPerPacket = 1,
            BytesPerFrame = (uint)(2 * Channels),
            BytesPerPacket = (uint)(2 * Channels)
        };

        var status = AudioQueueNewInput(ref format, AudioQueueCallback, GCHandle.ToIntPtr(_gcHandle),
            IntPtr.Zero, IntPtr.Zero, 0, out _queue);
        if (status != 0)
            throw new InvalidOperationException($"AudioQueueNewInput failed: {status}");

        const int bufferSize = 4096;
        for (int i = 0; i < _buffers.Length; i++)
        {
            AudioQueueAllocateBuffer(_queue, bufferSize, out _buffers[i]);
            AudioQueueEnqueueBuffer(_queue, _buffers[i], 0, IntPtr.Zero);
        }

        status = AudioQueueStart(_queue, IntPtr.Zero);
        if (status != 0)
            throw new InvalidOperationException($"AudioQueueStart failed: {status}");

        _isRecording = true;
        LoggerService.Info("macOS audio capture started (microphone mode, 44100Hz mono)");
    }

    public void StopRecording()
    {
        if (!_isRecording) return;

        _isRecording = false;

        if (_queue != IntPtr.Zero)
        {
            AudioQueueStop(_queue, true);
            AudioQueueDispose(_queue, true);
            _queue = IntPtr.Zero;
        }

        if (_gcHandle.IsAllocated)
            _gcHandle.Free();

        LoggerService.Info("macOS audio capture stopped");
        RecordingStopped?.Invoke(this, null!);
    }

    private static void AudioQueueCallback(IntPtr userData, IntPtr queue, IntPtr buffer,
        ref AudioTimeStamp startTime, uint numPacketDescs, IntPtr packetDescs)
    {
        var handle = GCHandle.FromIntPtr(userData);
        if (handle.Target is not MacAudioCapture capture) return;

        var buf = Marshal.PtrToStructure<AudioQueueBuffer>(buffer);
        if (buf.AudioDataBytes > 0)
        {
            var data = new byte[buf.AudioDataBytes];
            Marshal.Copy(buf.AudioData, data, 0, (int)buf.AudioDataBytes);
            capture.DataAvailable?.Invoke(capture, data);
        }

        if (capture._isRecording)
        {
            AudioQueueEnqueueBuffer(queue, buffer, 0, IntPtr.Zero);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_isRecording)
            StopRecording();
    }

    #region P/Invoke

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioStreamBasicDescription
    {
        public double SampleRate;
        public int FormatID;
        public int FormatFlags;
        public uint BytesPerPacket;
        public uint FramesPerPacket;
        public uint BytesPerFrame;
        public uint ChannelsPerFrame;
        public uint BitsPerChannel;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioTimeStamp
    {
        public double SampleTime;
        public long HostTime;
        public double RateScalar;
        public uint WordClockTime;
        public IntPtr SMPTETime;
        public uint Flags;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioQueueBuffer
    {
        public uint AudioDataBytesCapacity;
        public IntPtr AudioData;
        public uint AudioDataDescription;
        public uint AudioDataBytes;
        public IntPtr UserData;
    }

    private delegate void AudioQueueInputCallback(
        IntPtr userData, IntPtr queue, IntPtr buffer,
        ref AudioTimeStamp startTime, uint numPacketDescs, IntPtr packetDescs);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueNewInput(
        ref AudioStreamBasicDescription format,
        AudioQueueInputCallback callback,
        IntPtr userData,
        IntPtr callbackRunLoop,
        IntPtr callbackRunLoopMode,
        uint flags,
        out IntPtr queue);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueAllocateBuffer(IntPtr queue, uint bufferSize, out IntPtr buffer);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueEnqueueBuffer(IntPtr queue, IntPtr buffer, uint numPacketDescs, IntPtr packetDescs);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueStart(IntPtr queue, IntPtr startTime);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueStop(IntPtr queue, bool immediate);

    [DllImport("/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox")]
    private static extern int AudioQueueDispose(IntPtr queue, bool immediate);

    #endregion
}
#endif
