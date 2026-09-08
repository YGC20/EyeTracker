using OpenCvSharp;

namespace LookAtCat.Vision
{
    public class PositionSmoother
    {
        private Point? _smoothed;
        private readonly double _alpha;

        public PositionSmoother(double alpha)
        {
            _alpha = alpha;
        }

        public Point? Smooth(Point? newValue)
        {
            if(newValue is null)
            {
                return _smoothed;
            }

            if(_smoothed is null)
            {
                _smoothed = newValue.Value;
            }
            else
            {
                double smoothedX = _alpha * newValue.Value.X + (1 - _alpha) * _smoothed.Value.X;
                double smoothedY = _alpha * newValue.Value.Y + (1 - _alpha) * _smoothed.Value.Y;
                _smoothed = new Point((int)Math.Round(smoothedX), (int)Math.Round(smoothedY));
            }
            return _smoothed;
        }

        public void Reset()
        {
            _smoothed = null;
        }
    }
}
