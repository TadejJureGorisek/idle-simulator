using UnityEngine;

namespace IdleSim
{
    // Applies the Higgsfield holographic-glass skin to the built-in IMGUI box + button styles, so EVERY
    // HUD panel and button picks it up with no per-call changes. Idempotent — call EnsureApplied() at the
    // top of each OnGUI (before a component builds its own cached styles, so the copies inherit the skin).
    public static class UISkin
    {
        static bool applied;
        public static Texture2D Panel, Button;

        public static void EnsureApplied()
        {
            if (applied) return;
            applied = true;

            Panel = Resources.Load<Texture2D>("ui_panel");
            Button = Resources.Load<Texture2D>("ui_button");

            if (Panel != null)
            {
                // Small inset to match the panel tile's SMALL corner radius — a border larger than the
                // corner radius is fine (captures the whole corner), smaller smears it. Tile has low-radius corners.
                int b = Mathf.Clamp(Mathf.RoundToInt(Panel.width * 0.05f), 10, 13);
                var box = GUI.skin.box;
                box.normal.background = Panel;
                box.border = new RectOffset(b, b, b, b);
                box.normal.textColor = Color.white;
                box.richText = true;
            }
            if (Button != null)
            {
                // Small inset so even short (≈22 px) rows read as rounded rectangles, not pills.
                int b = Mathf.Clamp(Mathf.RoundToInt(Button.width * 0.05f), 10, 14);
                var bt = GUI.skin.button;
                bt.normal.background = Button;
                bt.hover.background = Button;
                bt.active.background = Button;
                bt.focused.background = Button;
                bt.onNormal.background = Button;
                bt.onHover.background = Button;
                bt.onActive.background = Button;
                bt.border = new RectOffset(b, b, b, b);
                bt.alignment = TextAnchor.MiddleCenter;
                bt.wordWrap = false;
                bt.richText = true;
                bt.normal.textColor = new Color(0.86f, 0.92f, 1f);
                bt.hover.textColor = new Color(0.55f, 0.95f, 1f);   // cyan pop on hover
                bt.active.textColor = Color.white;
            }
        }
    }
}
