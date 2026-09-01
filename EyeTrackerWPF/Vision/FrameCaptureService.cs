using System.Windows.Media.Imaging;
using System.Diagnostics.CodeAnalysis;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace EyeTrackerWPF.Vision
{
    public sealed class FrameCaptureService : IDisposable
    {
        private readonly object _frameLock = new();
        private readonly object _matLock = new();
        private BitmapSource? _latestFrame;
        private Mat? _latestMat;
        private Thread? _captureThread;
        private CancellationTokenSource? _cts;
        
        public bool IsRunning => _captureThread is { IsAlive: true };
        public void Start(int cameraIndex = 0)
        {
            if (IsRunning) return;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _captureThread = new Thread(() => CaptureLoop(cameraIndex, token))
            {
                IsBackground = true,
                Name = "FrameCaptureThread",
            };
            _captureThread.Start();
        }
        public void Stop()
        {
            _cts?.Cancel();
            _captureThread?.Join(TimeSpan.FromSeconds(2));
            _captureThread = null;
        }

        public bool 
            TryGetLatestFrame([NotNullWhen(true)]out BitmapSource? frame)
        {
            lock(_frameLock)
            {
                frame = _latestFrame;
                return frame is not null;
            }
        }
        public bool 
            TryGetLatestMat([NotNullWhen(true)] out Mat? mat)
        {
            lock (_matLock)
            {
                mat = _latestMat?.Clone();
                return mat is not null;
            }
        }

        public event Action<Exception>? CaptureError;
        private void CaptureLoop(int cameraIndex, CancellationToken token)
        {
            VideoCapture? capture = null;
            try
            {
                capture = new VideoCapture(cameraIndex);
                if (!capture.IsOpened())
                {
                    throw new InvalidOperationException($"카메라(index={cameraIndex}를 열 수 없습니다.");
                }
                using var mat = new Mat();
                while (!token.IsCancellationRequested)
                {
                    if (!capture.Read(mat) || mat.Empty())
                    {
                        continue;
                    }

                    lock (_matLock)
                    {
                        _latestMat?.Dispose();
                        _latestMat = mat.Clone();
                    }

                    var bitmap = mat.ToBitmapSource();
                    bitmap.Freeze();

                    lock (_frameLock)
                    {
                        _latestFrame = bitmap;
                    }
                }
            }
            catch (Exception ex)
            {
                CaptureError?.Invoke(ex);
            }
            finally
            {
                capture?.Release();
                capture?.Dispose();
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
