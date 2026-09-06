namespace EyeTrackerWPF.Vision
{
    public class GazeDirection
    {
        // 눈 위치 계산
        private static (double Dx, double Dy)? 
            ComputeEyeDirection(DlibDotNet.Point[] eyeLandmarks, OpenCvSharp.Point? pupil)
        {
            if(pupil is null)
            {
                return null;
            }

            int minX = eyeLandmarks[0].X, maxX = eyeLandmarks[0].X;
            int minY = eyeLandmarks[0].Y, maxY = eyeLandmarks[0].Y;

            for (int i = 1; i < eyeLandmarks.Length; ++i)
            {
                int eyeX = eyeLandmarks[i].X, eyeY = eyeLandmarks[i].Y;
                if (eyeX < minX) { minX = eyeX; }
                if (eyeX > maxX) { maxX = eyeX; }
                if (eyeY < minY) { minY = eyeY; }
                if (eyeY > maxY) { maxY = eyeY; }
            }

            double eyeCenterX = (minX + maxX) / 2.0;
            double eyeCenterY = (minY + maxY) / 2.0;
            
            double eyeHalfWidth = (maxX - minX) / 2.0;
            double eyeHalfHeight = (maxY - minY) / 2.0;
            if(eyeHalfWidth == 0 || eyeHalfHeight == 0)
            {
                return null;
            }

            double dx = Math.Clamp(((pupil.Value.X - eyeCenterX) / eyeHalfWidth), -1, 1);
            double dy = Math.Clamp(((pupil.Value.Y - eyeCenterY) / eyeHalfHeight), -1, 1);
            return (dx, dy);
        }

        public static (double Dx, double Dy)? 
            Compute(FaceTrackingResult? result, int frameWidth, int frameHeight)
        {
            if(result is null)
            {
                return null;
            }

            /*
            // 좌우 눈 방향 계산
            var left = ComputeEyeDirection(result.LeftEye, result.LeftPupil);
            var right = ComputeEyeDirection(result.RightEye, result.RightPupil);

            if(left is null && right is null)
            {
                double faceCenterX = (result.Face.Left + result.Face.Right) / 2.0;
                double faceCenterY = (result.Face.Top + result.Face.Bottom) / 2.0;

                double dx = Math.Clamp(((faceCenterX - frameWidth / 2.0) / (frameWidth / 2.0)), -1, 1);
                double dy = Math.Clamp(((faceCenterY - frameHeight / 2.0) / (frameHeight / 2.0)), -1, 1);

                return (dx, dy);
            }
            else if(left is not null && right is not null)
            {
                double dx = (left.Value.Dx + right.Value.Dx) / 2.0;
                double dy = (left.Value.Dy + right.Value.Dy) / 2.0;
                return (dx, dy);
            }
            else
            {
                return left ?? right;
            }
            */

            // 얼굴 기반 위치
            double faceCenterX = (result.Face.Left + result.Face.Right) / 2.0;
            double faceCenterY = (result.Face.Top + result.Face.Bottom) / 2.0;

            double dx = Math.Clamp(((faceCenterX - frameWidth / 2.0) / (frameWidth / 2.0)), -1, 1);
            double dy = Math.Clamp(((faceCenterY - frameHeight / 2.0) / (frameHeight / 2.0)), -1, 1);

            return (dx, dy);
        }
    }
}
