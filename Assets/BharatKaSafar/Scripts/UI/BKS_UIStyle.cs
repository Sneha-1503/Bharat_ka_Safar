using TMPro;
using UnityEngine;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// One place for the game's look (TealTravel): colours and the main font, so every panel matches.
    /// Font: Electronic Highway Sign SDF (TextMesh Pro "Fonts & Materials" Resources folder).
    /// </summary>
    public static class BKS_UIStyle
    {
        public static readonly Color Teal      = new Color(0.04f, 0.42f, 0.50f, 0.96f);
        public static readonly Color TealDark  = new Color(0.02f, 0.25f, 0.31f, 0.96f);
        public static readonly Color Gold      = new Color(1.00f, 0.80f, 0.30f, 1f);
        public static readonly Color Parchment = new Color(0.98f, 0.92f, 0.76f, 1f);
        public static readonly Color Ink       = new Color(0.32f, 0.18f, 0.05f, 1f);

        private static TMP_FontAsset font;
        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Electronic Highway Sign SDF");
                return font;
            }
        }

        /// <summary>Apply the game font to a text (keeps everything else).</summary>
        public static void Use(TMP_Text t) { if (t != null && Font != null) t.font = Font; }
    }
}
