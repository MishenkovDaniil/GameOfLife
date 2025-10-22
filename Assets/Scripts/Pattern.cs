using UnityEngine;

/// <summary>
/// Game of Life patterns.
/// </summary>
[System.Serializable]
public class Pattern
{
    public string name;
    public int width;
    public int height;
    public int[,] grid;

    public Pattern(string name, int[,] grid)
    {
        this.name = name;
        this.grid = grid;
        this.height = grid.GetLength(0);
        this.width = grid.GetLength(1);
    }

    public static class Library
    {
        public static Pattern Glider => new Pattern("Glider", new int[,]
        {
            { 0, 1, 0 },
            { 0, 0, 1 },
            { 1, 1, 1 }
        });

        public static Pattern Blinker => new Pattern("Blinker", new int[,]
        {
            { 1, 1, 1 }
        });

        public static Pattern Toad => new Pattern("Toad", new int[,]
        {
            { 0, 1, 1, 1 },
            { 1, 1, 1, 0 }
        });

        public static Pattern Beacon => new Pattern("Beacon", new int[,]
        {
            { 1, 1, 0, 0 },
            { 1, 1, 0, 0 },
            { 0, 0, 1, 1 },
            { 0, 0, 1, 1 }
        });

        public static Pattern Pulsar => new Pattern("Pulsar", new int[,]
        {
            { 0,0,1,1,1,0,0,0,1,1,1,0,0 },
            { 0,0,0,0,0,0,0,0,0,0,0,0,0 },
            { 1,0,0,0,0,1,0,1,0,0,0,0,1 },
            { 1,0,0,0,0,1,0,1,0,0,0,0,1 },
            { 1,0,0,0,0,1,0,1,0,0,0,0,1 },
            { 0,0,1,1,1,0,0,0,1,1,1,0,0 },
            { 0,0,0,0,0,0,0,0,0,0,0,0,0 },
            { 0,0,1,1,1,0,0,0,1,1,1,0,0 },
            { 1,0,0,0,0,1,0,1,0,0,0,0,1 },
            { 1,0,0,0,0,1,0,1,0,0,0,0,1 },
            { 1,0,0,0,0,1,0,1,0,0,0,0,1 },
            { 0,0,0,0,0,0,0,0,0,0,0,0,0 },
            { 0,0,1,1,1,0,0,0,1,1,1,0,0 }
        });

        public static Pattern LWSS => new Pattern("LWSS (Lightweight Spaceship)", new int[,]
        {
            { 0, 1, 0, 0, 1 },
            { 1, 0, 0, 0, 0 },
            { 1, 0, 0, 0, 1 },
            { 1, 1, 1, 1, 0 }
        });

        public static Pattern GliderGun => new Pattern("Gosper Glider Gun", new int[,]
        {
            { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,0,0,0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,1,1 },
            { 0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,1,0,0,0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,1,1 },
            { 1,1,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0 },
            { 1,1,0,0,0,0,0,0,0,0,1,0,0,0,1,0,1,1,0,0,0,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,0,0,0,1,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0 },
            { 0,0,0,0,0,0,0,0,0,0,0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0 }
        });

        public static Pattern Acorn => new Pattern("Acorn", new int[,]
        {
            { 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 1, 0, 0, 0 },
            { 1, 1, 0, 0, 1, 1, 1 }
        });

        public static Pattern[] All => new Pattern[]
        {
            Glider,
            Blinker,
            Toad,
            Beacon,
            Pulsar,
            LWSS,
            GliderGun,
            Acorn
        };
    }
}
