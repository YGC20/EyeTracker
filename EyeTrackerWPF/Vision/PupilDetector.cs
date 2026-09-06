using OpenCvSharp;

namespace EyeTrackerWPF.Vision
{
    public class PupilDetector
    {
        public static Point? DetectPupil(Mat grayFrame, DlibDotNet.Point[] eyePoints)
        {
            // 바운딩 박스 + 패딩
            int minX = eyePoints[0].X, maxX = eyePoints[0].X; 
            int minY = eyePoints[0].Y, maxY = eyePoints[0].Y;

            for(int i=1; i<eyePoints.Length; ++i)
            {
                int eyeX = eyePoints[i].X, eyeY = eyePoints[i].Y;
                if (eyeX < minX) { minX = eyeX; }
                if (eyeX > maxX) { maxX = eyeX; }
                if (eyeY < minY) { minY = eyeY; }
                if (eyeY > maxY) { maxY = eyeY; }
            }
            int paddedMinX = minX - (int)((maxX - minX) * 0.2);
            int paddedMaxX = maxX + (int)((maxX - minX) * 0.2);
            int paddedMinY = minY - (int)((maxY - minY) * 0.2);
            int paddedMaxY = maxY + (int)((maxY - minY) * 0.2);

            // 클램핑
            paddedMinX = Math.Max(0, paddedMinX);
            paddedMaxX = Math.Min(grayFrame.Width - 1, paddedMaxX);
            paddedMinY = Math.Max(0, paddedMinY);
            paddedMaxY = Math.Min(grayFrame.Height - 1, paddedMaxY);

            // 서브맷 자르기
            int roiWidth = paddedMaxX - paddedMinX;
            int roiHeight = paddedMaxY - paddedMinY;

            Rect roi = new Rect(paddedMinX, paddedMinY, roiWidth, roiHeight);
            using Mat eyeRegion = new Mat(grayFrame, roi);

            // Otsu 이진화
            using Mat binary = new Mat();
            Cv2.Threshold(eyeRegion, binary, 0, 255, 
                ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            // 컨투어 찾기 + 최적 컨투어 선택
            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(binary, out contours, out hierarchy, 
                RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if(contours.Length == 0)
            {
                return null;
            }
            Point[] bestContour = contours[0];
            double maxArea = Cv2.ContourArea(bestContour);
            for(int i=1; i<contours.Length; ++i)
            {
                double area = Cv2.ContourArea(contours[i]);
                if (maxArea < area)
                {
                    maxArea = area;
                    bestContour = contours[i];
                }
            }

            // 무게중심 계산 + 좌표 역변환
            Moments m = Cv2.Moments(bestContour);
            if(m.M00 == 0)
            {
                return null;
            }
            int localX = (int)(m.M10 / m.M00);
            int localY = (int)(m.M01 / m.M00);

            int globalX = localX + paddedMinX;
            int globalY = localY + paddedMinY;

            return new Point(globalX, globalY);
        }
    }
}
