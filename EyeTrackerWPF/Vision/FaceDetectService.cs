using DlibDotNet;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace EyeTrackerWPF.Vision
{
    public sealed class FaceDetectService : IDisposable
    {
        private readonly object _resultLock = new();
        private Rectangle? _latestResult;
        private Thread? _detectThread;
        private CancellationTokenSource? _cts;
        private readonly FrameCaptureService _frameSource;

        public FaceDetectService(FrameCaptureService frameSource)
        {
            _frameSource = frameSource;
        }

        public bool IsRunning => _detectThread is { IsAlive: true };
        public void Start()
        {
            if (IsRunning) return;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _detectThread = new Thread(() => DetectLoop(_frameSource, token))
            {
                IsBackground = true,
                Name = "FaceDetectThread",
            };
            _detectThread.Start();
        }
        public void Stop()
        {
            _cts?.Cancel();
            _detectThread?.Join(TimeSpan.FromSeconds(2));
            _detectThread = null;
        }

        public bool 
            TryGetLatestFace([NotNullWhen(true)]out Rectangle? face)
        {
            lock(_resultLock)
            {
                face = _latestResult;
                return face is not null;
            }
        }

        public event Action<Exception>? DetectError;
        private void DetectLoop(FrameCaptureService frameSource, CancellationToken token)
        {
            try
            {
                using var detector = Dlib.GetFrontalFaceDetector();
                while (!token.IsCancellationRequested)
                {
                    if (!frameSource.TryGetLatestMat(out var mat))
                    {
                        Thread.Sleep(30);
                        continue;
                    }

                    using (mat)
                    {
                        int byteLenght = mat.Rows * (int)mat.Step();
                        var pixelData = new byte[byteLenght];
                        Marshal.Copy(mat.Data, pixelData, 0, byteLenght);
                        using var img = Dlib.LoadImageData<BgrPixel>(pixelData, (uint)mat.Rows, (uint)mat.Cols, (uint)mat.Step());
                        var faces = detector.Operator(img);

                        lock (_resultLock)
                        {
                            _latestResult = faces.Length > 0 ? faces[0] : (Rectangle?)null;
                        }
                    }

                    Thread.Sleep(30);
                }
            }
            catch (Exception ex)
            {
                DetectError?.Invoke(ex);
            }
            
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
