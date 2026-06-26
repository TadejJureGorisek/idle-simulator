using System.Collections.Generic;
using UnityEngine;

namespace IdleSim
{
    // Lightweight 4-direction grid + A* (with a turn penalty) for orthogonal customer
    // routing around static obstacles. Customers are NOT on the grid, so they overlap freely.
    public class NavGrid
    {
        readonly float cell;
        readonly Vector3 origin; // min corner (x,z)
        readonly int w, h;
        readonly bool[,] blocked;

        public NavGrid(Vector3 min, Vector3 max, float cellSize)
        {
            cell = cellSize;
            origin = new Vector3(min.x, 0, min.z);
            w = Mathf.Max(1, Mathf.CeilToInt((max.x - min.x) / cell));
            h = Mathf.Max(1, Mathf.CeilToInt((max.z - min.z) / cell));
            blocked = new bool[w, h];
        }

        public void BlockBounds(Bounds b, float pad)
        {
            int x0 = WorldToX(b.min.x - pad), x1 = WorldToX(b.max.x + pad);
            int z0 = WorldToZ(b.min.z - pad), z1 = WorldToZ(b.max.z + pad);
            for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++)
                for (int z = Mathf.Max(0, z0); z <= Mathf.Min(h - 1, z1); z++)
                    blocked[x, z] = true;
        }

        // Block every cell whose centre is not inside the test (e.g. the floating pad).
        public void KeepOnly(System.Func<Vector3, bool> insideWorld)
        {
            for (int x = 0; x < w; x++)
                for (int z = 0; z < h; z++)
                    if (!blocked[x, z] && !insideWorld(CellToWorld(new Vector2Int(x, z), 0f)))
                        blocked[x, z] = true;
        }

        int WorldToX(float wx) => Mathf.FloorToInt((wx - origin.x) / cell);
        int WorldToZ(float wz) => Mathf.FloorToInt((wz - origin.z) / cell);
        bool InBounds(int x, int z) => x >= 0 && x < w && z >= 0 && z < h;
        bool Walkable(int x, int z) => InBounds(x, z) && !blocked[x, z];

        Vector2Int WorldToCell(Vector3 p) =>
            new Vector2Int(Mathf.Clamp(WorldToX(p.x), 0, w - 1), Mathf.Clamp(WorldToZ(p.z), 0, h - 1));

        Vector3 CellToWorld(Vector2Int c, float y) =>
            new Vector3(origin.x + (c.x + 0.5f) * cell, y, origin.z + (c.y + 0.5f) * cell);

        Vector2Int NearestWalkable(Vector2Int c)
        {
            if (Walkable(c.x, c.y)) return c;
            int max = Mathf.Max(w, h);
            for (int r = 1; r < max; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dz = -r; dz <= r; dz++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dz) != r) continue; // ring only
                        if (Walkable(c.x + dx, c.y + dz)) return new Vector2Int(c.x + dx, c.y + dz);
                    }
            return c;
        }

        static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        // Returns world waypoints (collapsed at corners) or null if unreachable.
        public List<Vector3> FindPath(Vector3 start, Vector3 goal, float y)
        {
            Vector2Int s = NearestWalkable(WorldToCell(start));
            Vector2Int g = NearestWalkable(WorldToCell(goal));
            if (s == g) return new List<Vector3> { CellToWorld(g, y) };

            var came = new Dictionary<Vector2Int, Vector2Int>();
            var dirOf = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { [s] = 0 };
            var fScore = new Dictionary<Vector2Int, float> { [s] = Heur(s, g) };
            var open = new List<Vector2Int> { s };
            var closed = new HashSet<Vector2Int>();
            dirOf[s] = Vector2Int.zero;

            int guard = 0;
            while (open.Count > 0 && guard++ < 40000)
            {
                int bi = 0;
                for (int i = 1; i < open.Count; i++)
                    if (fScore[open[i]] < fScore[open[bi]]) bi = i;
                var cur = open[bi];
                open.RemoveAt(bi);
                if (cur == g) return Reconstruct(came, cur, y);
                closed.Add(cur);

                foreach (var d in Dirs)
                {
                    var nb = new Vector2Int(cur.x + d.x, cur.y + d.y);
                    if (!Walkable(nb.x, nb.y) || closed.Contains(nb)) continue;
                    float turn = (dirOf[cur] != Vector2Int.zero && dirOf[cur] != d) ? 0.7f : 0f;
                    float tentative = gScore[cur] + 1f + turn;
                    if (!gScore.TryGetValue(nb, out float gPrev) || tentative < gPrev)
                    {
                        came[nb] = cur;
                        dirOf[nb] = d;
                        gScore[nb] = tentative;
                        fScore[nb] = tentative + Heur(nb, g);
                        if (!open.Contains(nb)) open.Add(nb);
                    }
                }
            }
            return null;
        }

        static float Heur(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        List<Vector3> Reconstruct(Dictionary<Vector2Int, Vector2Int> came, Vector2Int cur, float y)
        {
            var cells = new List<Vector2Int> { cur };
            while (came.ContainsKey(cur)) { cur = came[cur]; cells.Add(cur); }
            cells.Reverse();

            var pts = new List<Vector3>();
            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0 && i < cells.Count - 1 && (cells[i] - cells[i - 1]) == (cells[i + 1] - cells[i]))
                    continue; // collinear -> only keep corners
                pts.Add(CellToWorld(cells[i], y));
            }
            return pts;
        }
    }
}
