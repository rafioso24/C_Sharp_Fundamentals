namespace Catan;

public class Game
{
    public const int WinningPoints = 10;
    public const int MaxTurns = 1000;   // safety stop so a stuck game always ends

    public Board Board { get; }
    public List<Player> Players { get; } = new();
    public Random Rng { get; }
    public int TurnNumber { get; private set; }
    public Player? Winner { get; private set; }

    public Game(Random rng)
    {
        Rng = rng;
        Board = new Board(rng);
    }

    public void Log(string message) => Console.WriteLine(message);

    // ---------------------------------------------------------------- game flow

    public void Run()
    {
        Log("=== Setup: each player places 2 settlements and 2 roads ===");
        var order = Players.Concat(Enumerable.Reverse(Players)).ToList();
        for (int i = 0; i < order.Count; i++)
        {
            var p = order[i];
            bool second = i >= Players.Count;
            int v = p.Controller.ChooseSetupSettlement(this, p);
            PlaceSettlement(p, v);
            Log($"{p} placed a settlement at {v:D2} ({Board.DescribeVertex(v)}).");
            if (second) GiveStartingResources(p, v);
            int e = p.Controller.ChooseSetupRoad(this, p, v);
            PlaceRoad(p, e);
            Log($"{p} placed a road {Board.Edges[e].A:D2}-{Board.Edges[e].B:D2}.");
        }

        while (Winner == null && TurnNumber < MaxTurns)
        {
            var p = Players[TurnNumber % Players.Count];
            TurnNumber++;
            PlayTurn(p);
        }

        Console.WriteLine();
        Renderer.DrawBoard(this);
        Renderer.DrawScores(this);
        Console.WriteLine();
        Log(Winner != null
            ? $"*** {Winner} wins with {Winner.VictoryPoints} points after {TurnNumber} turns! ***"
            : $"Game stopped after {MaxTurns} turns with no winner.");
    }

    private void PlayTurn(Player p)
    {
        Log("");
        Log($"--- Turn {TurnNumber}: {p} ({p.VictoryPoints} VP) ---");
        p.Controller.BeforeRoll(this, p);
        int d1 = Rng.Next(1, 7), d2 = Rng.Next(1, 7);
        int roll = d1 + d2;
        Log($"{p} rolled {d1} + {d2} = {roll}.");
        if (roll == 7) HandleSeven(p);
        else Produce(roll);

        p.Controller.TakeTurn(this, p);
        CheckWinner(p);
    }

    private void GiveStartingResources(Player p, int v)
    {
        foreach (int h in Board.Vertices[v].Hexes)
        {
            var r = ResourceInfo.FromTerrain(Board.Hexes[h].Terrain);
            if (r != null) p.Hand[(int)r.Value]++;
        }
    }

    private void Produce(int roll)
    {
        var gains = Players.ToDictionary(p => p, _ => new int[5]);
        foreach (var hex in Board.Hexes.Where(h => h.Number == roll && h.Id != Board.RobberHex))
        {
            var r = ResourceInfo.FromTerrain(hex.Terrain)!.Value;
            foreach (int c in hex.Corners)
            {
                var v = Board.Vertices[c];
                if (v.Owner < 0) continue;
                gains[Players[v.Owner]][(int)r] += v.IsCity ? 2 : 1;
            }
        }
        bool anyone = false;
        foreach (var (p, g) in gains)
        {
            if (g.Sum() == 0) continue;
            anyone = true;
            for (int i = 0; i < 5; i++) p.Hand[i] += g[i];
            var parts = ResourceInfo.All.Where(r => g[(int)r] > 0).Select(r => $"{g[(int)r]} {r}");
            Log($"  {p} gets {string.Join(", ", parts)}.");
        }
        if (!anyone) Log("  Nobody produces anything.");
    }

    private void HandleSeven(Player mover)
    {
        foreach (var p in Players.Where(p => p.CardCount > 7))
        {
            int count = p.CardCount / 2;
            int[] discard = p.Controller.ChooseDiscard(this, p, count);
            bool valid = discard.Length == 5 && discard.Sum() == count &&
                         discard.Select((n, i) => n >= 0 && n <= p.Hand[i]).All(ok => ok);
            if (!valid) discard = BotController.DefaultDiscard(p, count);
            p.Pay(discard);
            Log($"  {p} discards {count} cards.");
        }

        int hex = mover.Controller.ChooseRobberHex(this, mover);
        if (hex == Board.RobberHex || hex < 0 || hex >= Board.Hexes.Count)
            hex = Board.Hexes.First(h => h.Id != Board.RobberHex).Id;
        Board.RobberHex = hex;
        Log($"  {mover} moves the robber to hex {Board.Hexes[hex].Label}.");

        var victims = RobberVictims(mover, hex);
        if (victims.Count == 0) return;
        var victim = victims.Count == 1 ? victims[0] : mover.Controller.ChooseVictim(this, mover, victims);
        if (!victims.Contains(victim)) victim = victims[0];
        var cards = ResourceInfo.All.SelectMany(r => Enumerable.Repeat(r, victim.Hand[(int)r])).ToList();
        var stolen = cards[Rng.Next(cards.Count)];
        victim.Hand[(int)stolen]--;
        mover.Hand[(int)stolen]++;
        // Only the two players involved would see the card in real life.
        if (mover.IsHuman || victim.IsHuman) Log($"  {mover} steals 1 {stolen} from {victim}.");
        else Log($"  {mover} steals a card from {victim}.");
    }

    public List<Player> RobberVictims(Player mover, int hex) =>
        Board.Hexes[hex].Corners
            .Select(c => Board.Vertices[c].Owner)
            .Where(o => o >= 0 && o != mover.Id)
            .Distinct()
            .Select(o => Players[o])
            .Where(p => p.CardCount > 0)
            .ToList();

    private void CheckWinner(Player p)
    {
        if (p.VictoryPoints >= WinningPoints) Winner = p;
    }

    // ---------------------------------------------------------------- rule checks

    public bool CanPlaceSettlement(Player p, int v, bool setup)
    {
        if (v < 0 || v >= Board.Vertices.Count) return false;
        var vertex = Board.Vertices[v];
        if (vertex.Owner >= 0) return false;
        // Distance rule: no building on any neighboring corner.
        if (vertex.Neighbors.Any(n => Board.Vertices[n].Owner >= 0)) return false;
        if (setup) return true;
        return vertex.Edges.Any(e => Board.Edges[e].Owner == p.Id);
    }

    public bool CanPlaceRoad(Player p, int e)
    {
        if (e < 0 || e >= Board.Edges.Count) return false;
        var edge = Board.Edges[e];
        if (edge.Owner >= 0) return false;
        return ConnectsAt(p, edge.A) || ConnectsAt(p, edge.B);
    }

    // A new road may attach at a vertex with your building, or with your road
    // as long as an opponent's building isn't sitting there.
    private bool ConnectsAt(Player p, int v)
    {
        var vertex = Board.Vertices[v];
        if (vertex.Owner == p.Id) return true;
        if (vertex.Owner >= 0) return false;
        return vertex.Edges.Any(x => Board.Edges[x].Owner == p.Id);
    }

    public bool CanPlaceCity(Player p, int v) =>
        v >= 0 && v < Board.Vertices.Count && Board.Vertices[v].Owner == p.Id && !Board.Vertices[v].IsCity;

    public List<int> SettlementSpots(Player p, bool setup) =>
        Board.Vertices.Where(v => CanPlaceSettlement(p, v.Id, setup)).Select(v => v.Id).ToList();

    public List<int> RoadSpots(Player p) =>
        Board.Edges.Where(e => CanPlaceRoad(p, e.Id)).Select(e => e.Id).ToList();

    public List<int> SetupRoadSpots(int settlement) =>
        Board.Vertices[settlement].Edges.Where(e => Board.Edges[e].Owner < 0).ToList();

    // ---------------------------------------------------------------- actions (return an error, or null on success)

    public string? BuildRoad(Player p, int e)
    {
        if (p.Roads >= Player.MaxRoads) return "You have no roads left.";
        if (!p.CanAfford(Cost.Road)) return "A road costs 1 Wood + 1 Brick.";
        if (!CanPlaceRoad(p, e)) return "You can't build a road there. It must be empty and touch your road or building.";
        p.Pay(Cost.Road);
        PlaceRoad(p, e);
        Log($"{p} built a road {Board.Edges[e].A:D2}-{Board.Edges[e].B:D2}.");
        return null;
    }

    public string? BuildSettlement(Player p, int v)
    {
        if (p.Settlements >= Player.MaxSettlements) return "You have no settlements left. Upgrade one to a city.";
        if (!p.CanAfford(Cost.Settlement)) return "A settlement costs 1 Wood + 1 Brick + 1 Sheep + 1 Wheat.";
        if (!CanPlaceSettlement(p, v, false))
            return "You can't settle there. It must touch your road and be 2+ steps from every building.";
        p.Pay(Cost.Settlement);
        PlaceSettlement(p, v);
        Log($"{p} built a settlement at {v:D2}.");
        return null;
    }

    public string? BuildCity(Player p, int v)
    {
        if (p.Cities >= Player.MaxCities) return "You have no cities left.";
        if (!p.CanAfford(Cost.City)) return "A city costs 2 Wheat + 3 Ore.";
        if (!CanPlaceCity(p, v)) return "A city must replace one of your own settlements.";
        p.Pay(Cost.City);
        Board.Vertices[v].IsCity = true;
        p.Settlements--;
        p.Cities++;
        Log($"{p} upgraded {v:D2} to a city.");
        return null;
    }

    public const int BankRate = 4;

    public string? BankTrade(Player p, Resource give, Resource get)
    {
        if (give == get) return "Pick two different resources.";
        if (p.Hand[(int)give] < BankRate) return $"You need {BankRate} {give} to trade with the bank.";
        p.Hand[(int)give] -= BankRate;
        p.Hand[(int)get]++;
        Log($"{p} traded {BankRate} {give} for 1 {get} with the bank.");
        return null;
    }

    private void PlaceSettlement(Player p, int v)
    {
        Board.Vertices[v].Owner = p.Id;
        p.Settlements++;
        UpdateLongestRoad(); // a new settlement can cut someone's road
    }

    private void PlaceRoad(Player p, int e)
    {
        Board.Edges[e].Owner = p.Id;
        p.Roads++;
        UpdateLongestRoad();
    }

    // ---------------------------------------------------------------- longest road

    public int LongestRoad(Player p)
    {
        int best = 0;
        var used = new HashSet<int>();
        foreach (var e in Board.Edges.Where(e => e.Owner == p.Id))
        {
            best = Math.Max(best, Walk(p, e.A, used));
            best = Math.Max(best, Walk(p, e.B, used));
        }
        return best;
    }

    private int Walk(Player p, int v, HashSet<int> used)
    {
        int best = 0;
        foreach (int e in Board.Vertices[v].Edges)
        {
            var edge = Board.Edges[e];
            if (edge.Owner != p.Id || used.Contains(e)) continue;
            used.Add(e);
            int next = edge.Other(v);
            var nv = Board.Vertices[next];
            bool blocked = nv.Owner >= 0 && nv.Owner != p.Id;
            best = Math.Max(best, 1 + (blocked ? 0 : Walk(p, next, used)));
            used.Remove(e);
        }
        return best;
    }

    private void UpdateLongestRoad()
    {
        var lengths = Players.ToDictionary(p => p, LongestRoad);
        int max = lengths.Values.Max();
        var holder = Players.FirstOrDefault(p => p.HasLongestRoad);
        if (holder != null && max >= 5 && lengths[holder] == max) return; // holder keeps it on a tie

        if (holder != null) holder.HasLongestRoad = false;
        var top = Players.Where(p => lengths[p] == max).ToList();
        if (max >= 5 && top.Count == 1)
        {
            top[0].HasLongestRoad = true;
            if (top[0] != holder) Log($"  {top[0]} now has the Longest Road ({max})!");
        }
        else if (holder != null)
        {
            Log($"  {holder} lost the Longest Road.");
        }
    }
}
