using DlibDotNet;

namespace LookAtCat.Vision
{
    public sealed record FaceTrackingResult(
        Rectangle Face, 
        Point[] LeftEye, Point[] RightEye,
        OpenCvSharp.Point? LeftPupil,
        OpenCvSharp.Point? RightPupil);
}
