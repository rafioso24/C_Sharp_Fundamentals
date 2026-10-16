namespace Catan;

public class Hex
{
    public int Id { get; init; }
    public Terrain Terrain { get; set; }
    public int Number { get; set; }      // 0 for the desert
    public int X { get; init; }          // center position on the drawn map
    public int Y { get; init; }
    public int[] Corners { get; } = new int[6];
    public char Label => (char)('A' + Id);
}

public class Vertex
{
    public int Id { get; set; }
    public int X { get; init; }
    public int Y { get; init; }
    public List<int> Hexes { get; } = new();
    public List<int> Neighbors { get; } = new();
    public List<int> Edges { get; } = new();
    public int Owner { get; set; } = -1;  // player id, -1 = empty
    public bool IsCity { get; set; }
}

public class Edge
{
    public int Id { get; init; }
    public int A { get; init; }
    public int B { get; init; }
    public int Owner { get; set; } = -1;
    public int Other(int v) => v == A ? B : A;
}

// The standard 19-hex board. Hexes are "pointy top" and laid out in rows of 3-4-5-4-3.
// Vertex and edge graphs are built from where the corners land on the drawn map,
// so the map and the rules always agree.
public class Board
{
    // Corner offsets from a hex center: top-left, top, top-right, bottom-right, bottom, bottom-left.
    private static readonly (int dx, int dy)[] CornerOffsets =
        { (-6, -1), (0, -3), (6, -1), (6, 1), (0, 3), (-6, 1) };

    public List<Hex> Hexes { get; } = new();
    public List<Vertex> Vertices { get; } = new();
    public List<Edge> Edges { get; } = new();
    public int RobberHex { get; set; }

    public Board(Random rng)
    {
        BuildGeometry();
        AssignTerrainAndNumbers(rng);
    }

    private void BuildGeometry()
    {
        int[] rowSizes = { 3, 4, 5, 4, 3 };
        for (int row = 0; row < rowSizes.Length; row++)
        {
            int startX = 8 + 6 * Math.Abs(row - 2);
            for (int i = 0; i < rowSizes[row]; i++)
                Hexes.Add(new Hex { Id = Hexes.Count, X = startX + 12 * i, Y = 3 + 4 * row });
        }

        // Find every distinct corner position and number them top-to-bottom, left-to-right.
        var positions = Hexes
            .SelectMany(h => CornerOffsets.Select(o => (x: h.X + o.dx, y: h.Y + o.dy)))
            .Distinct()
            .OrderBy(p => p.y).ThenBy(p => p.x)
            .ToList();
        var byPos = new Dictionary<(int, int), Vertex>();
        foreach (var (x, y) in positions)
        {
            var v = new Vertex { Id = Vertices.Count, X = x, Y = y };
            Vertices.Add(v);
            byPos[(x, y)] = v;
        }

        foreach (var hex in Hexes)
        {
            for (int c = 0; c < 6; c++)
            {
                var v = byPos[(hex.X + CornerOffsets[c].dx, hex.Y + CornerOffsets[c].dy)];
                hex.Corners[c] = v.Id;
                v.Hexes.Add(hex.Id);
            }
            for (int c = 0; c < 6; c++)
            {
                int a = hex.Corners[c], b = hex.Corners[(c + 1) % 6];
                if (FindEdge(a, b) != null) continue;
                var e = new Edge { Id = Edges.Count, A = a, B = b };
                Edges.Add(e);
                Vertices[a].Edges.Add(e.Id);
                Vertices[b].Edges.Add(e.Id);
                Vertices[a].Neighbors.Add(b);
                Vertices[b].Neighbors.Add(a);
            }
        }
    }

    private void AssignTerrainAndNumbers(Random rng)
    {
        var terrains = new List<Terrain>();
        terrains.AddRange(Enumerable.Repeat(Terrain.Forest, 4));
        terrains.AddRange(Enumerable.Repeat(Terrain.Pasture, 4));
        terrains.AddRange(Enumerable.Repeat(Terrain.Fields, 4));
        terrains.AddRange(Enumerable.Repeat(Terrain.Hills, 3));
        terrains.AddRange(Enumerable.Repeat(Terrain.Mountains, 3));
        terrains.Add(Terrain.Desert);
        int[] numbers = { 2, 3, 3, 4, 4, 5, 5, 6, 6, 8, 8, 9, 9, 10, 10, 11, 11, 12 };

        // Reshuffle until no two red numbers (6 and 8) touch, like the real game.
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            Shuffle(terrains, rng);
            var nums = numbers.ToList();
            Shuffle(nums, rng);
            int n = 0;
            for (int i = 0; i < Hexes.Count; i++)
            {
                Hexes[i].Terrain = terrains[i];
                Hexes[i].Number = terrains[i] == Terrain.Desert ? 0 : nums[n++];
            }
            if (!RedNumbersTouch()) break;
        }
        RobberHex = Hexes.First(h => h.Terrain == Terrain.Desert).Id;
    }

    private bool RedNumbersTouch()
    {
        foreach (var a in Hexes)
            foreach (var b in Hexes)
                if (a.Id < b.Id && IsRed(a) && IsRed(b) && a.Corners.Intersect(b.Corners).Any())
                    return true;
        return false;
    }

    private static bool IsRed(Hex h) => h.Number == 6 || h.Number == 8;

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public Edge? FindEdge(int a, int b) =>
        Edges.FirstOrDefault(e => (e.A == a && e.B == b) || (e.A == b && e.B == a));

    public Hex? FindHex(char label)
    {
        int id = char.ToUpperInvariant(label) - 'A';
        return id >= 0 && id < Hexes.Count ? Hexes[id] : null;
    }

    // Sum of dice "pips" around a vertex: a quick measure of how often it produces.
    public int VertexPips(int v) => Vertices[v].Hexes.Sum(h => ResourceInfo.Pips(Hexes[h].Number));

    public string DescribeVertex(int v) =>
        string.Join(", ", Vertices[v].Hexes.Select(h => Hexes[h])
            .Select(h => h.Terrain == Terrain.Desert ? "Desert" : $"{ResourceInfo.FromTerrain(h.Terrain)} {h.Number}"));
}
