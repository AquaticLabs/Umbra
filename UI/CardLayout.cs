using System;

namespace UmbraMenu
{
    /// <summary>Pure measured-card packing shared by the native menu and regression tests.</summary>
    internal sealed class CardLayout
    {
        private const float Gap = 16f;
        private readonly float width;
        private float left, right;
        public float Height { get { return Math.Max(left, right); } }

        /// <summary>Creates a fresh layout in physical pixels.</summary>
        public CardLayout(float availableWidth) { width = Math.Max(1f, availableWidth); }

        /// <summary>Uses one column on narrow viewports and allows explicit full-width sections.</summary>
        public float WidthFor(int column) { return width < 640f || column == 2 ? width : (width - Gap) * 0.5f; }

        /// <summary>Places a measured card without overlap; full-width cards flush both columns.</summary>
        public Placement Add(int column, float measuredHeight)
        {
            bool full = width < 640f || column == 2;
            float cardWidth = WidthFor(column), height = Math.Max(0f, measuredHeight);
            float y = full ? Math.Max(left, right) : column == 0 ? left : right;
            var result = new Placement { X = full || column == 0 ? 0 : cardWidth + Gap, Y = y, Width = cardWidth, Height = height };
            if (full) left = right = y + height + Gap;
            else if (column == 0) left = y + height + Gap;
            else right = y + height + Gap;
            return result;
        }

        /// <summary>A Unity-independent rectangle suitable for tests and GUI conversion.</summary>
        internal struct Placement { public float X, Y, Width, Height; }
    }
}
