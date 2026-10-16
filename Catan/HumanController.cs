namespace Catan;

// Lets a person play from the console.
public class HumanController : IController
{
    private const string Help = @"Commands:
  road A B     build a road between corners A and B   (1 Wood, 1 Brick)
  settle N     build a settlement on corner N          (1 Wood, 1 Brick, 1 Sheep, 1 Wheat)
  city N       upgrade your settlement on corner N     (2 Wheat, 3 Ore)
  trade X Y    give 4 of X to the bank, get 1 of Y     (e.g. trade sheep ore)
  spots        list corners where you can settle now
  map          show the board
  costs        show build costs
  end          end your turn";

    public void BeforeRoll(Game game, Player me)
    {
        Console.WriteLine();
        Renderer.DrawBoard(game);
        Renderer.DrawScores(game);
        Console.WriteLine($"Your hand: {me.HandString()}");
        Prompt("Press Enter to roll the dice...");
    }

    public int ChooseSetupSettlement(Game game, Player me)
    {
        Console.WriteLine();
        Renderer.DrawBoard(game);
        Console.WriteLine("Place a settlement. Type a corner number, or 'hint' for good spots.");
        while (true)
        {
            string input = Prompt("Settlement corner> ");
            if (input == "hint")
            {
                ShowBestSpots(game, me, game.SettlementSpots(me, setup: true));
                continue;
            }
            if (int.TryParse(input, out int v) && game.CanPlaceSettlement(me, v, setup: true)) return v;
            Console.WriteLine("You can't settle there. Pick an empty corner with no building next to it.");
        }
    }

    public int ChooseSetupRoad(Game game, Player me, int settlement)
    {
        var options = game.SetupRoadSpots(settlement);
        var ends = options.Select(e => game.Board.Edges[e].Other(settlement)).ToList();
        Console.WriteLine($"Place a road from {settlement:D2} toward: {string.Join(", ", ends.Select(v => v.ToString("D2")))}");
        while (true)
        {
            string input = Prompt("Road to corner> ");
            if (int.TryParse(input, out int v) && ends.Contains(v)) return options[ends.IndexOf(v)];
            Console.WriteLine("Pick one of the listed corners.");
        }
    }

    public void TakeTurn(Game game, Player me)
    {
        Console.WriteLine("Type 'help' for commands.");
        while (true)
        {
            Console.WriteLine($"Your hand: {me.HandString()}   ({me.VictoryPoints} VP)");
            if (me.VictoryPoints >= Game.WinningPoints) return;
            string[] words = Prompt("> ").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) continue;
            string? error = null;
            bool built = false;
            switch (words[0])
            {
                case "end" or "e" or "done":
                    return;
                case "help" or "h" or "?":
                    Console.WriteLine(Help);
                    break;
                case "map" or "m":
                    Renderer.DrawBoard(game);
                    Renderer.DrawScores(game);
                    break;
                case "costs":
                    Console.WriteLine("Road: Wood+Brick | Settlement: Wood+Brick+Sheep+Wheat | City: 2 Wheat+3 Ore");
                    break;
                case "spots":
                    var spots = game.SettlementSpots(me, setup: false);
                    if (spots.Count == 0) Console.WriteLine("No spots yet. Build roads to reach an open corner.");
                    else ShowBestSpots(game, me, spots);
                    break;
                case "road" or "r":
                    if (words.Length == 3 && int.TryParse(words[1], out int a) && int.TryParse(words[2], out int b))
                    {
                        var edge = game.Board.FindEdge(a, b);
                        error = edge == null ? $"Corners {a} and {b} are not next to each other." : game.BuildRoad(me, edge.Id);
                    }
                    else error = "Usage: road A B   (two corners next to each other)";
                    built = error == null;
                    break;
                case "settle" or "s":
                    error = words.Length == 2 && int.TryParse(words[1], out int sv)
                        ? game.BuildSettlement(me, sv) : "Usage: settle N";
                    built = error == null;
                    break;
                case "city" or "c":
                    error = words.Length == 2 && int.TryParse(words[1], out int cv)
                        ? game.BuildCity(me, cv) : "Usage: city N";
                    built = error == null;
                    break;
                case "trade" or "t":
                    if (words.Length == 3 && ResourceInfo.TryParse(words[1], out var give) &&
                        ResourceInfo.TryParse(words[2], out var get))
                        error = game.BankTrade(me, give, get);
                    else error = "Usage: trade X Y   (resources: wood, brick, sheep, wheat, ore)";
                    break;
                default:
                    error = "Unknown command. Type 'help'.";
                    break;
            }
            if (error != null) Console.WriteLine(error);
            if (built) Renderer.DrawBoard(game);
        }
    }

    public int ChooseRobberHex(Game game, Player me)
    {
        Console.WriteLine("Move the robber. Pick a hex letter (A-S) different from where it is now.");
        while (true)
        {
            string input = Prompt("Robber hex> ");
            var hex = input.Length == 1 ? game.Board.FindHex(input[0]) : null;
            if (hex != null && hex.Id != game.Board.RobberHex) return hex.Id;
            Console.WriteLine("That's not a valid hex.");
        }
    }

    public Player ChooseVictim(Game game, Player me, List<Player> options)
    {
        Console.WriteLine("Steal from who?");
        for (int i = 0; i < options.Count; i++)
            Console.WriteLine($"  {i + 1}. {options[i]} ({options[i].CardCount} cards)");
        while (true)
        {
            if (int.TryParse(Prompt("Victim> "), out int n) && n >= 1 && n <= options.Count) return options[n - 1];
            Console.WriteLine("Pick one of the numbers.");
        }
    }

    public int[] ChooseDiscard(Game game, Player me, int count)
    {
        Console.WriteLine($"You have {me.CardCount} cards and must discard {count}.");
        Console.WriteLine($"Your hand: {me.HandString()}");
        while (true)
        {
            var words = Prompt($"Discard {count} (e.g. 'wood wood ore')> ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var discard = new int[5];
            bool ok = words.Length == count;
            foreach (var w in words)
            {
                if (ResourceInfo.TryParse(w, out var r)) discard[(int)r]++;
                else ok = false;
            }
            if (ok && discard.Select((n, i) => n <= me.Hand[i]).All(x => x)) return discard;
            Console.WriteLine($"Name exactly {count} cards that you have.");
        }
    }

    private static void ShowBestSpots(Game game, Player me, List<int> spots)
    {
        Console.WriteLine("Best open corners (dots = how often they pay out):");
        foreach (int v in spots.OrderByDescending(v => BotController.ScoreSpot(game, me, v)).Take(6))
            Console.WriteLine($"  {v:D2}: {game.Board.VertexPips(v),2} dots - {game.Board.DescribeVertex(v)}");
    }

    private static string Prompt(string text)
    {
        Console.Write(text);
        string? line = Console.ReadLine();
        if (line == null)
        {
            Console.WriteLine();
            Console.WriteLine("Input closed. Goodbye!");
            Environment.Exit(0);
        }
        return line.Trim().ToLowerInvariant();
    }
}
