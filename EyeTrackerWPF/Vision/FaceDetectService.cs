using DlibDotNet;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace EyeTrackerWPF.Vision
{
    public sealed class FaceDetectService : IDisposable
    {
        private readonly object _resultLock = new();
        private FaceTrackingResult? _latestResult;
        private Thread? _detectThread;
        private CancellationTokenSource? _cts;
        private readonly FrameCaptureService _frameSource;
        private readonly FaceTracker _faceTracker;

        private readonly PositionSmoother _leftSmoother = new(0.3);
        private readonly PositionSmoother _rightSmoother = new(0.3);

        public FaceDetectService(FrameCaptureService frameSource)
        {
            _frameSource = frameSource;
            _faceTracker = new FaceTracker();
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
            TryGetLatestTrackingResult([NotNullWhen(true)]out FaceTrackingResult? result)
        {
            lock(_resultLock)
            {
                result = _latestResult;
                return result is not null;
            }
        }

        public event Action<Exception>? DetectError;
        private void DetectLoop(FrameCaptureService frameSource, CancellationToken token)
        {
            try
            {
                using var detector = Dlib.GetFrontalFaceDetector();
                using var shapePredictor = 
                    ShapePredictor.Deserialize(Path.Combine(
                        AppContext.BaseDirectory,"Models","shape_predictor_68_face_landmarks.dat"));
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
                        using var img = Dlib.LoadImageData<BgrPixel>(
                            pixelData, (uint)mat.Rows, (uint)mat.Cols, (uint)mat.Step());
                        var faces = detector.Operator(img);

                        var selectedFace = _faceTracker.SelectFace(faces);
                        FaceTrackingResult? result = null;
                        if(selectedFace is not null)
                        {
                            var shape = shapePredictor.Detect(img, selectedFace.Value);
                            using var gray = new OpenCvSharp.Mat();
                            OpenCvSharp.Cv2.CvtColor(mat, gray,
                                OpenCvSharp.ColorConversionCodes.BGR2GRAY);
                            
                            Point[] leftEye = new Point[6];
                            for(int i=36; i<=41; ++i)
                            {
                                leftEye[i - 36] = shape.GetPart((uint)i);
                            }
                            var leftPupil = _leftSmoother.Smooth(PupilDetector.DetectPupil(gray, leftEye));

                            Point[] rightEye = new Point[6];
                            for(int i=42; i<=47; ++i)
                            {
                                rightEye[i - 42] = shape.GetPart((uint)i);
                            }
                            var rightPupil = _rightSmoother.Smooth(PupilDetector.DetectPupil(gray, rightEye));
                            
                            result = new FaceTrackingResult(selectedFace.Value, leftEye, rightEye, leftPupil, rightPupil);
                        }
                        else
                        {
                            _leftSmoother.Reset();
                            _rightSmoother.Reset();
                        }

                        lock (_resultLock)
                        {
                            _latestResult = result;
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
