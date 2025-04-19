﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.Json;
using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .5)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        public Board(GameSettings settings)
        {
            CellSize = settings.CellSize;

            Cells = new Cell[settings.Width / settings.CellSize, settings.Height / settings.CellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(settings.LiveDensity);
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        public void SaveToFile(string filename)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                writer.Write(Height);
                writer.Write(' ');
                writer.Write(Width);
                writer.Write(' ');
                writer.Write(CellSize);
                writer.Write('\n');
                for (int x = 0; x < Rows; x++)
                {
                    for (int y = 0; y < Columns; y++)
                    {
                        writer.Write(Cells[y, x].IsAlive ? '1' : '0');
                    }
                    writer.Write('\n');
                }
            }
        }

        public void LoadPattern(string fileName, int offsetX = 0, int offsetY = 0)
        {
            int yLen = Cells.Length / Cells.GetLength(0);
            int xLen = Cells.GetLength(0);
            string[] lines = File.ReadAllLines(fileName);
            for (int y = 0; y < Math.Min(lines.Length, yLen) ; y++)
            {
                for (int x = 0; x < Math.Min(lines[y].Length, xLen); x++)
                {
                    int targetX = (x + offsetX) % Columns;
                    int targetY = (y + offsetY) % Rows;
                    Cells[targetX, targetY].IsAlive = lines[y][x] == '1';
                }
            }
        }

    }
    public class GameSettings
    {
        public int Width { get; set; } = 50;
        public int Height { get; set; } = 20;
        public int CellSize { get; set; } = 1;
        public double LiveDensity { get; set; } = 0.5;
        public int Delay { get; set; } = 1000;
    }

    public class ClusterAnalyzer
    {
        public static List<HashSet<(int, int)>> FindClusters(Board board)
        {
            var clusters = new List<HashSet<(int, int)>>();
            var visited = new bool[board.Columns, board.Rows];

            for (int y = 0; y < board.Rows; y++)
            {
                for (int x = 0; x < board.Columns; x++)
                {
                    if (board.Cells[x, y].IsAlive && !visited[x, y])
                    {
                        var cluster = new HashSet<(int, int)>();
                        ExploreCluster(board, x, y, visited, cluster);
                        clusters.Add(cluster);
                    }
                }
            }

            return clusters;
        }

        private static void ExploreCluster(Board board, int x, int y, bool[,] visited, HashSet<(int, int)> cluster)
        {
            var queue = new Queue<(int, int)>();
            queue.Enqueue((x, y));
            visited[x, y] = true;

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                cluster.Add((cx, cy));

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;

                        int nx = (cx + dx + board.Columns) % board.Columns;
                        int ny = (cy + dy + board.Rows) % board.Rows;

                        if (board.Cells[nx, ny].IsAlive && !visited[nx, ny])
                        {
                            visited[nx, ny] = true;
                            queue.Enqueue((nx, ny));
                        }
                    }
                }
            }
        }

        public static string ClassifyCluster(HashSet<(int x, int y)> cluster, string patternsDir)
        {
            var normalized = NormalizeCluster(cluster);

            var templates = LoadTemplates(patternsDir);

            foreach (var (name, template) in templates)
            {
                if (AreClustersEqual(normalized, template))
                {
                    return name;
                }
            }

            return $"Неизвестная фигура ({cluster.Count} клеток)";
        }

        private static HashSet<(int x, int y)> NormalizeCluster(HashSet<(int x, int y)> cluster)
        {
            int minX = cluster.Min(p => p.x);
            int minY = cluster.Min(p => p.y);

            return [.. cluster.Select(p => (p.x - minX, p.y - minY))];
        }

        private static Dictionary<string, HashSet<(int x, int y)>> LoadTemplates(string dir)
        {
            var templates = new Dictionary<string, HashSet<(int x, int y)>>();

            foreach (var file in Directory.GetFiles(dir, "*.txt"))
            {
                var pattern = new HashSet<(int x, int y)>();
                string[] lines = File.ReadAllLines(file);

                for (int y = 0; y < lines.Length; y++)
                {
                    for (int x = 0; x < lines[y].Length; x++)
                    {
                        if (lines[y][x] == '1')
                        {
                            pattern.Add((x, y));
                        }
                    }
                }

                templates.Add(Path.GetFileNameWithoutExtension(file), pattern);
            }

            return templates;
        }

        private static bool AreClustersEqual(
            HashSet<(int x, int y)> cluster1,
            HashSet<(int x, int y)> cluster2)
        {
            if (cluster1.Count != cluster2.Count)
                return false;

            for (int rotation = 0; rotation < 4; rotation++)
            {
                var rotated = RotateCluster(cluster1, rotation);
                if (rotated.SetEquals(cluster2))
                    return true;
            }

            return false;
        }

        private static HashSet<(int x, int y)> RotateCluster(
            HashSet<(int x, int y)> cluster,
            int rotations)
        {
            var result = new HashSet<(int x, int y)>();
            int size = cluster.Max(p => Math.Max(p.x, p.y)) + 1;

            foreach (var (x, y) in cluster)
            {
                var (rx, ry) = (x, y);

                for (int i = 0; i < rotations; i++)
                {
                    (rx, ry) = (ry, size - 1 - rx);
                }

                result.Add((rx, ry));
            }

            return result;
        }
    }
    public class StabilityAnalyzer
    {
        private const int StabilityThreshold = 5;
        private Queue<int> history = new Queue<int>();

        public bool CheckStability(Board board)
        {
            int aliveCount = 0;
            for (int y = 0; y < board.Rows; y++)
                for (int x = 0; x < board.Columns; x++)
                    if (board.Cells[x, y].IsAlive)
                        aliveCount++;

            history.Enqueue(aliveCount);
            if (history.Count > StabilityThreshold)
                history.Dequeue();

            return history.Distinct().Count() == 1 && history.Count == StabilityThreshold;
        }
    }

    class Program
    {
        static Board board;
        static private int Delay;
        static StabilityAnalyzer stabilityAnalyzer = new StabilityAnalyzer();

        static private Board LoadFromFile(string filename)
        {
            using (StreamReader reader = new StreamReader(filename))
            {
                var str = reader.ReadLine().Split(' ');
                int rows = int.Parse(str[0]);
                int cols = int.Parse(str[1]);
                int cellSize = int.Parse(str[2]);
                Board board = new Board(cols * cellSize, rows * cellSize, cellSize);

                for (int y = 0; y < rows; y++)
                {
                    string line = reader.ReadLine();
                    for (int x = 0; x < cols; x++)
                    {
                        board.Cells[x, y].IsAlive = line[x] == '1';
                    }
                }
                return board;
            }

        }
        static private void Reset(string configPath, string loadPath, bool newRunning = true)
        {
            using (StreamReader r = new StreamReader(configPath))
            {
                string json = r.ReadToEnd();
                GameSettings settings = JsonConvert.DeserializeObject<GameSettings>(json);
                if (newRunning) {
                    board = new Board(settings);
                } else {
                    board = LoadFromFile(loadPath);
                }
                Delay = settings.Delay;
            }
        }
        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)   
                {
                    var cell = board.Cells[col, row];
                    if (cell.IsAlive)
                    {
                        Console.Write('*');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
        }

        static void printClustersInfo(string patternsPath)
        {
            var clusters = ClusterAnalyzer.FindClusters(board);
            Console.WriteLine($"\nclusters count: {clusters.Count}");
            foreach (var cluster in clusters.OrderBy(c => -c.Count))
            {
                Console.WriteLine($"{ClusterAnalyzer.ClassifyCluster(cluster, patternsPath)} (размер: {cluster.Count})");
            }
        }
        static void Main(string[] args)
        {
            string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            string configPath = Path.Combine(projectDirectory, "config.json");
            string filePath = Path.Combine(projectDirectory, "board.txt");
            string patternPath = Path.Combine(projectDirectory, "patterns");
            Reset(configPath, filePath, true);

            board.LoadPattern(patternPath + "/train.txt");

            int generation = 0;
            while(++generation < 0|| true)
            {
                Console.Clear();
                Render();

                if (stabilityAnalyzer.CheckStability(board))
                {
                    Console.WriteLine($"\nСистема стабилизировалась на поколении {generation}");
                    break;
                }

                board.Advance();
                Thread.Sleep(Delay);
            }

            board.SaveToFile(filePath);
            printClustersInfo(patternPath);
        }
    }
}