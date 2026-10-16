using Catan;

// Usage: dotnet run [-- --bots N] [--seed N] [--watch]
//   --bots N   number of bots (1-3); asks if not given
//   --seed N   same seed = same board and dice
//   --watch    no human player; watch bots play each other
int? bots = null;
int? seed = null;
bool watch = false;
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--bots" && i + 1 < args.Length && int.TryParse(args[++i], out int b)) bots = Math.Clamp(b, 1, 3);
    else if (args[i] == "--seed" && i + 1 < args.Length && int.TryParse(args[++i], out int s)) seed = s;
    else if (args[i] == "--watch") watch = true;
}

Console.WriteLine("=== Settlers of Catan (MVP) ===");
Console.WriteLine($"First to {Game.WinningPoints} points wins. Settlement = 1, City = 2, Longest Road (5+) = 2.");
while (bots == null)
{
    Console.Write("How many bots? (1-3) ");
    string? line = Console.ReadLine();
    if (line == null) return;
    if (int.TryParse(line, out int n) && n >= 1 && n <= 3) bots = n;
}

var game = new Game(seed.HasValue ? new Random(seed.Value) : new Random());
ConsoleColor[] colors = { ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.Red, ConsoleColor.Blue };
string[] botNames = { "Ada", "Bob", "Cy", "Dee" };

int seats = bots.Value + 1;
int humanSeat = watch ? -1 : game.Rng.Next(seats); // random turn order for you
int botIndex = 0;
for (int i = 0; i < seats; i++)
{
    if (i == humanSeat) game.Players.Add(new Player(i, "You", colors[i], new HumanController()));
    else game.Players.Add(new Player(i, botNames[botIndex++], colors[i], new BotController()));
}

Console.WriteLine("Players in turn order: " + string.Join(", ", game.Players));
game.Run();
