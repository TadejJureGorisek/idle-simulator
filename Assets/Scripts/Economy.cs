using System;
using System.Globalization;
using UnityEngine;

namespace IdleSim
{
    // Money, lifetime stats, save/load, and offline earnings.
    public class Economy : MonoBehaviour
    {
        public static Economy Instance { get; private set; }

        public double Money;
        public double TotalEarned;
        public int CustomersServed;
        public int LostSales;
        public double OfflineGain;   // for the welcome-back toast

        const double StartCash = 50;
        const double OfflineCapSeconds = 7200; // 2 hours
        const double OfflineEfficiency = 0.5;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Load();
        }

        public void Add(double amt) { Money += amt; TotalEarned += amt; }

        public bool TrySpend(double amt)
        {
            if (Money >= amt) { Money -= amt; return true; }
            return false;
        }

        public void RecordServed() { CustomersServed++; }
        public void RecordLost() { LostSales++; }

        // Shared K/M/B/T money formatter (used by the edit-mode catalog). Named Fmt to avoid
        // colliding with the Money field above.
        public static string Fmt(double v)
        {
            if (v < 1000) return "$" + v.ToString("0.#");
            string[] s = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "De" };
            int i = 0; double x = v;
            while (x >= 1000 && i < s.Length - 1) { x /= 1000; i++; }
            return "$" + x.ToString("0.00") + s[i];
        }

        public void Save(double incomePerSec)
        {
            var ic = CultureInfo.InvariantCulture;
            PlayerPrefs.SetString("money", Money.ToString(ic));
            PlayerPrefs.SetString("earned", TotalEarned.ToString(ic));
            PlayerPrefs.SetInt("served", CustomersServed);
            PlayerPrefs.SetInt("lost", LostSales);
            PlayerPrefs.SetString("lastQuit", DateTime.UtcNow.Ticks.ToString(ic));
            PlayerPrefs.SetString("lastInc", incomePerSec.ToString(ic));
            PlayerPrefs.Save();
        }

        public void Load()
        {
            var ic = CultureInfo.InvariantCulture;
            if (!PlayerPrefs.HasKey("money"))
            {
                Money = StartCash;
                return;
            }

            Money = double.Parse(PlayerPrefs.GetString("money", "50"), ic);
            TotalEarned = double.Parse(PlayerPrefs.GetString("earned", "0"), ic);
            CustomersServed = PlayerPrefs.GetInt("served", 0);
            LostSales = PlayerPrefs.GetInt("lost", 0);

            double inc = double.Parse(PlayerPrefs.GetString("lastInc", "0"), ic);
            if (inc > 0 && PlayerPrefs.HasKey("lastQuit"))
            {
                long ticks = long.Parse(PlayerPrefs.GetString("lastQuit", "0"), ic);
                var last = new DateTime(ticks, DateTimeKind.Utc);
                double elapsed = (DateTime.UtcNow - last).TotalSeconds;
                elapsed = Math.Max(0, Math.Min(elapsed, OfflineCapSeconds));
                OfflineGain = inc * elapsed * OfflineEfficiency;
                Add(OfflineGain);
            }
        }
    }
}
