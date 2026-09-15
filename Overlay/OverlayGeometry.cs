using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Shared screen geometry in physical pixels, using GUI's top-left origin.</summary>
    internal static class OverlayGeometry
    {
        private static readonly Vector3[] Corners = new Vector3[8];

        /// <summary>Selects the local player's camera, with main camera fallback during transitions.</summary>
        public static Camera GetCamera()
        {
            var body = UmbraMenu.LocalPlayerBody;
            foreach (var rig in CameraRigController.readOnlyInstancesList)
                if (rig && body && rig.target == body.gameObject && rig.sceneCam) return rig.sceneCam;
            return Camera.main;
        }

        /// <summary>Returns the camera viewport center in GUI coordinates.</summary>
        public static Vector2 Center(Camera camera)
        {
            var viewport = camera.pixelRect;
            return new Vector2(viewport.center.x, Screen.height - viewport.center.y);
        }

        /// <summary>Computes the exact radius shared by the guide and target selection.</summary>
        public static float FovRadius(Camera camera)
        {
            return ProjectionMath.FovRadius(camera.pixelRect.height, camera.fieldOfView, ModernAimbot.FieldOfView);
        }

        /// <summary>Projects only points ahead of the near plane and within the viewport.</summary>
        public static bool ProjectPoint(Camera camera, Vector3 world, out Vector2 point)
        {
            Vector3 screen = camera.WorldToScreenPoint(world);
            point = new Vector2(screen.x, Screen.height - screen.y);
            return screen.z >= camera.nearClipPlane && camera.pixelRect.Contains(new Vector2(screen.x, screen.y));
        }

        /// <summary>Tests the final aim point against the displayed circular boundary.</summary>
        public static bool InsideFov(Camera camera, Vector3 world, out Vector2 screen)
        {
            if (!ProjectPoint(camera, world, out screen)) return false;
            Vector2 center = Center(camera);
            return ProjectionMath.InsideDisk(screen.x, screen.y, center.x, center.y, FovRadius(camera));
        }

        /// <summary>Projects AABB corners and near-plane intersections; clamps the resulting box to the viewport.</summary>
        public static bool ProjectBounds(Camera camera, Bounds bounds, out Rect rect)
        {
            rect = default(Rect);
            if (bounds.Contains(camera.transform.position)) return false;
            Vector3 min = bounds.min, max = bounds.max;
            float near = camera.nearClipPlane + 0.001f;
            Vector2 lo = new Vector2(float.MaxValue, float.MaxValue), hi = new Vector2(float.MinValue, float.MinValue);
            bool found = false;
            for (int i = 0; i < 8; i++)
            {
                Corners[i] = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                if (Vector3.Dot(Corners[i] - camera.transform.position, camera.transform.forward) >= near)
                    Accumulate(camera, Corners[i], ref lo, ref hi, ref found);
            }
            // Each cube edge differs by one bit. Clipping prevents inverted boxes at the camera plane.
            for (int i = 0; i < 8; i++)
                for (int bit = 1; bit <= 4; bit <<= 1)
                {
                    if ((i & bit) != 0) continue;
                    Vector3 a = Corners[i], b = Corners[i | bit];
                    float za = Vector3.Dot(a - camera.transform.position, camera.transform.forward);
                    float zb = Vector3.Dot(b - camera.transform.position, camera.transform.forward);
                    if ((za >= near) == (zb >= near)) continue;
                    Accumulate(camera, Vector3.Lerp(a, b, (near - za) / (zb - za)), ref lo, ref hi, ref found);
                }
            if (!found) return false;
            Rect viewport = camera.pixelRect;
            float top = Screen.height - viewport.yMax, bottom = Screen.height - viewport.yMin;
            if (hi.x < viewport.xMin || lo.x > viewport.xMax || hi.y < top || lo.y > bottom) return false;
            rect = Rect.MinMaxRect(Mathf.Max(lo.x, viewport.xMin), Mathf.Max(lo.y, top), Mathf.Min(hi.x, viewport.xMax), Mathf.Min(hi.y, bottom));
            return rect.width > 0.5f && rect.height > 0.5f;
        }

        /// <summary>Accumulates a finite projected corner or clipping intersection.</summary>
        private static void Accumulate(Camera camera, Vector3 world, ref Vector2 min, ref Vector2 max, ref bool found)
        {
            Vector3 p = camera.WorldToScreenPoint(world);
            if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x) || float.IsInfinity(p.y)) return;
            var xy = new Vector2(p.x, Screen.height - p.y);
            min = Vector2.Min(min, xy); max = Vector2.Max(max, xy); found = true;
        }
    }
}
