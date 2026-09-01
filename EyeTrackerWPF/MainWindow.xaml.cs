using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EyeTrackerWPF.Vision;

namespace EyeTrackerWPF
{
    public partial class MainWindow : Window
    {
        private bool _hasError;
        private readonly FrameCaptureService _captureService;
        private readonly FaceDetectService _detectService;

        public MainWindow()
        {
            InitializeComponent();
            _captureService = new FrameCaptureService();
            _detectService = new FaceDetectService(_captureService);
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

            if(_detectService.TryGetLatestFace(out var face))
            {
                FaceBox.Visibility = Visibility.Visible;
                Canvas.SetLeft(FaceBox, face.Value.Left);
                Canvas.SetTop(FaceBox, face.Value.Top);
                FaceBox.Width = face.Value.Right - face.Value.Left;
                FaceBox.Height = face.Value.Bottom - face.Value.Top;
            } else
            {
                FaceBox.Visibility = Visibility.Collapsed;
            }
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
    }
}