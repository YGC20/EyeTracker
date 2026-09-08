using DlibDotNet;

namespace LookAtCat.Vision
{
    public class FaceTracker
    {
        private DlibDotNet.Rectangle? _lastTrackedFace;

        private static double SquaredDistance(Rectangle a, Rectangle b)
        {
            double ax = (a.Left + a.Right) / 2.0;
            double ay = (a.Top + a.Bottom) / 2.0;

            double bx = (b.Left + b.Right) / 2.0;
            double by = (b.Top + b.Bottom) / 2.0;

            double dist = (ax - bx) * (ax - bx) + (ay - by) * (ay - by);
            return dist;
        }

        private static double Area(Rectangle rec)
        {
            return (rec.Right - rec.Left) * (rec.Bottom - rec.Top);
        }

        public Rectangle? SelectFace(Rectangle[] candidates)
        {
            if(candidates.Length == 0)
            {
                return null;
            }

            if(_lastTrackedFace is null)
            {
                Rectangle largeRec = candidates[0];
                double maxLR = Area(largeRec);
                for (int i=1; i<candidates.Length; ++i)
                {
                    double candR = Area(candidates[i]);
                    if (maxLR < candR)
                    {
                        largeRec = candidates[i];
                        maxLR = candR;
                    }
                }
                _lastTrackedFace = largeRec;
            }
            else
            {
                Rectangle nearestRec = candidates[0];
                double minDist = SquaredDistance(_lastTrackedFace.Value, nearestRec);
                for(int i=1; i<candidates.Length; ++i)
                {
                    double dist = SquaredDistance(_lastTrackedFace.Value, candidates[i]);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestRec = candidates[i];
                    }
                }
                _lastTrackedFace = nearestRec;
            }
            return _lastTrackedFace;
        }
    }
}
