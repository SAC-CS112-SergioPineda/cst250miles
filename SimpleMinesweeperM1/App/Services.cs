// File: App/Services.cs
// Board logic: setup, visit (with flood fill), flag, peek.
using System;
using Minesweeper.App.Models;

namespace Minesweeper.App.Services
{
    public interface IBoardService
    {
        void Setup(BoardModel b);
        bool Visit(BoardModel b, int r, int c);                 // returns true if bomb hit
        void ToggleFlag(BoardModel b, int r, int c);
        (bool Ok, string Message) Peek(BoardModel b, int r, int c);
        bool IsWin(BoardModel b);
    }

    public sealed class BoardService : IBoardService
    {
        private readonly Random _rng;
        public BoardService(Random? rng = null) => _rng = rng ?? new Random();

        public void Setup(BoardModel b)
        {
            PlaceBombs(b);
            CountNeighbors(b);
            DistributeRewards(b);
        }

        public bool Visit(BoardModel b, int r, int c)
        {
            RequireInBounds(b, r, c);
            var cell = b.Cells[r,c];
            if (cell.IsFlagged || cell.IsVisited) return false;

            cell.IsVisited = true;
            if (cell.IsBomb) return true;

            if (cell.HasSpecialReward)
            {
                cell.HasSpecialReward = false;
                b.RewardsRemaining += 1; // bonus
            }

            if (cell.NumberOfBombNeighbors == 0)
                FloodReveal(b, r, c);

            return false;
        }

        public void ToggleFlag(BoardModel b, int r, int c)
        {
            RequireInBounds(b, r, c);
            var cell = b.Cells[r,c];
            if (cell.IsVisited) return;
            cell.IsFlagged = !cell.IsFlagged;
        }

        public (bool Ok, string Message) Peek(BoardModel b, int r, int c)
        {
            RequireInBounds(b, r, c);
            if (b.RewardsRemaining <= 0) return (false, "No peek rewards left.");
            var cell = b.Cells[r,c];
            if (cell.IsVisited) return (false, "Cell already revealed.");
            // Peek: reveal only the number temporarily by marking visited then undoing bomb reveal
            // For simplicity, mark visited but if it's a bomb, don't end game; keep it hidden as '.'
            // We'll implement a lightweight peek: print the count and decrement reward.
            b.RewardsRemaining--;
            Console.WriteLine($"Peek [{r},{c}] → {(cell.IsBomb ? "*" : cell.NumberOfBombNeighbors.ToString())}");
            return (true, "OK");
        }

        public bool IsWin(BoardModel b)
        {
            for (int r=0;r<b.Size;r++)
                for (int c=0;c<b.Size;c++)
                {
                    var cell = b.Cells[r,c];
                    if (!cell.IsBomb && !cell.IsVisited) return false;
                }
            return true;
        }

        private void PlaceBombs(BoardModel b)
        {
            int placed = 0;
            while (placed < b.Bombs)
            {
                int r = _rng.Next(b.Size);
                int c = _rng.Next(b.Size);
                if (!b.Cells[r,c].IsBomb)
                {
                    b.Cells[r,c].IsBomb = true;
                    placed++;
                }
            }
        }

        private void CountNeighbors(BoardModel b)
        {
            for (int r=0;r<b.Size;r++)
                for (int c=0;c<b.Size;c++)
                    b.Cells[r,c].NumberOfBombNeighbors = CountBombNeighbors(b, r, c);
        }

        private int CountBombNeighbors(BoardModel b, int r, int c)
        {
            int n=0;
            for (int dr=-1;dr<=1;dr++)
                for (int dc=-1;dc<=1;dc++)
                {
                    if (dr==0 && dc==0) continue;
                    int nr=r+dr, nc=c+dc;
                    if (InBounds(b, nr, nc) && b.Cells[nr,nc].IsBomb) n++;
                }
            return n;
        }

        private void FloodReveal(BoardModel b, int r, int c)
        {
            var stack = new System.Collections.Generic.Stack<(int r, int c)>();
            stack.Push((r,c));
            while (stack.Count>0)
            {
                var (cr, cc) = stack.Pop();
                for (int dr=-1; dr<=1; dr++)
                    for (int dc=-1; dc<=1; dc++)
                    {
                        int nr = cr+dr, nc = cc+dc;
                        if (!InBounds(b, nr, nc)) continue;
                        var ncell = b.Cells[nr,nc];
                        if (ncell.IsVisited || ncell.IsFlagged || ncell.IsBomb) continue;
                        ncell.IsVisited = true;
                        if (ncell.HasSpecialReward)
                        {
                            ncell.HasSpecialReward = false;
                            b.RewardsRemaining += 1;
                        }
                        if (ncell.NumberOfBombNeighbors == 0)
                            stack.Push((nr,nc));
                    }
            }
        }

        private void DistributeRewards(BoardModel b)
        {
            int rewardCells = Math.Max(1, b.Size*b.Size / 25); // ~4% of cells
            int placed = 0;
            while (placed < rewardCells)
            {
                int r = _rng.Next(b.Size);
                int c = _rng.Next(b.Size);
                var cell = b.Cells[r,c];
                if (!cell.IsBomb && !cell.HasSpecialReward)
                {
                    cell.HasSpecialReward = true;
                    placed++;
                }
            }
        }

        private static void RequireInBounds(BoardModel b, int r, int c)
        {
            if (!InBounds(b, r, c)) throw new ArgumentException("Row/Col out of bounds.");
        }

        private static bool InBounds(BoardModel b, int r, int c)
            => r>=0 && r<b.Size && c>=0 && c<b.Size;
    }
}
