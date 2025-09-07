using System.Collections.Generic;
using UnityEngine;

// quick A* test for grid-based pathfinding inside a room
// uses BFS for marking wall cells (from the grid) and also promotes staying away from the walls when moving
// the heuristic used is euclidean
public class RoomPathfindInstance
{
    // input
    private List<Vector2> polygon;
    private Vector2 start;
    private Vector2 end;
    private float cellSize;
    private float maxClearance;
    private float penaltyFactor;

    // precomputed
    private Bounds bounds;
    private Vector2Int gridSize;
    private CellNode[,] grid;
    private float[,] distanceField;
    private Vector2Int startCell;
    private Vector2Int endCell;

    public List<Vector2> finishedPath;

    public RoomPathfindInstance(List<Vector2> polygon, Vector2 start, Vector2 end, float cellSize, float maxClearance, float penaltyFactor)
    {
        this.polygon = polygon;
        this.start = start;
        this.end = end;
        this.cellSize = cellSize;
        this.maxClearance = maxClearance;
        this.penaltyFactor = penaltyFactor;

        CalculateInitialParameters();
    }

    private void CalculateInitialParameters()
    {
        // bounds
        bounds = new Bounds(polygon[0], Vector3.zero);
        for (int i = 1; i < polygon.Count; i++) bounds.Encapsulate(polygon[i]);

        gridSize = new Vector2Int(
            Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / cellSize)),
            Mathf.Max(1, Mathf.CeilToInt(bounds.size.y / cellSize))
        );

        // grid
        grid = new CellNode[gridSize.x, gridSize.y];
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2 p = CellToWorld(new Vector2Int(x, y));
                bool walk = PointInPolygon(p, polygon);
                grid[x, y] = new CellNode(new Vector2Int(x, y), walk);
            }
        }

        // distance to walls (seed from non-walkable)
        distanceField = ComputeDistanceFieldBFS();

        // start/end cells (clamped + snap to nearest walkable)
        startCell = ClampToGrid(WorldToGrid(start));
        endCell = ClampToGrid(WorldToGrid(end));

        startCell = EnsureWalkable(startCell);
        endCell = EnsureWalkable(endCell);
        if (startCell.x < 0 || endCell.x < 0)
        {
            Debug.LogWarning("RoomPathfindInstance: start or end not walkable.");
        }
    }

    public void FindPath()
    {
        finishedPath = null;
        if (grid == null || startCell.x < 0 || endCell.x < 0) return;

        if (startCell == endCell)
        {
            finishedPath = new List<Vector2> { start, end };
            return;
        }

        // A*
        SimpleNodeQueue<CellNode> open = new SimpleNodeQueue<CellNode>(n => n.f);
        List<CellNode> closed = new List<CellNode>();

        CellNode startNode = grid[startCell.x, startCell.y];
        CellNode endNode   = grid[endCell.x, endCell.y];

        startNode.g = 0f;
        startNode.h = Vector2Int.Distance(startNode.pos, endNode.pos);
        startNode.f = startNode.h;
        open.Enqueue(startNode);

        int safety = 0;
        while (open.IsNotEmpty())
        {
            CellNode current = open.Dequeue();
            if (current == endNode)
            {
                FinishPath(endNode);
                return;
            }

            closed.Add(current);

            foreach (CellNode n in GetNeighbors(current))
            {
                if (!n.walkable || closed.Contains(n)) continue;

                // prevent diagonal corner cutting
                int dx = n.pos.x - current.pos.x;
                int dy = n.pos.y - current.pos.y;
                if (dx != 0 && dy != 0)
                {
                    int ax = current.pos.x + dx;
                    int ay = current.pos.y;
                    int bx = current.pos.x;
                    int by = current.pos.y + dy;
                    if (!grid[ax, ay].walkable || !grid[bx, by].walkable) continue;
                }

                float step = Vector2Int.Distance(current.pos, n.pos); // 1 or sqrt(2)
                float dWall = distanceField[n.pos.x, n.pos.y];
                float penalty = Mathf.Max(0, (maxClearance - dWall) * penaltyFactor);

                float tentativeG = current.g + step + penalty;
                if (tentativeG < n.g)
                {
                    n.parent = current;
                    n.g = tentativeG;
                    n.h = Vector2Int.Distance(n.pos, endNode.pos);
                    n.f = n.g + n.h;

                    if (!open.Contains(n)) open.Enqueue(n);
                    else open.UpdatePriority(n);
                }
            }

            safety++;
            if (safety > 200000)
            {
                Debug.LogWarning("RoomPathfindInstance: safety break.");
                break;
            }
        }

        Debug.Log("RoomPathfindInstance: no path found.");
    }

    private void FinishPath(CellNode endNode)
    {
        List<Vector2> path = new List<Vector2>();
        CellNode c = endNode;
        while (c != null)
        {
            path.Add(CellToWorld(c.pos));
            c = c.parent;
        }
        path.Reverse();
        finishedPath = path;
    }

    // helpers

    private Vector2Int WorldToGrid(Vector2 w)
    {
        return new Vector2Int(
            Mathf.FloorToInt((w.x - bounds.min.x) / cellSize),
            Mathf.FloorToInt((w.y - bounds.min.y) / cellSize)
        );
    }

    private Vector2 CellToWorld(Vector2Int cell)
    {
        return new Vector2(
            bounds.min.x + (cell.x + 0.5f) * cellSize,
            bounds.min.y + (cell.y + 0.5f) * cellSize
        );
    }

    private Vector2Int ClampToGrid(Vector2Int c)
    {
        return new Vector2Int(
            Mathf.Clamp(c.x, 0, gridSize.x - 1),
            Mathf.Clamp(c.y, 0, gridSize.y - 1)
        );
    }

    private Vector2Int EnsureWalkable(Vector2Int from)
    {
        if (grid[from.x, from.y].walkable) return from;

        Queue<Vector2Int> q = new Queue<Vector2Int>();
        bool[,] seen = new bool[gridSize.x, gridSize.y];
        q.Enqueue(from);
        seen[from.x, from.y] = true;

        Vector2Int[] dirs = {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1),
            new Vector2Int(1,1), new Vector2Int(1,-1),
            new Vector2Int(-1,1), new Vector2Int(-1,-1)
        };

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            foreach (var d in dirs)
            {
                int nx = c.x + d.x, ny = c.y + d.y;
                if (nx < 0 || ny < 0 || nx >= gridSize.x || ny >= gridSize.y || seen[nx, ny]) continue;
                if (grid[nx, ny].walkable) return new Vector2Int(nx, ny);
                seen[nx, ny] = true;
                q.Enqueue(new Vector2Int(nx, ny));
            }
        }

        return new Vector2Int(-1, -1); // not found
    }

    private IEnumerable<CellNode> GetNeighbors(CellNode n)
    {
        Vector2Int[] dirs = {
            new Vector2Int(1,0), new Vector2Int(-1,0),
            new Vector2Int(0,1), new Vector2Int(0,-1),
            new Vector2Int(1,1), new Vector2Int(1,-1),
            new Vector2Int(-1,1), new Vector2Int(-1,-1)
        };
        for (int i = 0; i < dirs.Length; i++)
        {
            int nx = n.pos.x + dirs[i].x;
            int ny = n.pos.y + dirs[i].y;
            if (nx >= 0 && ny >= 0 && nx < gridSize.x && ny < gridSize.y)
                yield return grid[nx, ny];
        }
    }

    private float[,] ComputeDistanceFieldBFS()
    {
        int w = gridSize.x, h = gridSize.y;
        float[,] dist = new float[w, h];
        bool[,] visited = new bool[w, h];
        Queue<CellNode> q = new Queue<CellNode>();

        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            if (!grid[x, y].walkable)
            {
                dist[x, y] = 0f;
                visited[x, y] = true;
                q.Enqueue(grid[x, y]);
            }
            else
            {
                dist[x, y] = float.PositiveInfinity;
            }
        }

        Vector2Int[] dirs = {
            new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1),
            new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1,-1)
        };

        while (q.Count > 0)
        {
            CellNode cur = q.Dequeue();
            float cd = dist[cur.pos.x, cur.pos.y];

            for (int i = 0; i < dirs.Length; i++)
            {
                int nx = cur.pos.x + dirs[i].x, ny = cur.pos.y + dirs[i].y;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h || visited[nx, ny]) continue;

                float step = (dirs[i].x != 0 && dirs[i].y != 0) ? cellSize * Mathf.Sqrt(2f) : cellSize;
                dist[nx, ny] = cd + step;
                visited[nx, ny] = true;
                q.Enqueue(grid[nx, ny]);
            }
        }

        return dist;
    }

    private bool PointInPolygon(Vector2 p, List<Vector2> poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector2 pi = poly[i]; Vector2 pj = poly[j];
            if (((pi.y > p.y) != (pj.y > p.y)) &&
                (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y + Mathf.Epsilon) + pi.x))
                inside = !inside;
        }
        return inside;
    }

    // inner cell node
    private class CellNode
    {
        public Vector2Int pos;
        public bool walkable;
        public float g = float.PositiveInfinity;
        public float h;
        public float f;
        public CellNode parent;

        public CellNode(Vector2Int pos, bool walkable)
        {
            this.pos = pos;
            this.walkable = walkable;
        }
    }
}
