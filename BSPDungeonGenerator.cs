namespace BSPConsoleDungeon;
public class BSPDungeonGenerator
{
    private int width;
    private int height;
    private int minRoomSize;
    private int maxDepth;
    private List<Rectangle> rooms;
    private Random rng;

    private TileType[,] Map { get; set; }

    public BSPDungeonGenerator(int width, int height, int minRoomSize, int maxDepth, int seed = 0)
    {
        this.width = width;
        this.height = height;
        this.minRoomSize = minRoomSize;
        this.maxDepth = maxDepth;
        rng = seed == 0 ? new Random() : new Random(seed);

        Map = new TileType[width, height];
        rooms = new List<Rectangle>();
    }

    public void Generate()
    {
        // Todo comienza como muro
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                Map[x, y] = TileType.Wall;

        Node root = new Node(new Rectangle(0, 0, width, height));
        Split(root, 0);

        CreateRooms(root);
        ConnectRooms(root);
        AddRandomElements();
    }

    private void Split(Node node, int depth)
    {
        if (depth >= maxDepth) return;

        bool splitH = rng.Next(0, 2) == 0;
        if (node.Area.Width > node.Area.Height && node.Area.Width / node.Area.Height >= 1.25)
            splitH = false;
        else if (node.Area.Height > node.Area.Width && node.Area.Height / node.Area.Width >= 1.25)
            splitH = true;

        int max = (splitH ? node.Area.Height : node.Area.Width) - minRoomSize;
        if (max <= minRoomSize) return;

        int split = rng.Next(minRoomSize, max);

        if (splitH)
        {
            node.Left = new Node(new Rectangle(node.Area.X, node.Area.Y, node.Area.Width, split));
            node.Right = new Node(new Rectangle(node.Area.X, node.Area.Y + split, node.Area.Width, node.Area.Height - split));
        }
        else
        {
            node.Left = new Node(new Rectangle(node.Area.X, node.Area.Y, split, node.Area.Height));
            node.Right = new Node(new Rectangle(node.Area.X + split, node.Area.Y, node.Area.Width - split, node.Area.Height));
        }

        Split(node.Left, depth + 1);
        Split(node.Right, depth + 1);
    }

    private void CreateRooms(Node node)
    {
        if (node.Left != null || node.Right != null)
        {
            if (node.Left != null) CreateRooms(node.Left);
            if (node.Right != null) CreateRooms(node.Right);
            return;
        }

        // Evitar nodos demasiado pequeños
        if (node.Area.Width <= minRoomSize || node.Area.Height <= minRoomSize)
            return;

        // Limitar el área para no tocar los bordes
        int nodeX = Math.Max(node.Area.X, 1);
        int nodeY = Math.Max(node.Area.Y, 1);
        int nodeWidth = Math.Min(node.Area.Width, width - 2 - nodeX);
        int nodeHeight = Math.Min(node.Area.Height, height - 2 - nodeY);

        // Calculamos dimensiones de la sala de forma segura
        int maxRoomWidth = Math.Max(minRoomSize, nodeWidth);
        int maxRoomHeight = Math.Max(minRoomSize, nodeHeight);

        int roomWidth = rng.Next(minRoomSize, maxRoomWidth + 1);
        int roomHeight = rng.Next(minRoomSize, maxRoomHeight + 1);

        // Calculamos posición de la sala dentro del nodo
        int roomX = rng.Next(nodeX, nodeX + nodeWidth - roomWidth + 1);
        int roomY = rng.Next(nodeY, nodeY + nodeHeight - roomHeight + 1);

        Rectangle room = new Rectangle(roomX, roomY, roomWidth, roomHeight);
        rooms.Add(room);
        node.Room = room;

        // Marcamos el mapa con suelo (0)
        for (int x = room.Left; x < room.Right; x++)
            for (int y = room.Top; y < room.Bottom; y++)
                Map[x, y] = 0;
    }

    private void ConnectRooms(Node node)
    {
        if (node.Left != null && node.Right != null)
        {
            Rectangle roomA = GetRoom(node.Left);
            Rectangle roomB = GetRoom(node.Right);

            Point centerA = roomA.Center;
            Point centerB = roomB.Center;

            if (rng.Next(0, 2) == 0)
            {
                CreateCorridor(centerA.X, centerB.X, centerA.Y, true);
                CreateCorridor(centerA.Y, centerB.Y, centerB.X, false);
            }
            else
            {
                CreateCorridor(centerA.Y, centerB.Y, centerA.X, false);
                CreateCorridor(centerA.X, centerB.X, centerB.Y, true);
            }

            ConnectRooms(node.Left);
            ConnectRooms(node.Right);
        }
    }

    private void AddRandomElements(double waterChance = 0.05, double grassChance = 0.10)
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (Map[x, y] != TileType.Floor) continue;

                double roll = rng.NextDouble();

                if (roll < waterChance)
                    Map[x, y] = TileType.Water;
                else if (roll < waterChance + grassChance)
                    Map[x, y] = TileType.Grass;
            }
        }
    }


    private Rectangle GetRoom(Node node)
    {
        if (node.Room != Rectangle.Empty) return node.Room;

        Rectangle leftRoom = Rectangle.Empty;
        Rectangle rightRoom = Rectangle.Empty;

        if (node.Left != null) leftRoom = GetRoom(node.Left);
        if (node.Right != null) rightRoom = GetRoom(node.Right);

        if (leftRoom != Rectangle.Empty) return leftRoom;
        else if (rightRoom != Rectangle.Empty) return rightRoom;
        else return Rectangle.Empty;
    }

    private void CreateCorridor(int start, int end, int fixedCoord, bool horizontal)
    {
        for (int i = Math.Min(start, end); i <= Math.Max(start, end); i++)
        {
            if (horizontal)
            {
                if (i <= 0 || i >= width - 1 || fixedCoord <= 0 || fixedCoord >= height - 1)
                    continue;
                Map[i, fixedCoord] = 0;
            }
            else
            {
                if (i <= 0 || i >= height - 1 || fixedCoord <= 0 || fixedCoord >= width - 1)
                    continue;
                Map[fixedCoord, i] = 0;
            }
        }
    }

    public void PrintMap()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char c = Map[x, y] switch
                {
                    TileType.Wall => '#',
                    TileType.Floor => '.',
                    TileType.Water => '~',
                    TileType.Grass => '"',
                    _ => '?'
                };
                Console.Write(c);
            }
            Console.WriteLine();
        }
    }

    // Helpers internos
    private class Node
    {
        public Rectangle Area;
        public Node Left, Right;
        public Rectangle Room;

        public Node(Rectangle area)
        {
            Area = area;
            Left = Right = null;
            Room = Rectangle.Empty;
        }
    }

    // Agrega la implementación de IEquatable<Rectangle> y sobrecarga de operadores en Rectangle
    private struct Rectangle : IEquatable<Rectangle>
    {
        public int X, Y, Width, Height;

        public int Left => X;
        public int Right => X + Width;
        public int Top => Y;
        public int Bottom => Y + Height;

        public bool IsEmpty => Width == 0 || Height == 0;

        public Point Center => new Point(X + Width / 2, Y + Height / 2);

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public static readonly Rectangle Empty = new Rectangle(0, 0, 0, 0);

        public bool Equals(Rectangle other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is Rectangle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        public static bool operator ==(Rectangle left, Rectangle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rectangle left, Rectangle right)
        {
            return !(left == right);
        }
    }

    private struct Point
    {
        public int X, Y;
        public Point(int x, int y) { X = x; Y = y; }
    }

    public enum TileType
    {
        Wall = 1,
        Floor = 0,
        Water = 2,
        Grass = 3
    }

    public TileType[,] GetMap() => Map;
}