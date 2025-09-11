using GoRogue;
using GoRogue.GameFramework;
using GoRogue.MapGeneration;
using GoRogue.MapViews;
using System;
using System.Collections.Generic;

namespace BSPConsoleDungeon;

// Factory class para el terreno usando GameObjects de GoRogue
static class TerrainFactory
{
    public static GameObject Wall(Coord position)
        => new GameObject(position, layer: 0, parentObject: null, isStatic: true, isWalkable: false, isTransparent: false);

    public static GameObject Floor(Coord position)
        => new GameObject(position, layer: 0, parentObject: null, isStatic: true, isWalkable: true, isTransparent: true);

    public static GameObject Water(Coord position)
        => new GameObject(position, layer: 0, parentObject: null, isStatic: true, isWalkable: false, isTransparent: true);

    public static GameObject Grass(Coord position)
        => new GameObject(position, layer: 0, parentObject: null, isStatic: true, isWalkable: true, isTransparent: true);

    public static GameObject Pillar(Coord position)
        => new GameObject(position, layer: 0, parentObject: null, isStatic: true, isWalkable: false, isTransparent: false);
}

// Factory class para entidades
static class EntityFactory
{
    public static GameObject Player(Coord position)
        => new GameObject(position, layer: 1, parentObject: null, isStatic: false, isWalkable: false, isTransparent: true);
}

public struct Room(Coord topLeft, int width, int height)
{
    public Coord TopLeft = topLeft;
    public int Width = width;
    public int Height = height;
    public readonly Coord Center => new(TopLeft.X + Width / 2, TopLeft.Y + Height / 2);

    public readonly bool Contains(Coord point)
    {
        return point.X >= TopLeft.X && point.X < TopLeft.X + Width &&
               point.Y >= TopLeft.Y && point.Y < TopLeft.Y + Height;
    }
}

class Program
{
    private static Map _map;
    private static GameObject _player;
    private static readonly Random _rng = new Random();
    private static List<Room> _rooms = new List<Room>();

    public static void Main(string[] args)
    {
        // Crear el mapa de GoRogue más grande para el estilo Diablo
        _map = new Map(width: 300, height: 120,
                      numberOfEntityLayers: 1, distanceMeasurement: Distance.CHEBYSHEV);

        // Generar el mapa estilo Diablo
        GenerateStyleMap();

        // Encontrar una posición válida para el jugador
        var playerPosition = FindValidPlayerPosition();
        _player = EntityFactory.Player(playerPosition);
        _map.AddEntity(_player);

        // Bucle principal del juego
        GameLoop();
    }

    private static void GenerateStyleMap()
    {
        // Inicializar todo como muros
        for (int x = 0; x < _map.Width; x++)
        {
            for (int y = 0; y < _map.Height; y++)
            {
                _map.SetTerrain(TerrainFactory.Wall((x, y)));
            }
        }

        // Generar habitaciones grandes estilo Diablo
        GenerateRooms();

        // Conectar habitaciones con pasillos gruesos - CORREGIDO
        ConnectRoomsWithThickCorridors();

        // Añadir detalles arquitectónicos estilo Diablo
        AddArchitecturalDetails();
    }

    private static void GenerateRooms()
    {
        _rooms.Clear();
        int attempts = 0;
        int maxAttempts = 500;

        // Intentar crear 6-8 habitaciones grandes
        int targetRooms = _rng.Next(10, 20);

        while (_rooms.Count < targetRooms && attempts < maxAttempts)
        {
            attempts++;

            // Tamaños de habitación más grandes para estilo Diablo
            int roomWidth = _rng.Next(12, 14);
            int roomHeight = _rng.Next(10, 12);

            // Posición aleatoria con margen
            int roomX = _rng.Next(3, _map.Width - roomWidth - 3);
            int roomY = _rng.Next(3, _map.Height - roomHeight - 3);

            var newRoom = new Room(new Coord(roomX, roomY), roomWidth, roomHeight);

            // Verificar que no se superponga con habitaciones existentes
            bool overlaps = false;
            foreach (var existingRoom in _rooms)
            {
                if (RoomsOverlap(newRoom, existingRoom, 4)) // 4 celdas de separación mínima
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                _rooms.Add(newRoom);
                CreateRoom(newRoom);
            }
        }
    }

    private static bool RoomsOverlap(Room room1, Room room2, int buffer)
    {
        return !(room1.TopLeft.X + room1.Width + buffer <= room2.TopLeft.X ||
                 room2.TopLeft.X + room2.Width + buffer <= room1.TopLeft.X ||
                 room1.TopLeft.Y + room1.Height + buffer <= room2.TopLeft.Y ||
                 room2.TopLeft.Y + room2.Height + buffer <= room1.TopLeft.Y);
    }

    private static void CreateRoom(Room room)
    {
        // Crear el suelo de la habitación (incluyendo los bordes interiores)
        for (int x = room.TopLeft.X; x < room.TopLeft.X + room.Width; x++)
        {
            for (int y = room.TopLeft.Y; y < room.TopLeft.Y + room.Height; y++)
            {
                // Solo crear suelo en el interior, dejar los bordes como están
                if (x > room.TopLeft.X && x < room.TopLeft.X + room.Width - 1 &&
                    y > room.TopLeft.Y && y < room.TopLeft.Y + room.Height - 1)
                {
                    _map.SetTerrain(TerrainFactory.Floor((x, y)));
                }
            }
        }

        // Añadir pilares decorativos en habitaciones grandes
        if (room.Width > 15 && room.Height > 10)
        {
            AddPillarsToRoom(room);
        }
    }

    private static void AddPillarsToRoom(Room room)
    {
        // Añadir pilares en esquinas interiores y centro
        var pillarPositions = new[]
        {
            new Coord(room.TopLeft.X + 3, room.TopLeft.Y + 3),
            new Coord(room.TopLeft.X + room.Width - 4, room.TopLeft.Y + 3),
            new Coord(room.TopLeft.X + 3, room.TopLeft.Y + room.Height - 4),
            new Coord(room.TopLeft.X + room.Width - 4, room.TopLeft.Y + room.Height - 4)
        };

        foreach (var pos in pillarPositions)
        {
            if (_rng.NextDouble() < 0.7) // 70% probabilidad de colocar pilar
            {
                _map.SetTerrain(TerrainFactory.Pillar(pos));
            }
        }

        // Pilar central en habitaciones muy grandes
        if (room.Width > 18 && room.Height > 12)
        {
            _map.SetTerrain(TerrainFactory.Pillar(room.Center));
        }
    }

    private static void ConnectRoomsWithThickCorridors()
    {
        if (_rooms.Count < 2) return;

        // Conectar cada habitación con la siguiente
        for (int i = 0; i < _rooms.Count - 1; i++)
        {
            CreateThickCorridor(_rooms[i].Center, _rooms[i + 1].Center);
        }

        // Conectar la última habitación con la primera para crear un circuito
        if (_rooms.Count > 2)
        {
            CreateThickCorridor(_rooms[_rooms.Count - 1].Center, _rooms[0].Center);
        }

        // Añadir algunas conexiones adicionales aleatorias
        int extraConnections = _rng.Next(1, 3);
        for (int i = 0; i < extraConnections; i++)
        {
            int room1 = _rng.Next(_rooms.Count);
            int room2 = _rng.Next(_rooms.Count);
            if (room1 != room2)
            {
                CreateThickCorridor(_rooms[room1].Center, _rooms[room2].Center);
            }
        }
    }

    private static void CreateThickCorridor(Coord start, Coord end)
    {
        int corridorHalfWidthX = 6; // Ancho horizontal: 9 celdas
        int corridorHalfWidthY = 4; // Ancho vertical: 5 celdas
        
        var path = GetCorridorPath(start, end);

        foreach (var point in path)
        {
            for (int dx = -corridorHalfWidthX; dx <= corridorHalfWidthX; dx++)
            {
                for (int dy = -corridorHalfWidthY; dy <= corridorHalfWidthY; dy++)
                {
                    var pos = new Coord(point.X + dx, point.Y + dy);
                    
                    if (pos.X > 0 && pos.X < _map.Width - 1 && 
                        pos.Y > 0 && pos.Y < _map.Height - 1)
                    {
                        var currentTerrain = _map.Terrain[pos];
                        bool isPillar = !currentTerrain.IsWalkable && !currentTerrain.IsTransparent && IsInRoom(pos);
                        
                        if (!isPillar)
                        {
                            double random = _rng.NextDouble();
                            GameObject terrain;

                            if (random < 0.01)
                                terrain = TerrainFactory.Water(pos);
                            else if (random < 0.03)
                                terrain = TerrainFactory.Grass(pos);
                            else
                                terrain = TerrainFactory.Floor(pos);

                            _map.SetTerrain(terrain);
                        }
                    }
                }
            }
        }
    }

    private static List<Coord> GetCorridorPath(Coord start, Coord end)
    {
        var path = new List<Coord>();
        var current = start;

        // Añadir el punto de inicio
        path.Add(current);

        // Crear camino en forma de L (horizontal primero, luego vertical)
        // Mover horizontalmente
        while (current.X != end.X)
        {
            current = new Coord(current.X + Math.Sign(end.X - current.X), current.Y);
            path.Add(current);
        }

        // Mover verticalmente
        while (current.Y != end.Y)
        {
            current = new Coord(current.X, current.Y + Math.Sign(end.Y - current.Y));
            path.Add(current);
        }

        return path;
    }

    private static void AddArchitecturalDetails()
    {
        // Añadir algunos muros decorativos y obstáculos menores
        foreach (var room in _rooms)
        {
            // Pequeñas alcobas o nichos en las paredes
            if (_rng.NextDouble() < 0.6) // 60% probabilidad
            {
                AddAlcoveToRoom(room);
            }
        }
    }

    private static void AddAlcoveToRoom(Room room)
    {
        // Crear pequeñas alcobas en las paredes de la habitación
        var wallSide = _rng.Next(4); // 0=norte, 1=este, 2=sur, 3=oeste
        
        switch (wallSide)
        {
            case 0: // Norte
                {
                    int alcoveX = _rng.Next(room.TopLeft.X + 3, room.TopLeft.X + room.Width - 3);
                    var alcovePos = new Coord(alcoveX, room.TopLeft.Y);
                    _map.SetTerrain(TerrainFactory.Floor(alcovePos));
                    break;
                }
            case 1: // Este
                {
                    int alcoveY = _rng.Next(room.TopLeft.Y + 3, room.TopLeft.Y + room.Height - 3);
                    var alcovePos = new Coord(room.TopLeft.X + room.Width - 1, alcoveY);
                    _map.SetTerrain(TerrainFactory.Floor(alcovePos));
                    break;
                }
            case 2: // Sur
                {
                    int alcoveX = _rng.Next(room.TopLeft.X + 3, room.TopLeft.X + room.Width - 3);
                    var alcovePos = new Coord(alcoveX, room.TopLeft.Y + room.Height - 1);
                    _map.SetTerrain(TerrainFactory.Floor(alcovePos));
                    break;
                }
            case 3: // Oeste
                {
                    int alcoveY = _rng.Next(room.TopLeft.Y + 3, room.TopLeft.Y + room.Height - 3);
                    var alcovePos = new Coord(room.TopLeft.X, alcoveY);
                    _map.SetTerrain(TerrainFactory.Floor(alcovePos));
                    break;
                }
        }
    }

    private static Coord FindValidPlayerPosition()
    {
        // Colocar jugador en el centro de la primera habitación
        if (_rooms.Count > 0)
        {
            return _rooms[0].Center;
        }

        // Buscar la primera posición caminable como respaldo
        for (int x = 1; x < _map.Width - 1; x++)
        {
            for (int y = 1; y < _map.Height - 1; y++)
            {
                if (_map.Terrain[x, y].IsWalkable)
                    return new Coord(x, y);
            }
        }
        
        // Si no se encuentra una posición válida, crear una en el centro
        var centerPos = new Coord(_map.Width / 2, _map.Height / 2);
        _map.SetTerrain(TerrainFactory.Floor(centerPos));
        return centerPos;
    }

    private static void GameLoop()
    {
        bool gameRunning = true;

        while (gameRunning)
        {
            // Limpiar consola y mostrar el mapa
            Console.Clear();
            RenderMap();

            Console.WriteLine("\nUsa WASD para moverte, Q para salir");
            Console.WriteLine("Mapa estilo Diablo - Habitaciones grandes con pasillos gruesos");
            Console.WriteLine($"Habitaciones generadas: {_rooms.Count}");

            // Leer input del jugador
            var key = Console.ReadKey(true).Key;

            switch (key)
            {
                case ConsoleKey.W:
                    TryMovePlayer(0, -1);
                    break;
                case ConsoleKey.S:
                    TryMovePlayer(0, 1);
                    break;
                case ConsoleKey.A:
                    TryMovePlayer(-1, 0);
                    break;
                case ConsoleKey.D:
                    TryMovePlayer(1, 0);
                    break;
                case ConsoleKey.Q:
                    gameRunning = false;
                    break;
            }
        }
    }

    private static void TryMovePlayer(int deltaX, int deltaY)
    {
        var newPosition = _player.Position + new Coord(deltaX, deltaY);

        // Verificar límites del mapa
        if (newPosition.X < 0 || newPosition.X >= _map.Width ||
            newPosition.Y < 0 || newPosition.Y >= _map.Height)
            return;

        // Verificar si la nueva posición es caminable
        if (_map.Terrain[newPosition].IsWalkable)
        {
            // Remover el jugador de su posición actual
            _map.RemoveEntity(_player);

            // Cambiar la posición del jugador
            _player.Position = newPosition;

            // Añadir el jugador en la nueva posición
            _map.AddEntity(_player);
        }
    }

    private static void RenderMap()
    {
        for (int y = 0; y < _map.Height; y++)
        {
            for (int x = 0; x < _map.Width; x++)
            {
                var coord = new Coord(x, y);

                // Verificar si hay una entidad en esta posición
                var entity = _map.GetEntity<IGameObject>(coord, 1);
                if (entity != null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write('@'); // Jugador
                    Console.ResetColor();
                }
                else
                {
                    // Mostrar terreno
                    var terrain = _map.Terrain[x, y];
                    RenderTerrain(terrain);
                }
            }
            Console.WriteLine();
        }
    }

    private static void RenderTerrain(IGameObject terrain)
    {
        char symbol;
        ConsoleColor color = ConsoleColor.White;

        if (!terrain.IsWalkable && !terrain.IsTransparent)
        {
            // Distinguir entre muros normales y pilares
            if (IsInRoom(terrain.Position))
            {
                symbol = 'O'; // Pilar
                color = ConsoleColor.DarkGray;
            }
            else
            {
                symbol = '#'; // Muro
                color = ConsoleColor.Gray;
            }
        }
        else if (!terrain.IsWalkable && terrain.IsTransparent)
        {
            symbol = '~'; // Agua
            color = ConsoleColor.Blue;
        }
        else if (terrain.IsWalkable && terrain.IsTransparent)
        {
            // Determinar tipo de suelo
            int hash = (terrain.Position.X * 73 + terrain.Position.Y * 37) % 100;
            if (hash < 3)
            {
                symbol = '"'; // Hierba/musgo
                color = ConsoleColor.Green;
            }
            else
            {
                symbol = '.'; // Suelo
                color = ConsoleColor.White;
            }
        }
        else
        {
            symbol = '.'; // Suelo por defecto
            color = ConsoleColor.White;
        }

        Console.ForegroundColor = color;
        Console.Write(symbol);
        Console.ResetColor();
    }

    private static bool IsInRoom(Coord position)
    {
        foreach (var room in _rooms)
        {
            if (room.Contains(position))
                return true;
        }
        return false;
    }
}