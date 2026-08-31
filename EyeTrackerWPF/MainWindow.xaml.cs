using System.Windows;
using System.Windows.Media;
using EyeTrackerWPF.Vision;

namespace EyeTrackerWPF
{
    public partial class MainWindow : Window
    {
        private readonly FrameCaptureService _captureService = new();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closing += OnClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _captureService.CaptureError += OnCaptureError;
            _captureService.Start();
            CompositionTarget.Rendering += OnRendering;
        }
        private void OnRendering(object? sender, EventArgs e)
        {
            if(_captureService.TryGetLatestFrame(out var frame))
            {
                CameraView.Source = frame;
                StatusText.Visibility = Visibility.Collapsed;
            }
        }
        private void OnCaptureError(Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"카메라 오류: {ex.Message}";
                StatusText.Visibility = Visibility.Visible;
            });
        }
        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            _captureService.CaptureError -= OnCaptureError;
            _captureService.Dispose();
        }
    }
}