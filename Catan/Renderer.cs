namespace Catan;

// Draws the board as text. Vertex numbers are shown at every corner;
// buildings replace them (S2 = Player 2 settlement, C2 = Player 2 city).
// Roads show the owner's number on the edge.
public static class Renderer
{
    private const int Margin = 2;

    public static void DrawBoard(Game game)
    {
        var board = game.Board;
        int width = board.Vertices.Max(v => v.X) + Margin + 3;
        int height = board.Vertices.Max(v => v.Y) + 1;
        var chars = new char[height, width];
        var colors = new ConsoleColor?[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                chars[y, x] = ' ';

        void Put(int x, int y, string text, ConsoleColor? color)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int cx = x + Margin + i;
                if (cx < 0 || cx >= width || y < 0 || y >= height) continue;
                chars[y, cx] = text[i];
                colors[y, cx] = color;
            }
        }

        foreach (var e in board.Edges)
        {
            var a = board.Vertices[e.A];
            var b = board.Vertices[e.B];
            if (a.Y > b.Y) (a, b) = (b, a);
            ConsoleColor? color = e.Owner >= 0 ? game.Players[e.Owner].Color : ConsoleColor.DarkGray;
            string owner = e.Owner >= 0 ? (e.Owner + 1).ToString() : "";
            if (a.X == b.X)
            {
                Put(a.X, a.Y + 1, e.Owner >= 0 ? owner : "|", color);
            }
            else
            {
                char slash = b.X > a.X ? '\\' : '/';
                int mid = (a.X + b.X - 1) / 2;
                if (e.Owner >= 0) Put(mid - 1, a.Y + 1, $"{slash}{owner}{slash}", color);
                else Put(mid, a.Y + 1, slash.ToString(), color);
            }
        }

        foreach (var v in board.Vertices)
        {
            if (v.Owner >= 0)
                Put(v.X - 1, v.Y, (v.IsCity ? "C" : "S") + (v.Owner + 1), game.Players[v.Owner].Color);
            else
                Put(v.X - 1, v.Y, v.Id.ToString("D2"), ConsoleColor.Gray);
        }

        foreach (var h in board.Hexes)
        {
            var color = TerrainColor(h.Terrain);
            string name = h.Terrain == Terrain.Desert ? "Desert" : ResourceInfo.FromTerrain(h.Terrain).ToString()!;
            bool robber = board.RobberHex == h.Id;
            string label = robber ? "ROBBER" : $"[{h.Label}]";
            Put(h.X - label.Length / 2, h.Y - 1, label, robber ? ConsoleColor.Red : ConsoleColor.White);
            Put(h.X - name.Length / 2, h.Y, name, color);
            if (h.Number > 0)
            {
                // Number plus one dot per pip: more dots = rolled more often.
                string odds = $"{h.Number} {new string('.', ResourceInfo.Pips(h.Number))}";
                bool red = h.Number == 6 || h.Number == 8;
                Put(h.X - odds.Length / 2, h.Y + 1, odds, red ? ConsoleColor.Red : ConsoleColor.White);
            }
        }

        bool useColor = !Console.IsOutputRedirected;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (useColor)
                    Console.ForegroundColor = colors[y, x] ?? ConsoleColor.Gray;
                Console.Write(chars[y, x]);
            }
            if (useColor) Console.ResetColor();
            Console.WriteLine();
        }
    }

    public static void DrawScores(Game game)
    {
        foreach (var p in game.Players)
        {
            WriteColored($"  P{p.Number} {p.Name,-6}", p.Color);
            string extra = p.HasLongestRoad ? "  [Longest Road]" : "";
            Console.WriteLine($" {p.VictoryPoints,2} VP  cards:{p.CardCount,2}  road length:{game.LongestRoad(p),2}{extra}");
        }
    }

    public static void WriteColored(string text, ConsoleColor color)
    {
        if (!Console.IsOutputRedirected) Console.ForegroundColor = color;
        Console.Write(text);
        if (!Console.IsOutputRedirected) Console.ResetColor();
    }

    private static ConsoleColor TerrainColor(Terrain t) => t switch
    {
        Terrain.Forest => ConsoleColor.DarkGreen,
        Terrain.Hills => ConsoleColor.DarkRed,
        Terrain.Pasture => ConsoleColor.Green,
        Terrain.Fields => ConsoleColor.Yellow,
        Terrain.Mountains => ConsoleColor.DarkCyan,
        _ => ConsoleColor.DarkYellow
    };
}
