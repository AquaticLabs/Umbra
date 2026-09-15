using System;

namespace UmbraMenu
{
    /// <summary>Pure projection arithmetic shared by targeting, drawing, and regression tests.</summary>
    internal static class ProjectionMath
    {
        /// <summary>Converts a camera-centered half angle into pixels using the camera's vertical FOV.</summary>
        public static float FovRadius(float viewportHeight, float cameraFov, float halfAngle)
        {
            double radians = Math.PI / 180.0;
            return (float)(viewportHeight * 0.5 * Math.Tan(halfAngle * radians) / Math.Tan(cameraFov * 0.5 * radians));
        }

        /// <summary>Accepts only points inside or on the visible disk; rejects invalid projections.</summary>
        public static bool InsideDisk(float x, float y, float centerX, float centerY, float radius)
        {
            if (float.IsNaN(x) || float.IsNaN(y) || float.IsInfinity(x) || float.IsInfinity(y) || radius <= 0) return false;
            double dx = x - centerX, dy = y - centerY;
            return dx * dx + dy * dy <= (double)radius * radius;
        }
    }
}
