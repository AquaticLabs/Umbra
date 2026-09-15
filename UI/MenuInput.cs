using UnityEngine;
using UnityEngine.UI;

namespace UmbraMenu
{
    /// <summary>Transparent uGUI raycast shield and a gesture latch scoped to the Umbra rectangle.</summary>
    internal static class MenuInput
    {
        private static GameObject blocker;
        private static RectTransform rect;
        private static bool dragging;
        public static bool KeyboardFocused;
        /// <summary>Blocks the entire press/release gesture if it began inside Umbra, but not clicks beside it.</summary>
        public static bool Capturing
        {
            get
            {
                bool held = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
                bool released = Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2);
                if (!held && !released) dragging = false;
                Vector3 mouse = Input.mousePosition;
                bool inside = MenuController.IsOpen && MenuController.WindowRect.Contains(new Vector2(mouse.x, Screen.height - mouse.y));
                if (!inside && Input.GetMouseButtonDown(0)) KeyboardFocused = false;
                if (inside && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))) dragging = true;
                return inside || dragging || KeyBindings.IsCapturing || (MenuController.IsOpen && KeyboardFocused);
            }
        }
        /// <summary>Matches the transparent raycast shield to the visible window, leaving the rest of the screen untouched.</summary>
        public static void Update()
        {
            bool capture = Capturing;
            if (!blocker && MenuController.IsOpen)
            {
                blocker = new GameObject("Umbra pointer shield", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                Object.DontDestroyOnLoad(blocker);
                var canvas = blocker.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32760;
                var surface = new GameObject("Window bounds", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                surface.transform.SetParent(blocker.transform, false);
                rect = surface.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                var image = surface.GetComponent<Image>(); image.color = Color.clear; image.raycastTarget = true;
                surface.GetComponent<CanvasRenderer>().cullTransparentMesh = false;
            }
            if (!blocker) return;
            blocker.SetActive(MenuController.IsOpen || capture);
            if (rect)
            {
                Rect bounds = MenuController.WindowRect;
                rect.anchoredPosition = new Vector2(bounds.x, -bounds.y);
                rect.sizeDelta = bounds.size;
            }
        }
        /// <summary>Removes the raycast surface and any in-progress gesture state on unload.</summary>
        public static void Dispose() { if (blocker) Object.Destroy(blocker); blocker = null; rect = null; dragging = false; KeyboardFocused = false; }
    }
}
