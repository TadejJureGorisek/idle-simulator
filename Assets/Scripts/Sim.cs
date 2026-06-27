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
        [System.NonSerialized] public HashSet<string> UnlockedSections = new HashSet<string> { "common" };
        [System.NonSerialized] public Dictionary<string, int> SectionLevel = new Dictionary<string, int>(); // investment per section
        public string ActiveSection = "common"; // floor-paint brush / section selected in edit mode
        [System.NonSerialized] public Dictionary<int, string> FloorPaint = new Dictionary<int, string>(); // cell -> section
        Transform floorPaintLayer;
        readonly Dictionary<int, GameObject> floorCells = new Dictionary<int, GameObject>();
        readonly Dictionary<string, Material> paintMats = new Dictionary<string, Material>();
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
            LoadSections();
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
        public Shelf AddStand(CatalogItem it, Vector3 pos, string section = "common")
        {
            var go = Catalog.Build(it);
            if (ShelfParent != null) go.transform.SetParent(ShelfParent);
            go.transform.position = pos;
            var shelf = go.GetComponent<Shelf>();
            if (shelf != null) { shelf.section = section; ApplySectionColor(shelf); Shelves.Add(shelf); }
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

        // ---- sections / departments ----
        public bool IsSectionUnlocked(string id) => UnlockedSections.Contains(id);

        public bool UnlockSection(string id)
        {
            if (IsSectionUnlocked(id)) return false;
            var sec = Sections.ById(id);
            if (Economy.Instance == null || !Economy.Instance.TrySpend(sec.unlockCost)) return false;
            UnlockedSections.Add(id);
            SaveSections();
            return true;
        }

        // ---- section investment / value ----
        public int GetSectionLevel(string id) => SectionLevel.TryGetValue(id, out var l) ? l : 0;
        public double SectionMult(string id) => Sections.ById(id).valueMult * System.Math.Pow(1.15, GetSectionLevel(id));
        public double ItemValue(string id) => Profit * SectionMult(id);           // $ a single item of this section yields
        public double SectionUpgradeCost(string id) => Sections.ById(id).upgradeBase * System.Math.Pow(1.15, GetSectionLevel(id));

        public bool UpgradeSection(string id)
        {
            if (!IsSectionUnlocked(id)) return false;
            double cost = SectionUpgradeCost(id);
            if (Economy.Instance == null || !Economy.Instance.TrySpend(cost)) return false;
            SectionLevel[id] = GetSectionLevel(id) + 1;
            SaveSections();
            return true;
        }

        // invest one level into every unlocked section you can currently afford (left -> right)
        public void UpgradeAllSections()
        {
            foreach (var s in Sections.All)
                if (IsSectionUnlocked(s.id)) UpgradeSection(s.id);
        }

        public double AvgSectionMult()
        {
            double sum = 0; int n = 0;
            foreach (var s in Shelves) if (s != null) { sum += SectionMult(s.section); n++; }
            return n > 0 ? sum / n : 1.0;
        }

        // a shelf belongs to whatever section the floor cell beneath it is painted (else "common")
        public string SectionAt(Vector3 world)
        {
            if (WorldToCell(world, out int i, out int j) && FloorPaint.TryGetValue(CellKey(i, j), out var sec)) return sec;
            return "common";
        }

        public void RefreshShelfSection(Shelf s)
        {
            if (s == null) return;
            s.section = SectionAt(s.transform.position);
            ApplySectionColor(s);
        }

        public void RefreshAllShelfSections()
        {
            foreach (var s in Shelves) RefreshShelfSection(s);
        }

        public void CycleActiveSection()
        {
            var open = Sections.All.FindAll(s => IsSectionUnlocked(s.id));
            if (open.Count == 0) { ActiveSection = "common"; return; }
            int idx = open.FindIndex(s => s.id == ActiveSection);
            ActiveSection = open[(idx + 1) % open.Count].id;
        }

        public static void ApplySectionColor(Shelf s)
        {
            if (s == null) return;
            var body = s.transform.Find("Body");
            if (body == null) return;
            var r = body.GetComponent<Renderer>();
            if (r != null) r.material.color = Sections.ById(s.section).color;
        }

        void SaveSections()
        {
            foreach (var s in Sections.All)
            {
                PlayerPrefs.SetInt("sec_" + s.id, UnlockedSections.Contains(s.id) ? 1 : 0);
                PlayerPrefs.SetInt("seclvl_" + s.id, GetSectionLevel(s.id));
            }
        }

        void LoadSections()
        {
            UnlockedSections.Clear();
            UnlockedSections.Add("common");
            SectionLevel.Clear();
            foreach (var s in Sections.All)
            {
                if (PlayerPrefs.GetInt("sec_" + s.id, 0) == 1) UnlockedSections.Add(s.id);
                int lv = PlayerPrefs.GetInt("seclvl_" + s.id, 0);
                if (lv > 0) SectionLevel[s.id] = lv;
            }
        }

        // ---- floor painting (section zones) ----
        int CellKey(int i, int j) => j * 1000 + i;

        public bool WorldToCell(Vector3 world, out int i, out int j)
        {
            Vector3 local = ShopLocal(world);
            int W = Mathf.RoundToInt(FloorWidth), D = Mathf.RoundToInt(FloorDepth);
            i = Mathf.FloorToInt(local.x - FloorCenter.x + W / 2f);
            j = Mathf.FloorToInt(local.z - FloorCenter.z + D / 2f);
            return i >= 0 && i < W && j >= 0 && j < D;
        }

        Vector3 CellCenterLocal(int i, int j)
        {
            int W = Mathf.RoundToInt(FloorWidth), D = Mathf.RoundToInt(FloorDepth);
            return new Vector3(FloorCenter.x - W / 2f + i + 0.5f, 0.02f, FloorCenter.z - D / 2f + j + 0.5f);
        }

        void EnsureFloorLayer()
        {
            if (floorPaintLayer != null) return;
            var go = new GameObject("FloorPaint");
            if (ShopRoot != null) go.transform.SetParent(ShopRoot, false);
            floorPaintLayer = go.transform;
        }

        Material PaintMat(string sectionId)
        {
            if (!paintMats.TryGetValue(sectionId, out var m) || m == null)
            {
                m = new Material(Shader.Find("Sprites/Default")); // unlit, double-sided, alpha
                var c = Sections.ById(sectionId).color;
                m.color = new Color(c.r, c.g, c.b, 0.55f);
                paintMats[sectionId] = m;
            }
            return m;
        }

        public void PaintFloorAtWorld(Vector3 world, string sectionId)
        {
            if (WorldToCell(world, out int i, out int j)) PaintCell(i, j, sectionId);
        }

        // sectionId == null  ->  erase the cell
        public void PaintCell(int i, int j, string sectionId)
        {
            int key = CellKey(i, j);
            if (string.IsNullOrEmpty(sectionId))
            {
                if (floorCells.TryGetValue(key, out var g) && g != null) Destroy(g);
                floorCells.Remove(key);
                FloorPaint.Remove(key);
                return;
            }
            FloorPaint[key] = sectionId;
            EnsureFloorLayer();
            if (!floorCells.TryGetValue(key, out var cell) || cell == null)
            {
                cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cell.name = "Cell_" + i + "_" + j;
                var col = cell.GetComponent<Collider>(); if (col != null) Destroy(col);
                cell.transform.SetParent(floorPaintLayer, false);
                cell.transform.localRotation = Quaternion.Euler(90f, 0, 0);
                cell.transform.localScale = Vector3.one;
                cell.transform.localPosition = CellCenterLocal(i, j);
                floorCells[key] = cell;
            }
            cell.GetComponent<Renderer>().sharedMaterial = PaintMat(sectionId);
        }

        void ClearFloorPaint()
        {
            foreach (var kv in floorCells) if (kv.Value != null) Destroy(kv.Value);
            floorCells.Clear();
            FloorPaint.Clear();
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
            return Math.Min(serveRate, spawnRate) * Profit * AvgBasket * AvgSectionMult();
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
                if (s != null) { var xz = Fix(s.transform); xz.id = s.catalogId; xz.sec = s.section; d.shelves.Add(xz); }
            foreach (var dv in Dividers) if (dv != null) d.dividers.Add(Fix(dv));
            foreach (var de in DecorItems)
                if (de != null) { var xz = Fix(de); var dc = de.GetComponent<Decor>(); xz.id = dc != null ? dc.catalogId : ""; d.decor.Add(xz); }
            foreach (var kv in FloorPaint)
                d.paint.Add(new PaintCell { i = kv.Key % 1000, j = kv.Key / 1000, sec = kv.Value });
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
                    var sh = AddStand(it, ShopWorld(new Vector3(xz.x, 0f, xz.z)), string.IsNullOrEmpty(xz.sec) ? "common" : xz.sec);
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

                ClearFloorPaint();
                if (d.paint != null)
                    foreach (var pc in d.paint) PaintCell(pc.i, pc.j, pc.sec);

                RefreshAllShelfSections(); // shelves take the section of the floor they stand on
            }
            catch { /* corrupt layout -> keep authored default */ }
        }

        public void SaveAll()
        {
            if (Economy.Instance != null)
            {
                double inc = EstIncomePerSec();
                Economy.Instance.Save(inc);
            }
            SaveLevels();
            SaveSections();
            SaveLayout();
            PlayerPrefs.SetInt("day", Day);
            PlayerPrefs.Save();
        }

        void OnApplicationQuit() { SaveAll(); }
        void OnApplicationPause(bool paused) { if (paused) SaveAll(); }
    }
}
