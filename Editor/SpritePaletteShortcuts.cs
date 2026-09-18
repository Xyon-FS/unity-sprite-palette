using UnityEngine;

namespace WhatIDoToday.SpritePalette.Editor
{
    internal static class SpritePaletteShortcuts
    {
        internal const string RotateLeftDisplay = "Q";
        internal const string RotateRightDisplay = "E";
        internal const string ResizeUpDisplay = "+";
        internal const string ResizeDownDisplay = "-";
        internal const string OrderUpDisplay = "Page Up / ]";
        internal const string OrderDownDisplay = "Page Down / [";

        internal static bool HandlePlacementInput(Event currentEvent)
        {
            if (currentEvent == null ||
                currentEvent.type != EventType.KeyDown ||
                currentEvent.alt ||
                currentEvent.control ||
                currentEvent.command)
            {
                return false;
            }

            SpritePaletteWindow window = SpritePaletteWindow.ActivePlacementWindow;
            if (window == null)
                return false;

            switch (currentEvent.keyCode)
            {
                case KeyCode.Q:
                    window.RotatePlacement(-1f);
                    break;

                case KeyCode.E:
                    window.RotatePlacement(1f);
                    break;

                case KeyCode.Equals when currentEvent.shift:
                case KeyCode.KeypadPlus:
                    window.ResizePlacement(1f);
                    break;

                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    window.ResizePlacement(-1f);
                    break;

                case KeyCode.PageUp:
                case KeyCode.RightBracket:
                    window.AdjustOrder(1);
                    break;

                case KeyCode.PageDown:
                case KeyCode.LeftBracket:
                    window.AdjustOrder(-1);
                    break;

                case KeyCode.Escape:
                    window.StopPlacement();
                    break;

                default:
                    return false;
            }

            currentEvent.Use();
            return true;
        }
    }
}
