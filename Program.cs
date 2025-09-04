using System;
using System.Collections.Generic;

namespace BSPConsoleDungeon
{
    class Program
    {
        static void Main(string[] args)
        {
            BSPDungeonGenerator generator = new BSPDungeonGenerator(
                width: 60,
                height: 30,
                minRoomSize: 6,
                maxDepth: 5
            );

            generator.Generate();
            generator.PrintMap();

            Console.WriteLine("\nDungeon generado con BSP. Presiona cualquier tecla para salir...");
            Console.ReadKey();
        }
    }
}