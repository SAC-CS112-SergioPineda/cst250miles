// File: App/Program.cs
// Terminal Minesweeper — Milestone 1 complete feature set.
using System;
using System.Globalization;

namespace Minesweeper.App
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.Title = "Minesweeper M1 (Terminal)";
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            int size = ReadInt("Board size (e.g., 10): ", 10, 2, 40);
            int mines = ReadInt($"Mines (1..{size*size-1}, e.g., {Math.Max(10, size)}): ", Math.Max(10, size), 1, size*size - 1);
            int rewards = ReadInt("Peek rewards (default 3): ", 3, 0, 999);

            var board = new Models.BoardModel(size: size, bombs: mines, rewardsRemaining: rewards);
            var svc = new Services.BoardService(new Random());

            // Setup
            svc.Setup(board);
            board.StartTime = DateTime.UtcNow;

            // Loop
            while (true)
            {
                Console.Clear();
                Render(board, revealAll:false);

                if (svc.IsWin(board))
                {
                    Console.WriteLine("\n🎉 You win! All safe cells revealed.");
                    PrintStats(board, won:true);
                    break;
                }

                Console.WriteLine();
                Console.WriteLine("Commands:");
                Console.WriteLine("  v r c   Visit/reveal cell at row r, col c");
                Console.WriteLine("  f r c   Flag/unflag cell");
                Console.WriteLine("  p r c   Peek (consume reward) shows neighbors count only");
                Console.WriteLine("  help    Show help");
                Console.WriteLine("  q       Quit");
                Console.Write("> ");

                var line = Console.ReadLine();
                if (line is null) continue;
                var parts = line.Trim().Split(new[]{' ', '\t', ','}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;
                var cmd = parts[0].ToLowerInvariant();

                try
                {
                    switch (cmd)
                    {
                        case "v":
                            RequireCoords(parts, out int vr, out int vc);
                            var hitBomb = svc.Visit(board, vr, vc);
                            if (hitBomb)
                            {
                                Console.Clear();
                                Render(board, revealAll:true);
                                Console.WriteLine("\n💥 Boom! You hit a bomb. Game over.");
                                PrintStats(board, won:false);
                                return;
                            }
                            break;

                        case "f":
                            RequireCoords(parts, out int fr, out int fc);
                            svc.ToggleFlag(board, fr, fc);
                            break;

                        case "p":
                            RequireCoords(parts, out int pr, out int pc);
                            var pe = svc.Peek(board, pr, pc);
                            if (!pe.Ok)
                            {
                                Console.WriteLine(pe.Message);
                                Pause();
                            }
                            break;

                        case "help":
                            // continue loop; header reprints
                            break;

                        case "q":
                            return;

                        default:
                            Console.WriteLine("Unknown command. Type 'help'.");
                            Pause();
                            break;
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                    Pause();
                }
            }
        }

        private static void Render(Models.BoardModel b, bool revealAll)
        {
            Console.WriteLine($"Minesweeper — {b.Size}x{b.Size}, Mines: {b.Bombs}  Diff: {b.DifficultyPercentage:F1}%  Rewards: {b.RewardsRemaining}");
            var elapsed = b.StartTime.HasValue ? DateTime.UtcNow - b.StartTime.Value : TimeSpan.Zero;
            Console.WriteLine($"Time: {elapsed:mm\\:ss}   Flags: {CountFlags(b)}   Revealed: {CountRevealed(b)}/{b.Size*b.Size}");
            Console.Write("    ");
            for (int c=0;c<b.Size;c++) Console.Write($"{c,2} ");
            Console.WriteLine();
            Console.WriteLine(new string('-', 4 + b.Size*3));
            for (int r=0;r<b.Size;r++)
            {
                Console.Write($"{r,2} |");
                for (int c=0;c<b.Size;c++)
                {
                    var cell = b.Cells[r,c];
                    char ch;
                    if (!revealAll)
                    {
                        if (cell.IsFlagged) ch = 'F';
                        else if (!cell.IsVisited) ch = '·';
                        else if (cell.IsBomb) ch = '*';
                        else ch = cell.NumberOfBombNeighbors == 0 ? ' ' : cell.NumberOfBombNeighbors.ToString(CultureInfo.InvariantCulture)[0];
                    }
                    else
                    {
                        if (cell.IsBomb) ch = '*';
                        else ch = cell.NumberOfBombNeighbors == 0 ? ' ' : cell.NumberOfBombNeighbors.ToString(CultureInfo.InvariantCulture)[0];
                    }
                    Console.Write($" {ch} ");
                }
                Console.WriteLine();
            }
            Console.WriteLine(new string('-', 4 + b.Size*3));
        }

        private static int CountRevealed(Models.BoardModel b)
        {
            int n=0;
            for (int r=0;r<b.Size;r++)
                for (int c=0;c<b.Size;c++)
                    if (b.Cells[r,c].IsVisited) n++;
            return n;
        }
        private static int CountFlags(Models.BoardModel b)
        {
            int n=0;
            for (int r=0;r<b.Size;r++)
                for (int c=0;c<b.Size;c++)
                    if (b.Cells[r,c].IsFlagged) n++;
            return n;
        }

        private static void PrintStats(Models.BoardModel b, bool won)
        {
            var elapsed = b.StartTime.HasValue ? DateTime.UtcNow - b.StartTime.Value : TimeSpan.Zero;
            Console.WriteLine($"Difficulty: {b.DifficultyPercentage:F1}%   Time: {elapsed:mm\\:ss}   Rewards left: {b.RewardsRemaining}");
        }

        private static void RequireCoords(string[] parts, out int r, out int c)
        {
            if (parts.Length < 3) throw new ArgumentException("Usage: <cmd> <row> <col>");
            if (!int.TryParse(parts[1], out r) || !int.TryParse(parts[2], out c))
                throw new ArgumentException("Row/Col must be integers.");
        }

        private static int ReadInt(string prompt, int @default, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s)) return @default;
                if (int.TryParse(s, out var v) && v>=min && v<=max) return v;
                Console.WriteLine($"Enter an integer in [{min}..{max}] or press Enter for {@default}.");
            }
        }

        private static void Pause()
        {
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();
        }
    }
}
