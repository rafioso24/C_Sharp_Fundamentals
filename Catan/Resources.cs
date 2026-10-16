namespace Catan;

public enum Resource { Wood, Brick, Sheep, Wheat, Ore }

public enum Terrain { Forest, Hills, Pasture, Fields, Mountains, Desert }

public static class ResourceInfo
{
    public static readonly Resource[] All = Enum.GetValues<Resource>();

    public static Resource? FromTerrain(Terrain t) => t switch
    {
        Terrain.Forest => Resource.Wood,
        Terrain.Hills => Resource.Brick,
        Terrain.Pasture => Resource.Sheep,
        Terrain.Fields => Resource.Wheat,
        Terrain.Mountains => Resource.Ore,
        _ => null
    };

    // Accepts full names or any prefix, e.g. "wh" = Wheat, "o" = Ore.
    public static bool TryParse(string text, out Resource resource)
    {
        text = text.Trim().ToLowerInvariant();
        foreach (var r in All)
        {
            if (text.Length > 0 && r.ToString().ToLowerInvariant().StartsWith(text))
            {
                // "w" is ambiguous (Wood / Wheat), so require more letters.
                if (text == "w") break;
                resource = r;
                return true;
            }
        }
        resource = Resource.Wood;
        return false;
    }

    // How many of the 36 dice outcomes hit this number.
    public static int Pips(int number) => number == 0 ? 0 : 6 - Math.Abs(7 - number);
}

public static class Cost
{
    //                                   Wood Brick Sheep Wheat Ore
    public static readonly int[] Road = { 1, 1, 0, 0, 0 };
    public static readonly int[] Settlement = { 1, 1, 1, 1, 0 };
    public static readonly int[] City = { 0, 0, 0, 2, 3 };
}
