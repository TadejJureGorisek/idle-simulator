using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleSim
{
    // Central hub: shared config, upgrades, auto-systems (cashier/restocker),
    // click input, income estimate, and save/load of progression.
    public class Sim : MonoBehaviour
    {
        public static Sim Instance { get; private set; }

        // scene references (wired by IdleBootstrap)
        public List<Shelf> Shelves = new List<Shelf>();
        public Checkout Checkout;
        public CustomerSpawner Spawner;
        public Transform Entrance, Exit;

        // economy config
        public int ItemCost = 2;
        public int ItemPrice = 5;

        // upgradeable state
        public int Cashiers;
        public int Restockers;
        public int Managers;
        public double IncomeMult = 1.0;

        public float BaseSpawnInterval = 3f;
        public float SpawnInterval = 3f;
        public float BaseCheckoutInterval = 3f;
        public float CheckoutInterval = 3f; // effective auto-checkout interval
        public float RestockInterval = 4f;

        public List<Upgrade> Upgrades = new List<Upgrade>();

        int checkoutSpeedSteps;
        int adSteps;
        float autoCheckoutTimer;
        float autoRestockTimer;

        public double Profit => (ItemPrice - ItemCost) * IncomeMult;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            BuildUpgrades();
        }

        void Start() { RecalcRates(); }

        void BuildUpgrades()
        {
            Upgrades.Clear();
            Upgrades.Add(new Upgrade("shelf", "Extra Shelf", 120, 1.10f, () => IdleBootstrap.AddShelf(), 4));
            Upgrades.Add(new Upgrade("cashier", "Hire Cashier", 200, 1.12f, () => { Cashiers++; RecalcRates(); }));
            Upgrades.Add(new Upgrade("restocker", "Hire Restocker", 400, 1.12f, () => { Restockers++; RecalcRates(); }));
            Upgrades.Add(new Upgrade("fastco", "Faster Checkout", 600, 1.12f, () => { checkoutSpeedSteps++; RecalcRates(); }));
            Upgrades.Add(new Upgrade("manager", "Hire Manager", 2500, 1.15f, () => { Managers++; IncomeMult = 1.0 + 0.25 * Managers; }));
            Upgrades.Add(new Upgrade("ads", "Advertising", 3000, 1.13f, () => { adSteps++; RecalcRates(); }));
        }

        void RecalcRates()
        {
            CheckoutInterval = BaseCheckoutInterval;
            if (Cashiers > 0) CheckoutInterval = BaseCheckoutInterval / Cashiers;
            CheckoutInterval *= Mathf.Pow(0.85f, checkoutSpeedSteps);
            CheckoutInterval = Mathf.Max(0.3f, CheckoutInterval);

            SpawnInterval = Mathf.Max(0.4f, BaseSpawnInterval * Mathf.Pow(0.85f, adSteps));
        }

        public bool Buy(Upgrade u)
        {
            if (u.IsMaxed) return false;
            if (Economy.Instance.TrySpend(u.CurrentCost))
            {
                u.Level++;
                u.Apply?.Invoke();
                return true;
            }
            return false;
        }

        public Shelf GetStockedShelf()
        {
            var stocked = Shelves.FindAll(s => s.Stock > 0);
            if (stocked.Count == 0) return null;
            return stocked[UnityEngine.Random.Range(0, stocked.Count)];
        }

        void Update()
        {
            HandleInput();

            if (Cashiers > 0)
            {
                autoCheckoutTimer += Time.deltaTime;
                if (autoCheckoutTimer >= CheckoutInterval)
                {
                    autoCheckoutTimer = 0f;
                    Checkout.ServeFront();
                }
            }

            if (Restockers > 0)
            {
                autoRestockTimer += Time.deltaTime;
                if (autoRestockTimer >= RestockInterval / Restockers)
                {
                    autoRestockTimer = 0f;
                    AutoRestockOne();
                }
            }
        }

        void AutoRestockOne()
        {
            Shelf low = null;
            float lowest = 0.5f;
            foreach (var s in Shelves)
            {
                if (s.Ratio < lowest) { lowest = s.Ratio; low = s; }
            }
            if (low != null) low.RestockAffordable();
        }

        void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 200f))
                {
                    var cust = hit.collider.GetComponentInParent<Customer>();
                    if (cust != null && cust.InLine) { Checkout.ServeFront(); return; }
                    var shelf = hit.collider.GetComponentInParent<Shelf>();
                    if (shelf != null) { shelf.RestockAffordable(); return; }
                }
            }
            if (Input.GetKeyDown(KeyCode.Space)) Checkout.ServeFront();
            if (Input.GetKeyDown(KeyCode.R)) { foreach (var s in Shelves) s.RestockAffordable(); }
            if (Input.GetKeyDown(KeyCode.F12)) { PlayerPrefs.DeleteAll(); }
        }

        public double EstIncomePerSec()
        {
            if (Cashiers <= 0 || Restockers <= 0) return 0; // runs unattended only when both staffed
            double serveRate = 1.0 / CheckoutInterval;
            double spawnRate = 1.0 / SpawnInterval;
            return Math.Min(serveRate, spawnRate) * Profit;
        }

        public void SaveLevels()
        {
            foreach (var u in Upgrades) PlayerPrefs.SetInt("lvl_" + u.Id, u.Level);
        }

        public void LoadLevels()
        {
            foreach (var u in Upgrades)
            {
                int lvl = PlayerPrefs.GetInt("lvl_" + u.Id, 0);
                for (int i = 0; i < lvl; i++) { u.Level++; u.Apply?.Invoke(); }
            }
            RecalcRates();
        }

        public void SaveAll()
        {
            Economy.Instance.Save(EstIncomePerSec());
            SaveLevels();
            PlayerPrefs.Save();
        }

        void OnApplicationQuit() { SaveAll(); }
        void OnApplicationPause(bool paused) { if (paused) SaveAll(); }
    }
}
