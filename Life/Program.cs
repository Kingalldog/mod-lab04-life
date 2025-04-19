using System;
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

    }
    public class GameSettings
    {
        public int Width { get; set; } = 50;
        public int Height { get; set; } = 20;
        public int CellSize { get; set; } = 1;
        public double LiveDensity { get; set; } = 0.5;
        public int Delay { get; set; } = 1000;
    }
    class Program
    {
        static Board board;
        static private int Delay;

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
        static void Main(string[] args)
        {
            string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            string configPath = Path.Combine(projectDirectory, "config.json");
            string filePath = Path.Combine(projectDirectory, "board.txt");
            string patternPath = Path.Combine(projectDirectory, "patterns/2.txt");
            Reset(configPath, patternPath, false);

            int cnt = 0;
            while(++cnt < 100)
            {
                Console.Clear();
                Render();
                board.Advance();
                Thread.Sleep(Delay);
            }
            board.SaveToFile(filePath);
        }
    }
}