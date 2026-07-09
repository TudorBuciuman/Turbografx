using UnityEngine;

namespace BITROOT.UI
{
    /// <summary>
    /// Single source of truth for the Cyberpunk 2077-inspired palette so every UI
    /// script (inventory, weapon wheel, health bar, crafting menu) pulls from the
    /// same colors instead of each hardcoding its own hex values.
    ///
    /// Pairs well with a sharp, slightly condensed font (e.g. Rajdhani, Chakra Petch,
    /// or Blender Pro if you own it) and hard-edged or single-corner-cut panels rather
    /// than rounded rectangles - rounded corners read as "mobile app," not "netrunner."
    /// </summary>
    public static class CyberpunkTheme
    {
        public static readonly Color PanelBackground = new Color(0.04f, 0.05f, 0.07f, 0.92f);
        public static readonly Color PanelBorder = new Color(0f, 0.95f, 0.9f, 1f);        // neon cyan
        public static readonly Color AccentPrimary = new Color(1f, 0.05f, 0.55f, 1f);      // hot magenta
        public static readonly Color AccentSecondary = new Color(0.95f, 0.85f, 0f, 1f);    // warning yellow
        public static readonly Color TextPrimary = new Color(0.9f, 1f, 1f, 1f);
        public static readonly Color TextDim = new Color(0.55f, 0.7f, 0.7f, 1f);
        public static readonly Color SlotEmpty = new Color(1f, 1f, 1f, 0.04f);
        public static readonly Color SlotFilled = new Color(0f, 0.95f, 0.9f, 0.12f);
        public static readonly Color SlotHover = new Color(1f, 0.05f, 0.55f, 0.35f);
        public static readonly Color HealthHigh = new Color(0f, 0.95f, 0.6f, 1f);
        public static readonly Color HealthLow = new Color(1f, 0.15f, 0.2f, 1f);
    }
}
