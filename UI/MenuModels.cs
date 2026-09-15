using System;
using UnityEngine;

namespace UmbraMenu
{
    /// <summary>Contract for pages registered by MenuController; pages do not own runtime or window lifecycle.</summary>
    internal interface IMenuPage
    {
        string Title { get; }
        string Description { get; }
        void Build(float width);
    }
    /// <summary>Card declaration measured before it is painted.</summary>
    internal sealed class CardDefinition
    {
        public int Column;
        public string Title;
        public Action<CardCursor> Content;
        public Rect Rect;
        public bool HostOnly;
    }
    /// <summary>Shared cursor used by measure and paint passes.</summary>
    internal sealed class CardCursor
    {
        public readonly float Width;
        public readonly bool Measuring;
        public float Y;
        public CardCursor(float width, bool measuring) { Width = width; Measuring = measuring; }
        public void Advance(float value) { Y += value; }
        public void Space(float value) { Y += value; }
    }
    /// <summary>A labeled UI action with optional destructive visual emphasis.</summary>
    internal sealed class ButtonAction
    {
        public readonly string Label;
        public readonly Action Action;
        public readonly bool Danger;
        public ButtonAction(string label, Action action, bool danger = false) { Label = label; Action = action; Danger = danger; }
    }
}

