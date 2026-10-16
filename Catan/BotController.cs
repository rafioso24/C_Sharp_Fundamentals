namespace Catan;

public interface IController
{
    void BeforeRoll(Game game, Player me);
    int ChooseSetupSettlement(Game game, Player me);
    int ChooseSetupRoad(Game game, Player me, int settlement);
    void TakeTurn(Game game, Player me);
    int ChooseRobberHex(Game game, Player me);
    Player ChooseVictim(Game game, Player me, List<Player> options);
    int[] ChooseDiscard(Game game, Player me, int count);
}

// A simple greedy bot: it builds the best thing it can afford,
// and trades 4:1 with the bank to get closer to its next goal.
public class BotController : IController
{
    public void BeforeRoll(Game game, Player me) { }

    public int ChooseSetupSettlement(Game game, Player me) =>
        game.SettlementSpots(me, setup: true).OrderByDescending(v => ScoreSpot(game, me, v)).First();

    public int ChooseSetupRoad(Game game, Player me, int settlement) =>
        game.SetupRoadSpots(settlement)
            .OrderByDescending(e => RoadValue(game, me, game.Board.Edges[e].Other(settlement)))
            .First();

    public void TakeTurn(Game game, Player me)
    {
        for (int step = 0; step < 30; step++)
        {
            if (!TryBuild(game, me) && !TryTrade(game, me)) break;
            if (me.VictoryPoints >= Game.WinningPoints) break;
        }
    }

    private bool TryBuild(Game game, Player me)
    {
        if (me.CanAfford(Cost.City) && me.Cities < Player.MaxCities && me.Settlements > 0)
        {
            int best = game.Board.Vertices.Where(v => game.CanPlaceCity(me, v.Id))
                .OrderByDescending(v => game.Board.VertexPips(v.Id)).First().Id;
            return game.BuildCity(me, best) == null;
        }

        var spots = game.SettlementSpots(me, setup: false);
        if (spots.Count > 0 && me.CanAfford(Cost.Settlement) && me.Settlements < Player.MaxSettlements)
        {
            int best = spots.OrderByDescending(v => ScoreSpot(game, me, v)).First();
            return game.BuildSettlement(me, best) == null;
        }

        // Build roads when there's nowhere to settle, or when we have spare wood and brick.
        bool spare = me.Hand[(int)Resource.Wood] >= 2 && me.Hand[(int)Resource.Brick] >= 2;
        if (me.CanAfford(Cost.Road) && me.Roads < Player.MaxRoads && (spots.Count == 0 || spare))
        {
            var roads = game.RoadSpots(me);
            if (roads.Count == 0) return false;
            int best = roads.OrderByDescending(e => RoadValueForEdge(game, me, e)).First();
            return game.BuildRoad(me, best) == null;
        }
        return false;
    }

    private bool TryTrade(Game game, Player me)
    {
        var goal = PickGoal(game, me);
        var missing = ResourceInfo.All.Where(r => me.Hand[(int)r] < goal[(int)r]).ToList();
        if (missing.Count == 0) return false;
        foreach (var give in ResourceInfo.All.OrderByDescending(r => me.Hand[(int)r]))
        {
            if (me.Hand[(int)give] - goal[(int)give] >= Game.BankRate)
                return game.BankTrade(me, give, missing[0]) == null;
        }
        return false;
    }

    private static int[] PickGoal(Game game, Player me)
    {
        if (me.Settlements > 0 && me.Cities < Player.MaxCities &&
            me.Hand[(int)Resource.Ore] + me.Hand[(int)Resource.Wheat] >= 3)
            return Cost.City;
        if (game.SettlementSpots(me, false).Count > 0 && me.Settlements < Player.MaxSettlements)
            return Cost.Settlement;
        if (me.Roads < Player.MaxRoads) return Cost.Road;
        return Cost.City;
    }

    public int ChooseRobberHex(Game game, Player me)
    {
        var board = game.Board;
        return board.Hexes.Where(h => h.Id != board.RobberHex).OrderByDescending(h =>
        {
            double score = 0;
            foreach (int c in h.Corners)
            {
                var v = board.Vertices[c];
                if (v.Owner < 0) continue;
                int weight = v.IsCity ? 2 : 1;
                if (v.Owner == me.Id) score -= 10 * weight;
                else score += weight * (1 + game.Players[v.Owner].VictoryPoints / 3.0);
            }
            return score * ResourceInfo.Pips(h.Number) + game.Rng.NextDouble() * 0.1;
        }).First().Id;
    }

    public Player ChooseVictim(Game game, Player me, List<Player> options) =>
        options.OrderByDescending(p => p.VictoryPoints).ThenByDescending(p => p.CardCount).First();

    public int[] ChooseDiscard(Game game, Player me, int count) => DefaultDiscard(me, count);

    // Throw away cards from whatever we hold the most of.
    public static int[] DefaultDiscard(Player me, int count)
    {
        var hand = (int[])me.Hand.Clone();
        var discard = new int[5];
        for (int i = 0; i < count; i++)
        {
            int most = Array.IndexOf(hand, hand.Max());
            hand[most]--;
            discard[most]++;
        }
        return discard;
    }

    // ---------------------------------------------------------------- scoring helpers

    // Production value of a spot, with a bonus for resources we don't produce yet.
    public static double ScoreSpot(Game game, Player me, int v)
    {
        var board = game.Board;
        var have = OwnedResources(game, me);
        double score = 0;
        var seen = new HashSet<Resource>();
        foreach (int h in board.Vertices[v].Hexes)
        {
            var hex = board.Hexes[h];
            var r = ResourceInfo.FromTerrain(hex.Terrain);
            if (r == null) continue;
            double pips = ResourceInfo.Pips(hex.Number);
            if (h == board.RobberHex) pips /= 2;
            score += pips;
            if (!have.Contains(r.Value) && seen.Add(r.Value)) score += 1.5;
        }
        return score;
    }

    private static HashSet<Resource> OwnedResources(Game game, Player me)
    {
        var result = new HashSet<Resource>();
        foreach (var v in game.Board.Vertices.Where(v => v.Owner == me.Id))
            foreach (int h in v.Hexes)
            {
                var r = ResourceInfo.FromTerrain(game.Board.Hexes[h].Terrain);
                if (r != null) result.Add(r.Value);
            }
        return result;
    }

    private static double RoadValueForEdge(Game game, Player me, int e)
    {
        var edge = game.Board.Edges[e];
        return Math.Max(RoadValue(game, me, edge.A), RoadValue(game, me, edge.B)) + game.Rng.NextDouble() * 0.1;
    }

    // How good are the settlement spots this road end would open up?
    private static double RoadValue(Game game, Player me, int v)
    {
        var board = game.Board;
        if (board.Vertices[v].Owner >= 0 && board.Vertices[v].Owner != me.Id) return 0;
        double best = 0;
        if (game.CanPlaceSettlement(me, v, setup: true)) best = ScoreSpot(game, me, v);
        foreach (int n in board.Vertices[v].Neighbors)
            if (game.CanPlaceSettlement(me, n, setup: true))
                best = Math.Max(best, ScoreSpot(game, me, n) * 0.7);
        return best;
    }
}
