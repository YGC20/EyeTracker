namespace EyeTrackerWPF.Vision
{
    public class CharacterStateMachine
    {
        private enum State { Tracking, HoldLast, RandomWalk };

        private State _state = State.RandomWalk;
        private (double Dx, double Dy) _lastDirection = (0, 0);
        private DateTime? _lostSince;
        private readonly Random _random = new();

        private const double GracePeriodSeconds = 1.5;
        private const double SmoothingAlpha = 0.05;
        private (double Dx, double Dy) _randomTarget = (0, 0);
        private DateTime _nextTargetTime = DateTime.MinValue;
        private const double RandomWalkIntervalSeconds = 1.5;

        public (double Dx, double Dy) Update(FaceTrackingResult? result, int frameWidth, int frameHeight)
        {
            if(result is not null)
            {
                _state = State.Tracking;
                _lostSince = null;
            }
            else if(_state == State.Tracking)
            {
                _state = State.HoldLast;
                _lostSince = DateTime.UtcNow;
            }
            else if (_state == State.HoldLast && 
                (DateTime.UtcNow - _lostSince!.Value).TotalSeconds >= GracePeriodSeconds)
            {
                _state = State.RandomWalk;
            }

            (double Dx, double Dy) target;
            switch (_state)
            {
                case State.Tracking:
                    target = GazeDirection.Compute(result, frameWidth, frameHeight)!.Value;
                    break;
                case State.HoldLast:
                    target = _lastDirection;
                    break;
                default:
                    if (DateTime.UtcNow >= _nextTargetTime)
                    {
                        _randomTarget = (_random.NextDouble() * 2 - 1, _random.NextDouble() * 2 - 1);
                        _nextTargetTime = DateTime.UtcNow.AddSeconds(RandomWalkIntervalSeconds);
                    }
                    target = _randomTarget;
                    break;
            }

            _lastDirection = (SmoothingAlpha * target.Dx + (1 - SmoothingAlpha) * _lastDirection.Dx,
                SmoothingAlpha * target.Dy + (1 - SmoothingAlpha) * _lastDirection.Dy);

            return _lastDirection;
        }
    }
}
