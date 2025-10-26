// File: App/Models.cs
// Core models used by services and the game loop.
using System;

namespace Minesweeper.App.Models
{
    public class BoardModel
    {
        public int Size { get; }
        public int Bombs { get; }
        public double DifficultyPercentage { get; set; }
        public DateTime? StartTime { get; set; }
        public int RewardsRemaining { get; set; }
        public Cell[,] Cells { get; }

        public BoardModel(int size, int bombs, int rewardsRemaining)
        {
            if (size < 2) throw new ArgumentOutOfRangeException(nameof(size));
            if (bombs < 1 || bombs >= size*size) throw new ArgumentOutOfRangeException(nameof(bombs));
            Size = size;
            Bombs = bombs;
            RewardsRemaining = rewardsRemaining;
            DifficultyPercentage = (double)bombs / (size*size) * 100.0;
            Cells = new Cell[size,size];
            for (int r=0;r<size;r++)
                for (int c=0;c<size;c++)
                    Cells[r,c] = new Cell();
        }
    }

    public class Cell
    {
        public bool IsBomb { get; set; }
        public bool IsVisited { get; set; }
        public bool IsFlagged { get; set; }
        public int NumberOfBombNeighbors { get; set; }
        public bool HasSpecialReward { get; set; }
    }
}
