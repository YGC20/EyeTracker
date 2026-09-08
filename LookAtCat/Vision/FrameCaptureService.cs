using System.Windows.Media.Imaging;
using System.Diagnostics.CodeAnalysis;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace LookAtCat.Vision
{
    public sealed class FrameCaptureService : IDisposable
    {
        private readonly object _frameLock = new();
        private BitmapSource? _latestFrame;

        private readonly object _matLock = new();
        private Mat? _latestMat;

        public double Gamma { get; set; } = 1.8;
        private double _lastBuiltGamma;
        private Mat? _gammaLut;


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
            TryGetLatestFrame([NotNullWhen(true)] out BitmapSource? frame)
        {
            lock (_frameLock)
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

        private static Mat BuildGammaLut(double gamma)
        {
            Mat lut = new Mat(1, 256, MatType.CV_8UC1);
            for (int i = 0; i < 256; ++i)
            {
                double output = 255 * Math.Pow((i / 255.0), (1.0 / gamma));
                lut.Set<byte>(0, i, (byte)Math.Clamp(output, 0, 255));
            }
            return lut;
        }
        private void ApplyGammaCorrection(Mat mat)
        {
            if (Gamma <= 0 || Math.Abs(Gamma-1.0) < 1e-6)
            {
                return;
            }

            if(_gammaLut is null || Math.Abs(_lastBuiltGamma - Gamma) > 1e-6)
            {
                _gammaLut?.Dispose();
                _gammaLut = BuildGammaLut(Gamma);
                _lastBuiltGamma = Gamma;
            }

            Cv2.LUT(mat, _gammaLut, mat);
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

                    ApplyGammaCorrection(mat);

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

                lock (_matLock)
                {
                    _latestMat?.Dispose();
                    _latestMat = null;
                }

                _gammaLut?.Dispose();
                _gammaLut = null;
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
