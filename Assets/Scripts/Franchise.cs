using System.Collections.Generic;
using UnityEngine;

namespace IdleSim
{
    // The prestige loop, kept deliberately simple:
    //   Sell your whole chain  ->  earn Franchise Points (FP) from how much you made this run
    //   ->  every FP permanently boosts ALL income  ->  start a fresh shop and grow faster.
    // A few one-time FP perks give a head start. Auto-attached by the HUD.
    public class Franchise : MonoBehaviour
    {
        public static Franchise Instance;

        public int FP;
        readonly HashSet<string> owned = new HashSet<string>();

        // Tunables: each FP = +2% income; selling gives sqrt(run earnings / 100) points.
        const double PercentPerFP = 0.02;
        const double FpDivisor = 100.0;

        struct Perk { public string id, name, desc; public int cost; }
        static readonly Perk[] Perks =
        {
            new Perk { id = "seed",    name = "Seed Money",        desc = "Start each shop with $250 instead of $50", cost = 5 },
            new Perk { id = "cashier", name = "Veteran Cashier",   desc = "Start with 1 cashier already hired",       cost = 8 },
            new Perk { id = "stocker", name = "Veteran Stocker",   desc = "Start with 1 restocker already hired",     cost = 8 },
            new Perk { id = "owl",     name = "Night Owl",         desc = "Start each shop with a +2h longer shift",  cost = 12 },
            new Perk { id = "brand",   name = "Brand Recognition", desc = "+25% income, always",                      cost = 15 },
            new Perk { id = "depts",   name = "Keep Departments",  desc = "Keep unlocked sections when you restart",  cost = 20 },
        };

        // permanent multiplier applied to all income (see Sim.Profit)
        public static double Mult => Instance != null ? Instance.CurrentMult : 1.0;
        public double CurrentMult => (1.0 + PercentPerFP * FP) * (Has("brand") ? 1.25 : 1.0);

        int FpOnReset()
        {
            if (Economy.Instance == null) return 0;
            return (int)System.Math.Floor(System.Math.Sqrt(Economy.Instance.RunEarned / FpDivisor));
        }

        bool Has(string id) => owned.Contains(id);

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Load();
        }

        void SellAndRestart()
        {
            var sim = Sim.Instance; var e = Economy.Instance;
            if (sim == null || e == null) return;
            FP += FpOnReset();
            sim.HardResetRun(Has("depts"));
            e.ResetRun(50 + (Has("seed") ? 200 : 0));
            if (Has("cashier")) sim.Cashiers = 1;
            if (Has("stocker")) sim.Restockers = 1;
            if (Has("owl")) sim.ShiftHours = Mathf.Min(24f, sim.ShiftHours + 2f);
            sim.RecalcRates();
            Save();
            sim.SaveAll();
        }

        void BuyPerk(Perk p)
        {
            if (Has(p.id) || FP < p.cost) return;
            FP -= p.cost;
            owned.Add(p.id);
            Save();
        }

        // ---------- save / load ----------
        public void Save()
        {
            PlayerPrefs.SetInt("fp", FP);
            foreach (var p in Perks) PlayerPrefs.SetInt("perk_" + p.id, Has(p.id) ? 1 : 0);
            PlayerPrefs.Save();
        }

        void Load()
        {
            FP = PlayerPrefs.GetInt("fp", 0);
            owned.Clear();
            foreach (var p in Perks) if (PlayerPrefs.GetInt("perk_" + p.id, 0) == 1) owned.Add(p.id);
        }

        // ---------- UI (IMGUI, like the rest of the HUD) ----------
        bool open, confirm, ready;
        GUIStyle title, body, btn, box;

        void Setup()
        {
            ready = true;
            title = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(1f, 0.86f, 0.5f);
            body = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
            body.normal.textColor = new Color(0.88f, 0.9f, 0.96f);
            btn = new GUIStyle(GUI.skin.button) { fontSize = 12 };
            box = new GUIStyle(GUI.skin.box);
        }

        void OnGUI()
        {
            if (Economy.Instance == null || Sim.Instance == null || Sim.Instance.Editing) return;
            if (!ready) Setup();

            // small always-on toggle, bottom-left
            if (GUI.Button(new Rect(12, Screen.height - 40, 220, 30), "FRANCHISE   ★ " + FP + " FP", btn))
            { open = !open; confirm = false; }

            if (!open) return;

            float w = 460, h = 150 + Perks.Length * 54 + 44;
            float x = (Screen.width - w) / 2f, y = (Screen.height - h) / 2f;
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, box);
            float ix = x + 18, iw = w - 36, cy = y + 14;

            GUI.Label(new Rect(ix, cy, iw, 22), "FRANCHISE", title); cy += 26;
            GUI.Label(new Rect(ix, cy, iw, 40),
                "Sell your whole chain and open a fresh shop. You keep your Franchise Points — every point permanently boosts ALL income.", body);
            cy += 42;

            int bonus = Mathf.RoundToInt((float)((CurrentMult - 1.0) * 100.0));
            GUI.Label(new Rect(ix, cy, iw, 20), "You have  ★ " + FP + " FP   →   +" + bonus + "% income right now", body);
            cy += 24;

            int gain = FpOnReset();
            if (!confirm)
            {
                GUI.enabled = gain > 0;
                if (GUI.Button(new Rect(ix, cy, iw, 30), gain > 0 ? ("SELL CHAIN & RESTART   (+" + gain + " FP)") : "Earn more before selling is worth it", btn))
                    confirm = true;
                GUI.enabled = true;
            }
            else
            {
                GUI.Label(new Rect(ix, cy - 2, iw, 18), "This wipes your current shop. Sure?", body);
                if (GUI.Button(new Rect(ix, cy + 16, iw / 2 - 4, 26), "Yes, sell (+" + gain + " FP)", btn))
                { SellAndRestart(); open = false; confirm = false; }
                if (GUI.Button(new Rect(ix + iw / 2 + 4, cy + 16, iw / 2 - 4, 26), "Cancel", btn)) confirm = false;
            }
            cy += 42;

            GUI.Label(new Rect(ix, cy, iw, 20), "PERMANENT PERKS  (spend FP)", title); cy += 26;
            foreach (var p in Perks)
            {
                GUI.Label(new Rect(ix, cy, iw - 96, 18), p.name, body);
                GUI.Label(new Rect(ix, cy + 17, iw - 96, 18), p.desc, body);
                if (Has(p.id)) GUI.Label(new Rect(ix + iw - 92, cy + 6, 92, 20), "OWNED", body);
                else
                {
                    GUI.enabled = FP >= p.cost;
                    if (GUI.Button(new Rect(ix + iw - 92, cy + 4, 92, 28), p.cost + " FP", btn)) BuyPerk(p);
                    GUI.enabled = true;
                }
                cy += 52;
            }

            if (GUI.Button(new Rect(x + w - 90, y + h - 32, 76, 24), "Close", btn)) { open = false; confirm = false; }
        }
    }
}
