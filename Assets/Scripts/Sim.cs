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
        [System.NonSerialized] public List<Transform> DecorItems = new List<Transform>();
        public int UnlockedItems = 1; // catalog items 0..UnlockedItems-1 are available in edit mode
        public Checkout Checkout;
        public CustomerSpawner Spawner;
        public Transform Entrance, Exit;

        // store floor footprint — the pad AND the edit grid both derive from this, so they stay
        // 1:1, and a future "expand floor" upgrade just changes these and calls ApplyFloorSize().
        public float FloorWidth = 18f;
        public float FloorDepth = 14f;
        public Vector3 FloorCenter = new Vector3(0f, 0f, 0f);
        public Transform Pad;
        public Transform PadRim;
        public Transform ShopRoot;   // parents all rotatable shop content
        public float ShopRotation;   // whole-shop rotation in 45-degree steps (edit mode)

        public int ItemCost = 2;
        public int ItemPrice = 5;
        public int MaxCart = 4; // a customer buys a random 1..MaxCart products

        // ---- day / shift ----
        public enum DayState { Open, Closed }
        public float ShiftHours = 8f;          // open hours (06:00 -> 06:00 + ShiftHours); upgrades raise to 24
        public float ShiftRealSeconds = 1200f; // one shift's open period = 20 real minutes
        public int CleanReward = 8;            // $ per cleaned mess
        public DoorController Door;
        [System.NonSerialized] public DayState State = DayState.Open;
        [System.NonSerialized] public int Day = 1;
        [System.NonSerialized] public float Clock = 6f; // in-game hour (6.0 = 06:00)
        [System.NonSerialized] public int Cleaners;
        [System.NonSerialized] public bool AutoNewDay;
        [System.NonSerialized] public int Mess;
        [System.NonSerialized] public int servedToday;
        readonly List<Transform> messObjs = new List<Transform>();
        float cleanTimer;

        public bool IsOpen => State == DayState.Open;
        public bool Is247 => ShiftHours >= 23.99f;
        float CloseTime => 6f + ShiftHours;

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

        void Start()
        {
            LoadLevels();
            LoadLayout();
            RebuildNav();
            Day = PlayerPrefs.GetInt("day", 1);
            State = DayState.Open;
            Clock = 6f;
            if (Door != null) Door.SetOpen(true);
        }

        void BuildUpgrades()
        {
            Upgrades.Clear();
            Upgrades.Add(new Upgrade("cashier", "Hire Cashier", 200, 1.12f, () => { Cashiers++; RecalcRates(); }));
            Upgrades.Add(new Upgrade("restocker", "Hire Restocker", 400, 1.12f, () => { Restockers++; RecalcRates(); }));
            Upgrades.Add(new Upgrade("fastco", "Faster Checkout", 600, 1.12f, () => { checkoutSpeedSteps++; RecalcRates(); }));
            Upgrades.Add(new Upgrade("manager", "Hire Manager", 2500, 1.15f, () => { Managers++; IncomeMult = 1.0 + 0.25 * Managers; }));
            Upgrades.Add(new Upgrade("ads", "Advertising", 3000, 1.13f, () => { adSteps++; RecalcRates(); }));
            Upgrades.Add(new Upgrade("cleaner", "Hire Cleaner", 350, 1.20f, () => { Cleaners++; }));
            Upgrades.Add(new Upgrade("shift", "Longer Shift +2h", 500, 1.45f, () => { ShiftHours = Mathf.Min(24f, ShiftHours + 2f); }, 8));
            Upgrades.Add(new Upgrade("autoday", "AUTO NEW DAY", 250000, 1f, () => { AutoNewDay = true; }, 1));
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
        public Shelf AddStand(CatalogItem it, Vector3 pos)
        {
            var go = Catalog.Build(it);
            if (ShelfParent != null) go.transform.SetParent(ShelfParent);
            go.transform.position = pos;
            var shelf = go.GetComponent<Shelf>();
            if (shelf != null) Shelves.Add(shelf);
            return shelf;
        }

        public Shelf AddShelf(Vector3 pos) => AddStand(Catalog.ById("st_basic"), pos);

        public Transform AddDecor(CatalogItem it, Vector3 pos)
        {
            var go = Catalog.Build(it);
            if (ShelfParent != null) go.transform.SetParent(ShelfParent);
            pos.y = 0f;
            go.transform.position = pos;
            DecorItems.Add(go.transform);
            return go.transform;
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
            if (u.Id == "autoday" && !Is247) return false; // unlocks only at 24/7
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

        public bool HasStock
        {
            get
            {
                foreach (var s in Shelves) if (s != null && s.Stock > 0) return true;
                return false;
            }
        }

        void Update()
        {
            if (Editing) return; // store paused while editing the layout

            TickDay();
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
                    var mess = hit.collider.GetComponentInParent<Mess>();
                    if (mess != null) { CleanMessAt(mess.transform); return; }
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

        // ---- day / shift ----
        void TickDay()
        {
            if (State == DayState.Open)
            {
                Clock += (ShiftHours / Mathf.Max(1f, ShiftRealSeconds)) * Time.deltaTime;
                if (Clock >= CloseTime) CloseShop();
            }
            else if (Cleaners > 0 && Mess > 0)
            {
                cleanTimer += Time.deltaTime;
                if (cleanTimer >= 1.5f / Cleaners) { cleanTimer = 0f; CleanOne(); }
            }
        }

        void CloseShop()
        {
            Clock = CloseTime;
            State = DayState.Closed;
            if (Door != null) Door.SetOpen(false);
            SpawnMess(Mathf.Min(25, 4 + servedToday));
            if (AutoNewDay) NewDay();
        }

        public void NewDay()
        {
            Day++;
            Clock = 6f;
            State = DayState.Open;
            servedToday = 0;
            ClearMess();
            if (Door != null) Door.SetOpen(true);
        }

        public void RecordServedToday() { servedToday++; }

        void SpawnMess(int n)
        {
            ClearMess();
            for (int i = 0; i < n; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Mess";
                go.transform.localScale = new Vector3(0.4f, 0.2f, 0.4f);
                float x = UnityEngine.Random.Range(-FloorWidth / 2f + 1f, FloorWidth / 2f - 1f);
                float z = UnityEngine.Random.Range(-FloorDepth / 2f + 1f, FloorDepth / 2f - 1f);
                var local = new Vector3(FloorCenter.x + x, 0.12f, FloorCenter.z + z);
                if (ShopRoot != null) { go.transform.SetParent(ShopRoot); go.transform.localPosition = local; }
                else go.transform.position = local;
                go.GetComponent<Renderer>().material.color = new Color(0.42f, 0.32f, 0.20f);
                go.AddComponent<Mess>();
                messObjs.Add(go.transform);
            }
            Mess = messObjs.Count;
        }

        void ClearMess()
        {
            foreach (var m in messObjs) if (m != null) Destroy(m.gameObject);
            messObjs.Clear();
            Mess = 0;
        }

        void CleanOne()
        {
            for (int i = messObjs.Count - 1; i >= 0; i--)
            {
                if (messObjs[i] != null)
                {
                    Destroy(messObjs[i].gameObject);
                    messObjs.RemoveAt(i);
                    Mess = messObjs.Count;
                    Economy.Instance.Add(CleanReward);
                    return;
                }
                messObjs.RemoveAt(i);
            }
            Mess = 0;
        }

        public void CleanMessAt(Transform t)
        {
            int idx = messObjs.IndexOf(t);
            if (idx < 0) return;
            Destroy(t.gameObject);
            messObjs.RemoveAt(idx);
            Mess = messObjs.Count;
            Economy.Instance.Add(CleanReward);
        }

        // ---- navigation ----
        public void RebuildNav()
        {
            Physics.SyncTransforms();
            Nav = new NavGrid(new Vector3(-13f, 0, -13f), new Vector3(13f, 0, 13f), 0.5f); // square, fits the shop at any rotation
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
                foreach (var c in Checkout.GetComponentsInChildren<Collider>())
                    if (c.enabled) Nav.BlockBounds(c.bounds, 0.4f);
            }

            // there is no floor outside the pad, so confine the walkable area to it
            float hw = FloorWidth / 2f - 0.6f, hd = FloorDepth / 2f - 0.6f;
            Nav.KeepOnly(world =>
            {
                Vector3 lp = ShopRoot != null ? ShopRoot.InverseTransformPoint(world) : world;
                return Mathf.Abs(lp.x - FloorCenter.x) <= hw && Mathf.Abs(lp.z - FloorCenter.z) <= hd;
            });
        }

        // ---- store floor ----
        public void ApplyFloorSize()
        {
            if (Pad != null)
            {
                Pad.localScale = new Vector3(FloorWidth, 0.3f, FloorDepth);
                Pad.localPosition = new Vector3(FloorCenter.x, -0.15f, FloorCenter.z);
            }
            if (PadRim != null)
            {
                PadRim.localScale = new Vector3(FloorWidth + 0.5f, 0.2f, FloorDepth + 0.5f);
                PadRim.localPosition = new Vector3(FloorCenter.x, -0.22f, FloorCenter.z);
            }
        }

        public void ApplyShopRotation()
        {
            if (ShopRoot != null) ShopRoot.localRotation = Quaternion.Euler(0f, ShopRotation, 0f);
        }

        Vector3 ShopLocal(Vector3 world) => ShopRoot != null ? ShopRoot.InverseTransformPoint(world) : world;
        Vector3 ShopWorld(Vector3 local) => ShopRoot != null ? ShopRoot.TransformPoint(local) : local;

        // Clamp a world point to the (rotated) store floor, keeping its height.
        public Vector3 ClampToFloor(Vector3 world)
        {
            Vector3 lp = ShopLocal(world);
            float hw = FloorWidth / 2f - 0.6f, hd = FloorDepth / 2f - 0.6f;
            lp.x = Mathf.Clamp(lp.x, FloorCenter.x - hw, FloorCenter.x + hw);
            lp.z = Mathf.Clamp(lp.z, FloorCenter.z - hd, FloorCenter.z + hd);
            return ShopWorld(lp);
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
            d.shopRotation = ShopRotation;
            d.unlocked = UnlockedItems;
            if (Checkout != null) d.checkout = Fix(Checkout.transform);
            foreach (var s in Shelves)
                if (s != null) { var xz = Fix(s.transform); xz.id = s.catalogId; d.shelves.Add(xz); }
            foreach (var dv in Dividers) if (dv != null) d.dividers.Add(Fix(dv));
            foreach (var de in DecorItems)
                if (de != null) { var xz = Fix(de); var dc = de.GetComponent<Decor>(); xz.id = dc != null ? dc.catalogId : ""; d.decor.Add(xz); }
            PlayerPrefs.SetString("storeLayout", JsonUtility.ToJson(d));
        }

        XZ Fix(Transform t)
        {
            var xz = new XZ(ShopLocal(t.position));
            xz.rot = t.localEulerAngles.y;
            return xz;
        }

        public void LoadLayout()
        {
            if (!PlayerPrefs.HasKey("storeLayout")) return;
            try
            {
                var d = JsonUtility.FromJson<LayoutData>(PlayerPrefs.GetString("storeLayout"));
                if (d == null) return;

                ShopRotation = d.shopRotation;
                ApplyShopRotation();
                UnlockedItems = Mathf.Max(1, d.unlocked);

                if (Checkout != null && d.checkout != null)
                {
                    float y = ShopLocal(Checkout.transform.position).y;
                    Checkout.transform.position = ShopWorld(new Vector3(d.checkout.x, y, d.checkout.z));
                    Checkout.transform.localRotation = Quaternion.Euler(0, d.checkout.rot, 0);
                }

                // shelves: clear + rebuild each from its saved catalog type
                foreach (var s in Shelves) if (s != null) Destroy(s.gameObject);
                Shelves.Clear();
                foreach (var xz in d.shelves)
                {
                    var it = Catalog.ById(string.IsNullOrEmpty(xz.id) ? "st_basic" : xz.id);
                    var sh = AddStand(it, ShopWorld(new Vector3(xz.x, 0f, xz.z)));
                    if (sh != null) sh.transform.localRotation = Quaternion.Euler(0, xz.rot, 0);
                }

                foreach (var dv in Dividers) if (dv != null) Destroy(dv.gameObject);
                Dividers.Clear();
                foreach (var x in d.dividers)
                {
                    var dv = AddDivider(ShopWorld(new Vector3(x.x, 0.5f, x.z)));
                    dv.localRotation = Quaternion.Euler(0, x.rot, 0);
                }

                foreach (var de in DecorItems) if (de != null) Destroy(de.gameObject);
                DecorItems.Clear();
                if (d.decor != null)
                    foreach (var xz in d.decor)
                    {
                        var de = AddDecor(Catalog.ById(xz.id), ShopWorld(new Vector3(xz.x, 0f, xz.z)));
                        if (de != null) de.localRotation = Quaternion.Euler(0, xz.rot, 0);
                    }
            }
            catch { /* corrupt layout -> keep authored default */ }
        }

        public void SaveAll()
        {
            if (Economy.Instance != null)
            {
                double inc = EstIncomePerSec();
                if (ProducerEconomy.Instance != null) inc += ProducerEconomy.Instance.IncomePerSec;
                Economy.Instance.Save(inc);
            }
            SaveLevels();
            SaveLayout();
            PlayerPrefs.SetInt("day", Day);
            PlayerPrefs.Save();
        }

        void OnApplicationQuit() { SaveAll(); }
        void OnApplicationPause(bool paused) { if (paused) SaveAll(); }
    }
}
