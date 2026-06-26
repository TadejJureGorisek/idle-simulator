using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleSim
{
    // Central hub: config, upgrades, auto-systems, click input, income estimate, navigation,
    // fixtures (shelves/dividers/checkout), and save/load of progression + store layout.
    public class Sim : MonoBehaviour
    {
        public static Sim Instance { get; private set; }

        public Transform ShelfParent;
        public List<Shelf> Shelves = new List<Shelf>();
        [System.NonSerialized] public List<Transform> Dividers = new List<Transform>();
        public Checkout Checkout;
        public CustomerSpawner Spawner;
        public Transform Entrance, Exit;

        public int ItemCost = 2;
        public int ItemPrice = 5;
        public int MaxCart = 4; // a customer buys a random 1..MaxCart products

        public int Cashiers;
        public int Restockers;
        public int Managers;
        public double IncomeMult = 1.0;

        public float BaseSpawnInterval = 3f;
        public float SpawnInterval = 3f;
        public float BaseCheckoutInterval = 3f;
        public float CheckoutInterval = 3f;
        public float RestockInterval = 4f;

        [System.NonSerialized] public List<Upgrade> Upgrades = new List<Upgrade>();
        [System.NonSerialized] public NavGrid Nav;
        [System.NonSerialized] public bool Editing;
        [System.NonSerialized] public float GhostUntil;
        public bool IsGhost => Time.time < GhostUntil;

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

        void Start() { LoadLevels(); LoadLayout(); RebuildNav(); }

        void BuildUpgrades()
        {
            Upgrades.Clear();
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

        // ---- fixtures ----
        public Shelf AddShelf(Vector3 pos)
        {
            var rootGO = new GameObject("Shelf");
            if (ShelfParent != null) rootGO.transform.SetParent(ShelfParent);
            var shelf = rootGO.AddComponent<Shelf>();

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(rootGO.transform, false);
            body.transform.localScale = new Vector3(2.2f, 1.2f, 0.8f);
            body.transform.localPosition = new Vector3(0, 0.6f, 0);
            body.GetComponent<Renderer>().material.color = new Color(0.45f, 0.40f, 0.35f);

            rootGO.transform.position = pos;
            shelf.Init();
            Shelves.Add(shelf);
            return shelf;
        }

        public void RemoveLastShelf()
        {
            if (Shelves.Count == 0) return;
            var s = Shelves[Shelves.Count - 1];
            Shelves.RemoveAt(Shelves.Count - 1);
            if (s != null) Destroy(s.gameObject);
        }

        public Transform AddDivider(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Divider";
            if (ShelfParent != null) go.transform.SetParent(ShelfParent);
            go.transform.localScale = new Vector3(0.3f, 1.0f, 3.0f);
            pos.y = 0.5f;
            go.transform.position = pos;
            go.GetComponent<Renderer>().material.color = new Color(0.50f, 0.50f, 0.55f);
            go.AddComponent<Divider>();
            Dividers.Add(go.transform);
            return go.transform;
        }

        public bool Buy(Upgrade u)
        {
            if (u.IsMaxed) return false;
            if (Economy.Instance.TrySpend(u.CurrentCost))
            {
                u.Level++;
                u.Apply?.Invoke();
                RebuildNav();
                return true;
            }
            return false;
        }

        public Shelf GetStockedShelf()
        {
            var stocked = Shelves.FindAll(s => s != null && s.Stock > 0);
            if (stocked.Count == 0) return null;
            return stocked[UnityEngine.Random.Range(0, stocked.Count)];
        }

        void Update()
        {
            if (Editing) return; // store paused while editing the layout

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
                if (s != null && s.Ratio < lowest) { lowest = s.Ratio; low = s; }
            if (low != null) low.RestockAffordable();
        }

        void HandleInput()
        {
            if (Input.GetMouseButtonDown(0) && Camera.main != null)
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
            if (Input.GetKeyDown(KeyCode.R)) { foreach (var s in Shelves) if (s != null) s.RestockAffordable(); }
            if (Input.GetKeyDown(KeyCode.F12)) PlayerPrefs.DeleteAll();
        }

        public double AvgBasket => (1 + MaxCart) / 2.0;

        public double EstIncomePerSec()
        {
            if (Cashiers <= 0 || Restockers <= 0) return 0;
            double serveRate = 1.0 / CheckoutInterval;
            double spawnRate = 1.0 / SpawnInterval;
            return Math.Min(serveRate, spawnRate) * Profit * AvgBasket;
        }

        // ---- navigation ----
        public void RebuildNav()
        {
            Physics.SyncTransforms();
            Nav = new NavGrid(new Vector3(-11f, 0, -8f), new Vector3(11f, 0, 8f), 0.5f);
            foreach (var s in Shelves)
            {
                if (s == null) continue;
                var col = s.GetComponentInChildren<Collider>();
                if (col != null) Nav.BlockBounds(col.bounds, 0.4f);
            }
            foreach (var dv in Dividers)
            {
                if (dv == null) continue;
                var col = dv.GetComponent<Collider>();
                if (col != null) Nav.BlockBounds(col.bounds, 0.4f);
            }
            if (Checkout != null)
            {
                var c = Checkout.GetComponent<Collider>();
                if (c != null) Nav.BlockBounds(c.bounds, 0.4f);
            }
        }

        // ---- edit mode ----
        public void EnterEdit()
        {
            Editing = true;
            Time.timeScale = 0f; // hard pause
        }

        public void ResumeFromEdit()
        {
            Editing = false;
            Time.timeScale = 1f;
            RebuildNav();
            if (Checkout != null) Checkout.RefreshQueue();
            GhostUntil = Time.time + 1.5f; // brief walk-through-anything grace so nobody is trapped
            foreach (var c in UnityEngine.Object.FindObjectsByType<Customer>(FindObjectsSortMode.None))
                c.Repath();
            SaveLayout();
        }

        // ---- save / load ----
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

        public void SaveLayout()
        {
            var d = new LayoutData();
            if (Checkout != null) d.checkout = new XZ(Checkout.transform.position);
            foreach (var s in Shelves) if (s != null) d.shelves.Add(new XZ(s.transform.position));
            foreach (var dv in Dividers) if (dv != null) d.dividers.Add(new XZ(dv.position));
            PlayerPrefs.SetString("storeLayout", JsonUtility.ToJson(d));
        }

        public void LoadLayout()
        {
            if (!PlayerPrefs.HasKey("storeLayout")) return;
            try
            {
                var d = JsonUtility.FromJson<LayoutData>(PlayerPrefs.GetString("storeLayout"));
                if (d == null) return;

                if (Checkout != null && d.checkout != null)
                {
                    var p = Checkout.transform.position;
                    p.x = d.checkout.x; p.z = d.checkout.z;
                    Checkout.transform.position = p;
                }

                while (Shelves.Count < d.shelves.Count) AddShelf(new Vector3(0, 0, 3f));
                while (Shelves.Count > d.shelves.Count) RemoveLastShelf();
                for (int i = 0; i < Shelves.Count && i < d.shelves.Count; i++)
                {
                    var p = Shelves[i].transform.position;
                    p.x = d.shelves[i].x; p.z = d.shelves[i].z;
                    Shelves[i].transform.position = p;
                }

                foreach (var dv in Dividers) if (dv != null) Destroy(dv.gameObject);
                Dividers.Clear();
                foreach (var x in d.dividers) AddDivider(new Vector3(x.x, 0, x.z));
            }
            catch { /* corrupt layout -> keep authored default */ }
        }

        public void SaveAll()
        {
            if (Economy.Instance != null) Economy.Instance.Save(EstIncomePerSec());
            SaveLevels();
            SaveLayout();
            PlayerPrefs.Save();
        }

        void OnApplicationQuit() { SaveAll(); }
        void OnApplicationPause(bool paused) { if (paused) SaveAll(); }
    }
}
