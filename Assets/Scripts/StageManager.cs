using System.Collections.Generic;
using UnityEngine;

namespace IdleSim
{
    // Runs every location's 3-step stage (see Stages.cs): work arrives at step 0, flows buf0 → buf1 →
    // shipped. Each step is done manually (DoTask) or automated by hiring its employee. Sustained
    // shipping keeps a live efficiency (0..1) up; that efficiency scales how much of the location's
    // effect the main store receives. One component drives all five; state is saved per location.
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance;

        public class State
        {
            public string id;
            public int[] emp = new int[3];       // employees per step
            public int buf0, buf1;               // intermediate buffers
            public int upArrival, upCapacity;    // upgrades
            public float eff;                    // 0..1 live efficiency
            public int shipped;
            // transient
            public List<float> queue = new List<float>();
            public float arrTimer; public float[] acc = new float[3];
            public int QueueCount => queue.Count;
        }

        readonly Dictionary<string, State> states = new Dictionary<string, State>();

        const int QMax = 6;
        const float EffDecay = 0.05f, EffBump = 0.30f, Patience = 10f;
        const float Rate0 = 1.0f, Rate1 = 0.9f, Rate2 = 1.2f;   // seconds per auto action, per employee

        void Awake() { Instance = this; }
        void OnDisable() { SaveAll(); }
        void OnApplicationQuit() { SaveAll(); }

        public bool Active(string id) => Sim.Instance != null && Sim.Instance.LocLev(id) > 0;
        public State Get(string id) { if (!states.TryGetValue(id, out var s)) { s = new State { id = id }; Load(s); states[id] = s; } return s; }
        public int Cap(State s) => 12 + s.upCapacity * 8;
        int Infra(State s) => (Sim.Instance != null ? Sim.Instance.LocLev(s.id) : 0) + s.upArrival + s.upCapacity;
        float ArrInterval(State s) { var d = Stages.For(s.id); return Mathf.Max(0.35f, d.arrivalBase * Mathf.Pow(0.85f, s.upArrival)); }

        public float Eff(string id) => Active(id) ? Mathf.Clamp01(Get(id).eff) : 0f;

        // --- effects read by Sim ---
        public float Factor(string id)      // cost / spoil: live multiplicative discount
        {
            var d = Stages.For(id); var s = Get(id);
            return Mathf.Lerp(1f, Mathf.Pow(1f - d.perLevel, Infra(s)), Mathf.Clamp01(s.eff));
        }
        public float IncomeMult(string id)  // income: live additive bonus
        {
            var d = Stages.For(id); var s = Get(id);
            return 1f + d.perLevel * Infra(s) * Mathf.Clamp01(s.eff);
        }

        void Update()
        {
            var sim = Sim.Instance; if (sim == null || sim.Editing) return;
            float dt = Time.deltaTime;
            foreach (var def in Locations.All)
            {
                if (!Active(def.id)) continue;
                Tick(Get(def.id), dt);
            }
        }

        void Tick(State s, float dt)
        {
            // arrivals into step 0's queue (with patience so idle work eventually leaves)
            for (int i = s.queue.Count - 1; i >= 0; i--) { s.queue[i] += dt; if (s.queue[i] > Patience) s.queue.RemoveAt(i); }
            s.arrTimer += dt; if (s.arrTimer >= ArrInterval(s)) { s.arrTimer = 0; if (s.queue.Count < QMax) s.queue.Add(0f); }

            // employees automate each step
            AutoStep(s, 0, Rate0, dt);
            AutoStep(s, 1, Rate1, dt);
            AutoStep(s, 2, Rate2, dt);

            // efficiency decays unless ships keep bumping it (handled in DoTask step 2)
            s.eff = Mathf.MoveTowards(s.eff, 0f, EffDecay * dt);
        }

        void AutoStep(State s, int t, float rate, float dt)
        {
            if (s.emp[t] <= 0) return;
            s.acc[t] += dt * s.emp[t];
            int guard = 0;
            while (s.acc[t] >= rate && guard++ < 64) { s.acc[t] -= rate; if (!DoTask(s, t)) { s.acc[t] = 0; break; } }
        }

        public bool CanDo(State s, int t)
        {
            int cap = Cap(s);
            if (t == 0) return s.queue.Count > 0 && s.buf0 < cap;
            if (t == 1) return s.buf0 > 0 && s.buf1 < cap;
            return s.buf1 > 0;
        }

        public bool DoTask(State s, int t)
        {
            if (!CanDo(s, t)) return false;
            if (t == 0) { s.queue.RemoveAt(0); s.buf0++; }
            else if (t == 1) { s.buf0--; s.buf1++; }
            else { s.buf1--; s.shipped++; s.eff = Mathf.Lerp(s.eff, 1f, EffBump); }   // a shipment keeps efficiency up
            return true;
        }

        // --- purchases (main cash) ---
        public int EmpCost(State s, int t) => (int)(150000 * Mathf.Pow(1.7f, s.emp[t]));
        public int ArrUpCost(State s) => (int)(250000 * Mathf.Pow(1.8f, s.upArrival));
        public int CapUpCost(State s) => (int)(250000 * Mathf.Pow(1.8f, s.upCapacity));
        public bool HireEmp(State s, int t) { if (Pay(EmpCost(s, t))) { s.emp[t]++; Save(s); return true; } return false; }
        public bool UpgradeArrival(State s) { if (Pay(ArrUpCost(s))) { s.upArrival++; Save(s); return true; } return false; }
        public bool UpgradeCapacity(State s) { if (Pay(CapUpCost(s))) { s.upCapacity++; Save(s); return true; } return false; }
        static bool Pay(double c) => Economy.Instance != null && Economy.Instance.TrySpend(c);

        // --- boost text for the UI ---
        public string BoostText(string id)
        {
            var d = Stages.For(id); var s = Get(id);
            if (d.effect == "income")
            {
                int p = Mathf.RoundToInt((IncomeMult(id) - 1f) * 100f);
                int mp = Mathf.RoundToInt(d.perLevel * Infra(s) * 100f);
                return "<color=#6BE08A>+" + p + "% income</color> <color=#8A98B0>(max +" + mp + "%)</color>";
            }
            string w = d.effect == "cost" ? "cost of goods" : "spoilage";
            int q = Mathf.RoundToInt((1f - Factor(id)) * 100f);
            int mq = Mathf.RoundToInt((1f - Mathf.Pow(1f - d.perLevel, Infra(s))) * 100f);
            return "<color=#6BE08A>−" + q + "% " + w + "</color> <color=#8A98B0>(max −" + mq + "%)</color>";
        }

        // --- save / load (per location) ---
        void SaveAll() { foreach (var kv in states) Save(kv.Value); }
        void Save(State s)
        {
            string p = "st_" + s.id + "_";
            PlayerPrefs.SetInt(p + "e0", s.emp[0]); PlayerPrefs.SetInt(p + "e1", s.emp[1]); PlayerPrefs.SetInt(p + "e2", s.emp[2]);
            PlayerPrefs.SetInt(p + "b0", s.buf0); PlayerPrefs.SetInt(p + "b1", s.buf1);
            PlayerPrefs.SetInt(p + "ua", s.upArrival); PlayerPrefs.SetInt(p + "uc", s.upCapacity);
            PlayerPrefs.SetInt(p + "sh", s.shipped); PlayerPrefs.SetFloat(p + "ef", s.eff);
        }
        void Load(State s)
        {
            string p = "st_" + s.id + "_";
            s.emp[0] = PlayerPrefs.GetInt(p + "e0", 0); s.emp[1] = PlayerPrefs.GetInt(p + "e1", 0); s.emp[2] = PlayerPrefs.GetInt(p + "e2", 0);
            s.buf0 = PlayerPrefs.GetInt(p + "b0", 0); s.buf1 = PlayerPrefs.GetInt(p + "b1", 0);
            s.upArrival = PlayerPrefs.GetInt(p + "ua", 0); s.upCapacity = PlayerPrefs.GetInt(p + "uc", 0);
            s.shipped = PlayerPrefs.GetInt(p + "sh", 0); s.eff = PlayerPrefs.GetFloat(p + "ef", 0f);
        }
    }
}
