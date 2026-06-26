using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace IdleSim
{
    // Stage 1 of the idle economy: Cookie-Clicker-style producers (the main shop's departments),
    // generating passive $/s into Economy. Cost curve / outputs are the validated long-game numbers.
    // Drawn as a scrollable "DEPARTMENTS" panel; auto-attached by the HUD (no scene rebuild needed).
    public class ProducerEconomy : MonoBehaviour
    {
        public static ProducerEconomy Instance;
        public List<Producer> Producers = new List<Producer>();
        public double GlobalMult = 1.0;

        GUIStyle title, btn, box;
        bool ready;
        Vector2 scroll;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Build();
            Load();
        }

        void Build()
        {
            Producers.Clear();
            Add("snack",     "Snack Rack",          15,      0.1);
            Add("produce",   "Produce Stand",        100,    1);
            Add("dairy",     "Dairy Case",           1100,   8);
            Add("bakery",    "Bakery Counter",       12e3,   47);
            Add("deli",      "Deli",                 130e3,  260);
            Add("gaming",    "Gaming Aisle",         1.4e6,  1400);
            Add("electro",   "Electronics Dept",     20e6,   7800);
            Add("appliance", "Appliance Showroom",   330e6,  44e3);
            Add("pharma",    "Pharmacy",             5.1e9,  260e3);
            Add("luxury",    "Luxury & Gems",        75e9,   1.6e6);
            Add("warehouse", "Warehouse Club",       1e12,   10e6);
            Add("hyper",     "Hypermarket",          14e12,  65e6);
            Add("distrib",   "Distribution Hub",     170e12, 430e6);
            Add("galactic",  "Galactic Chain",       2.1e15, 2.9e9);
        }

        void Add(string id, string name, double cost, double output) => Producers.Add(new Producer(id, name, cost, output));

        public double IncomePerSec
        {
            get { double s = 0; foreach (var p in Producers) s += p.Rate; return s * GlobalMult; }
        }

        void Update()
        {
            if (Economy.Instance == null) return;
            Economy.Instance.Add(IncomePerSec * Time.deltaTime); // editing pauses via timeScale 0
        }

        public bool Buy(Producer p)
        {
            if (Economy.Instance.TrySpend(p.Cost)) { p.owned++; Save(); return true; }
            return false;
        }

        int RevealCount() // show bought tiers + the next one
        {
            int k = 1;
            for (int i = 0; i < Producers.Count; i++) { k = i + 1; if (Producers[i].owned == 0) break; }
            return Mathf.Min(Producers.Count, k);
        }

        void OnGUI()
        {
            if (Economy.Instance == null) return;
            if (Sim.Instance != null && Sim.Instance.Editing) return; // hide while editing (catalog takes the left)
            if (!ready) Setup();

            int reveal = RevealCount();
            const float w = 252f, rowH = 46f, x = 12f, y = 158f;
            float rows = Mathf.Min(reveal, 8);
            float h = rows * (rowH + 4f) + 28f;

            GUI.Box(new Rect(x - 4, y - 4, w + 8, h + 8), GUIContent.none, box);
            GUI.Label(new Rect(x, y, w, 20), "DEPARTMENTS    " + Money(IncomePerSec) + "/s", title);

            var view = new Rect(x, y + 24, w, h - 24);
            var content = new Rect(0, 0, w - 20, reveal * (rowH + 4f));
            scroll = GUI.BeginScrollView(view, scroll, content);
            var e = Economy.Instance;
            for (int i = 0; i < reveal; i++)
            {
                var p = Producers[i];
                GUI.enabled = e.Money >= p.Cost;
                string lbl = p.name + "   x" + p.owned + "\n" + Money(p.Rate * GlobalMult) + "/s      buy " + Money(p.Cost);
                if (GUI.Button(new Rect(0, i * (rowH + 4f), w - 20, rowH), lbl, btn)) Buy(p);
                GUI.enabled = true;
            }
            GUI.EndScrollView();
        }

        void Setup()
        {
            ready = true;
            title = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(0.82f, 0.90f, 1f);
            btn = new GUIStyle(GUI.skin.button) { fontSize = 11, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(8, 6, 3, 3) };
            box = new GUIStyle(GUI.skin.box);
        }

        // ---- save / load ----
        public void Save()
        {
            var ic = CultureInfo.InvariantCulture;
            foreach (var p in Producers) PlayerPrefs.SetInt("prod_" + p.id, p.owned);
            PlayerPrefs.SetString("prodMult", GlobalMult.ToString(ic));
        }

        void Load()
        {
            var ic = CultureInfo.InvariantCulture;
            foreach (var p in Producers) p.owned = PlayerPrefs.GetInt("prod_" + p.id, 0);
            if (PlayerPrefs.HasKey("prodMult")) GlobalMult = double.Parse(PlayerPrefs.GetString("prodMult", "1"), ic);
        }

        void OnApplicationQuit() { Save(); }
        void OnApplicationPause(bool paused) { if (paused) Save(); }

        public static string Money(double v)
        {
            if (v < 1000) return "$" + v.ToString("0.#");
            string[] s = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "De" };
            int i = 0; double x = v;
            while (x >= 1000 && i < s.Length - 1) { x /= 1000; i++; }
            return "$" + x.ToString("0.00") + s[i];
        }
    }
}
