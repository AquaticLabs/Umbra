using static UmbraMenu.MenuTheme;
using static UmbraMenu.MenuActions;
using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Measured two-column shell. Pages describe cards; controls measure before drawing.</summary>
    internal static class MenuController
    {
        private static IMenuPage[] Pages = CreatePages();
        private static readonly Queue<Action> layoutChanges = new Queue<Action>();
        /// <summary>Explicit page registry; each instance owns its fields and selection state.</summary>
        private static IMenuPage[] CreatePages() { return new IMenuPage[] { new PlayerPage(), new AimbotPage(), new VisualsPage(), new ItemsPage(), new WorldPage(), new SpawnPage(), new LobbyPage(), new MiscPage() }; }
        private static Rect window = new Rect(40f, 40f, 950f, 750f);
        internal static bool HasHost { get { return UnityEngine.Networking.NetworkServer.active; } }
        internal static Rect WindowRect { get { return window; } }
        private static Vector2 pageScroll;
        private static int page;
        private static string status = "Ready";
        private static float statusUntil;





        private static readonly List<CardDefinition> cards = new List<CardDefinition>();
        internal static VisualPreferences Prefs { get { return VisualSettings.Current; } }
        public static bool IsOpen { get; private set; }
        private static bool cursorVisible;
        private static CursorLockMode cursorLock;

        #region Shell and lifecycle

        /// <summary>Restores window position without changing the user's page layouts or dimensions.</summary>
        public static void Initialize()
        {
            window.x = Prefs.WindowX; window.y = Prefs.WindowY;
            Pages = CreatePages(); layoutChanges.Clear(); page = 0; pageScroll = Vector2.zero;
            status = "Ready"; statusUntil = 0;
        }

        /// <summary>Opens/closes the shell, preserving cursor state and saving visual preferences on close.</summary>
        public static void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            if (open) { cursorVisible = Cursor.visible; cursorLock = Cursor.lockState; }
            IsOpen = open;
            if (open) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else
            {
                MenuWidgets.CloseColorPicker(true);
                KeyBindings.CancelCapture();
                MenuInput.KeyboardFocused = false;
                Cursor.lockState = cursorLock; Cursor.visible = cursorVisible;
                SafeAction(VisualSettings.Save, "Save visual preferences");
            }
        }

        /// <summary>Routes former menu shortcuts to an actual visible page.</summary>
        public static void OpenPage(int index) { page = Mathf.Clamp(index, 0, Pages.Length - 1); pageScroll = Vector2.zero; SetOpen(true); }

        /// <summary>Draws at native pixel scale; short screens scroll and narrow screens use one column.</summary>
        public static void Draw()
        {
            while (layoutChanges.Count > 0) SafeAction(layoutChanges.Dequeue(), "Update menu");
            EnsureStyles();
            if (!IsOpen) return;
            window.width = Mathf.Min(950f, Mathf.Max(300f, Screen.width - 24f));
            window.height = Mathf.Min(750f, Mathf.Max(240f, Screen.height - 24f));
            window.x = Mathf.Clamp(window.x, 0, Mathf.Max(0, Screen.width - window.width));
            window.y = Mathf.Clamp(window.y, 0, Mathf.Max(0, Screen.height - window.height));
            GUI.depth = -100;
            if (Event.current.type == EventType.MouseDown && !window.Contains(Event.current.mousePosition))
            { GUI.FocusControl(null); MenuInput.KeyboardFocused = false; }
            window = GUI.Window(7026, window, DrawWindow, string.Empty, GUIStyle.none);
            Prefs.WindowX = window.x; Prefs.WindowY = window.y;
            MenuInput.KeyboardFocused = GUI.GetNameOfFocusedControl().StartsWith("Umbra:", StringComparison.Ordinal);
        }

        /// <summary>Builds and measures the selected page before drawing its correctly sized scroll area.</summary>
        private static void DrawWindow(int id)
        {
            float sidebarWidth = window.width < 700f ? 116f : 178f;
            Panel(new Rect(0, 0, window.width, window.height), windowStyle, Prefs.WindowOpacity);
            Panel(new Rect(1, 1, window.width - 2, 64), headerStyle, 0.18f);
            Panel(new Rect(1, 65, sidebarWidth - 1, window.height - 66), sidebarStyle, 0.16f);
            GUI.Label(new Rect(22, 12, 180, 28), "UMBRA", titleStyle);
            GUI.Label(new Rect(23, 40, 190, 20), "TRIDENT / " + UmbraRuntime.VERSION, subtitleStyle);
            string state = UmbraRuntime.characterCollected ? "IN RUN" : "NO CHARACTER";
            GUI.Label(new Rect(window.width - 170, 18, 140, 28), state, badgeStyle);
            if (HasHost) GUI.Label(new Rect(window.width - 260, 18, 90, 28), "HOST", badgeStyle);
            float notice = HasHost ? 0 : 38;
            if (!HasHost)
            {
                ESPHelper.Fill(new Rect(1, 65, window.width - 2, 36), new Color(0.65f, 0.25f, 0.3f, 0.27f));
                GUI.Label(new Rect(16, 72, window.width - 32, 28), "Not hosting — host-only mods are disabled. Local aim, movement and visuals remain available.", mutedStyle);
            }

            bool contentEnabled = GUI.enabled;
            if (MenuWidgets.ColorPickerOpen) GUI.enabled = false;
            DrawNavigation(sidebarWidth);
            float contentX = sidebarWidth + 20f;
            float availableWidth = window.width - contentX - 20f;
            GUI.Label(new Rect(contentX, 81 + notice, availableWidth, 30), Pages[page].Title, sectionStyle);
            GUI.Label(new Rect(contentX, 116 + notice, availableWidth, 36), Pages[page].Description, mutedStyle);

            cards.Clear();
            Pages[page].Build(availableWidth);
            Rect viewport = new Rect(contentX, 158 + notice, availableWidth, Mathf.Max(30f, window.height - 206f - notice));
            float contentWidth = Mathf.Max(130, viewport.width - 18f);
            float totalHeight = LayoutCards(contentWidth);
            pageScroll.y = Mathf.Clamp(pageScroll.y, 0f, Mathf.Max(0f, totalHeight - viewport.height));
            var previousThumb = GUI.skin.verticalScrollbarThumb;
            GUI.skin.verticalScrollbarThumb = scrollThumbStyle;
            pageScroll = GUI.BeginScrollView(viewport, pageScroll, new Rect(0, 0, contentWidth, totalHeight), false, false, GUIStyle.none, scrollStyle);
            GUI.skin.verticalScrollbarThumb = previousThumb;
            foreach (var card in cards) PaintCard(card);
            GUI.EndScrollView();
            GUI.Label(new Rect(contentX, window.height - 36, availableWidth, 26),
                KeyBindings.IsCapturing ? "Press a key / mouse button • Esc cancel • Delete clear" : Time.unscaledTime < statusUntil ? status : KeyBindings.LastError ?? VisualSettings.LastError ?? "Insert: menu  /  End: disable gameplay mods", mutedStyle);
            GUI.DragWindow(new Rect(0, 0, window.width, 64));
            GUI.enabled = contentEnabled;
            MenuWidgets.DrawColorPickerOverlay();
        }

        /// <summary>Uses compact sidebar rows so navigation fits a short window.</summary>
        private static void DrawNavigation(float width)
        {
            float rowHeight = Mathf.Min(46f, Mathf.Max(18f, (window.height - 104f - (HasHost ? 0 : 38)) / Pages.Length));
            for (int i = 0; i < Pages.Length; i++)
            {
                Rect row = new Rect(10, 88 + (HasHost ? 0 : 38) + i * rowHeight, width - 20, rowHeight - 5);
                if (GUI.Button(row, Pages[i].Title, i == page ? navActiveStyle : navStyle))
                { page = i; pageScroll = Vector2.zero; GUI.FocusControl(null); }
                if (i == page) ESPHelper.Fill(new Rect(row.x, row.y + 8, 2, row.height - 16), Accent);
            }
        }

        /// <summary>Registers a card; no layout height is guessed in page definitions.</summary>
        internal static void AddCard(int column, string title, Action<CardCursor> content, bool hostOnly = false)
        { cards.Add(new CardDefinition { Column = column, Title = title, Content = content, HostOnly = hostOnly }); }

        /// <summary>Measures wrapped text and every control, then flows each column without clipping.</summary>
        private static float LayoutCards(float width)
        {
            var layout = new CardLayout(width);
            foreach (var card in cards)
            {
                float cardWidth = layout.WidthFor(card.Column);
                var measure = new CardCursor(cardWidth - 32f, true);
                card.Content(measure);
                CardLayout.Placement position = layout.Add(card.Column, 52f + measure.Y);
                card.Rect = new Rect(position.X, position.Y, position.Width, position.Height);
            }
            return layout.Height;
        }

        /// <summary>Paints the measured card once; only this pass can process input or change values.</summary>
        private static void PaintCard(CardDefinition card)
        {
            Panel(card.Rect, cardStyle, 0.22f);
            GUI.Label(new Rect(card.Rect.x + 16, card.Rect.y + 12, card.Rect.width - 32, 22), card.Title, subtitleStyle);
            GUI.BeginGroup(new Rect(card.Rect.x + 16, card.Rect.y + 42, card.Rect.width - 32, card.Rect.height - 46));
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && (!card.HostOnly || HasHost);
            try { card.Content(new CardCursor(card.Rect.width - 32, false)); }
            finally { GUI.enabled = enabled; GUI.EndGroup(); }
        }

        /// <summary>Restores input and releases all generated UI resources.</summary>
        public static void Dispose()
        {
            SetOpen(false);
            DisposeStyles();
            MenuWidgets.DisposeColorPicker();
            cards.Clear();
            layoutChanges.Clear();
            Pages = Array.Empty<IMenuPage>();
        }

        #endregion

        /// <summary>Displays a short status message in the reserved footer.</summary>
        internal static void Toast(string message)
        {
            status = message;
            statusUntil = Time.unscaledTime + 3.5f;
        }

        /// <summary>Applies foldout/navigation changes before the next measure pass, keeping paint geometry consistent.</summary>
        internal static void DeferLayoutChange(Action action) { layoutChanges.Enqueue(action); }
        /// <summary>Centers the current window inside the display.</summary>
        internal static void CenterWindow()
        {
            window.x = (Screen.width - window.width) * 0.5f;
            window.y = (Screen.height - window.height) * 0.5f;
            Toast("Window centered");
        }
    }
}
