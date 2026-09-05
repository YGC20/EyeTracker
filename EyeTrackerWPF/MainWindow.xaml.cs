using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using EyeTrackerWPF.Vision;

namespace EyeTrackerWPF
{
    public partial class MainWindow : Window
    {
        private bool _hasError;
        private readonly FrameCaptureService _captureService;
        private readonly FaceDetectService _detectService;
        private readonly CharacterStateMachine _stateMachine;

        public MainWindow()
        {
            InitializeComponent();
            _captureService = new FrameCaptureService();
            _detectService = new FaceDetectService(_captureService);
            _stateMachine = new CharacterStateMachine();
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _captureService.CaptureError += OnCaptureError;
            _captureService.Start();
            _detectService.DetectError += OnDetectError;
            _detectService.Start();
            CompositionTarget.Rendering += OnRendering;
        }
        private void OnRendering(object? sender, EventArgs e)
        {
            if(_captureService.TryGetLatestFrame(out var frame))
            {
                CameraView.Source = frame;
                if(double.IsNaN(OverlayCanvas.Width))
                {
                    OverlayCanvas.Width = frame.PixelWidth;
                    OverlayCanvas.Height = frame.PixelHeight;
                }
                if (!_hasError)
                {
                    StatusText.Visibility = Visibility.Collapsed;
                }
            }

            if(_detectService.TryGetLatestTrackingResult(out var result))
            {
                FaceBox.Visibility = Visibility.Visible;

                if(result.LeftPupil is not null)
                {
                    LeftPupilMark.Visibility = Visibility.Visible;
                    Canvas.SetLeft(LeftPupilMark, result.LeftPupil.Value.X - LeftPupilMark.Width / 2);
                    Canvas.SetTop(LeftPupilMark, result.LeftPupil.Value.Y - LeftPupilMark.Height / 2);
                }
                else
                {
                    LeftPupilMark.Visibility = Visibility.Collapsed;
                }

                if(result.RightPupil is not null)
                {
                    RightPupilMark.Visibility = Visibility.Visible;
                    Canvas.SetLeft(RightPupilMark, result.RightPupil.Value.X - RightPupilMark.Width / 2);
                    Canvas.SetTop(RightPupilMark, result.RightPupil.Value.Y - RightPupilMark.Height / 2);
                }
                else
                {
                    RightPupilMark.Visibility = Visibility.Collapsed;
                }
                

                Canvas.SetLeft(FaceBox, result.Face.Left);
                Canvas.SetTop(FaceBox, result.Face.Top);

                FaceBox.Width = result.Face.Right - result.Face.Left;
                FaceBox.Height = result.Face.Bottom - result.Face.Top;
            } else
            {
                FaceBox.Visibility = Visibility.Collapsed;
                LeftPupilMark.Visibility = Visibility.Collapsed;
                RightPupilMark.Visibility = Visibility.Collapsed;
            }

            var gaze = _stateMachine.Update(result, (int)OverlayCanvas.Width, (int)OverlayCanvas.Height);
            double dx = gaze.Dx;
            double dy = gaze.Dy;
            PositionPupil(LeftEyeWhite, LeftEyePupil, dx, dy);
            PositionPupil(RightEyeWhite, RightEyePupil, dx, dy);
        }
        private void OnCaptureError(Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                _hasError = true;
                StatusText.Text = $"카메라 오류: {ex.Message}";
                StatusText.Visibility = Visibility.Visible;
            });
        }
        private void OnDetectError(Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                _hasError = true;
                StatusText.Text = $"탐지 오류: {ex.Message}";
                StatusText.Visibility = Visibility.Visible;
            });
        }
        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            _detectService.DetectError -= OnDetectError;
            _detectService.Dispose();
            _captureService.CaptureError -= OnCaptureError;
            _captureService.Dispose();
        }

        private static void PositionPupil(Ellipse eyeWhite, Ellipse pupil, double dx, double dy)
        {
            double rangeX = (eyeWhite.Width - pupil.Width) / 2.0;
            double rangeY = (eyeWhite.Height - pupil.Height) / 2.0;

            double eyeLeft = Canvas.GetLeft(eyeWhite);
            double eyeTop = Canvas.GetTop(eyeWhite);

            double pupilLeft = eyeLeft + eyeWhite.Width / 2.0 - pupil.Width / 2.0 + dx * rangeX;
            double pupilTop = eyeTop + eyeWhite.Height / 2.0 - pupil.Height / 2.0 + dy * rangeY;

            Canvas.SetLeft(pupil, pupilLeft);
            Canvas.SetTop(pupil, pupilTop);
        }
    }
}