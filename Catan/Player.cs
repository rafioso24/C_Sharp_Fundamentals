namespace Catan;

public class Player
{
    public const int MaxRoads = 15, MaxSettlements = 5, MaxCities = 4;

    public int Id { get; }
    public string Name { get; }
    public ConsoleColor Color { get; }
    public IController Controller { get; }
    public bool IsHuman => Controller is HumanController;

    public int[] Hand { get; } = new int[5];
    public int Roads { get; set; }
    public int Settlements { get; set; }
    public int Cities { get; set; }
    public bool HasLongestRoad { get; set; }

    public Player(int id, string name, ConsoleColor color, IController controller)
    {
        Id = id;
        Name = name;
        Color = color;
        Controller = controller;
    }

    public int Number => Id + 1;
    public int VictoryPoints => Settlements + 2 * Cities + (HasLongestRoad ? 2 : 0);
    public int CardCount => Hand.Sum();

    public bool CanAfford(int[] cost) => cost.Select((c, i) => Hand[i] >= c).All(ok => ok);

    public void Pay(int[] cost)
    {
        for (int i = 0; i < cost.Length; i++) Hand[i] -= cost[i];
    }

    public string HandString() =>
        string.Join("  ", ResourceInfo.All.Select(r => $"{r}:{Hand[(int)r]}"));

    public override string ToString() => $"{Name} (P{Number})";
}
