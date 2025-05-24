using Minesweeper.Models.DifficultyStrategy;
using Minesweeper.Models.Field;

namespace Minesweeper.Models
{
    public class GameManager
    {
        private const int FreeCellBonusPenalty = 500;
        private const int BombBonusPenalty = 1000;

        public Field.Field Field { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int Score { get; set; }
        public bool IsEnd { get; set; }
        public bool IsWin { get; set; }
        public int ShowFreeCellBonusQuantity { get; set; }
        public int ShowBombBonusQuantity { get; set; }
        public int SafeClickBonusQuantity { get; set; }
        public bool IsSafeClick { get; set; }
        public IDifficultyStrategy DifficultyStrategy { get; set; }


        public void Initialize(int rows, int columns, string difficulty)
        {
            Field = new Field.Field();
            IsEnd = false;
            IsWin = false;
            IsSafeClick = false;
            Score = 0;
            Rows = rows;
            Columns = columns;

            DifficultyStrategy = difficulty switch
            {
                "Expert" => new ExpertDifficultyStrategy(this),
                "Intermediate" => new IntermediateDifficultyStrategy(this),
                _ => new BeginnerDifficultyStrategy(this)
            };

            DifficultyStrategy.GenerateField();
            DifficultyStrategy.SetBonusesQuantity();
        }

        public void ActivateCell(int x, int y)
        {
            if (!IsWithinBounds(x, y))
                return;

            if (IsFirstMove() && Field.Cells[x, y].CellType != CellType.None)
            {
                RegenerateFieldUntilCellIsEmpty(x, y);
            }

            Cell cell = Field.Cells[x, y];
            if (cell.IsActivated)
                return;

            cell.Activate();

            if (cell.IsBomb)
            {
                HandleBombActivation();
            }
            else
            {
                DifficultyStrategy.UpdateScore(cell.CellType);
                Field.ActiveCellsRemain--;

                if (cell.CellType == CellType.None)
                {
                    ActivateAdjacentCells(x, y);
                }

                if (Field.ActiveCellsRemain == 0)
                {
                    IsEnd = true;
                    IsWin = true;
                }
            }

            IsSafeClick = false;
        }

        public void ShowFreeCellBonus()
        {
            if (ShowFreeCellBonusQuantity <= 0)
                return;

            AdjustScore(-FreeCellBonusPenalty);
            ShowFreeCellBonusQuantity--;

            if (!TryActivateFirstUnactivatedCell(CellType.None))
            {
                TryActivateFirstUnactivatedCellNotBomb();
            }
        }

        public void ShowBombBonus()
        {
            if (ShowBombBonusQuantity <= 0)
                return;

            AdjustScore(-BombBonusPenalty);
            ShowBombBonusQuantity--;
        }

        public void SafeClickBonus()
        {
            if (SafeClickBonusQuantity <= 0)
                return;

            IsSafeClick = true;
            SafeClickBonusQuantity--;
        }

        private bool IsWithinBounds(int x, int y) =>
            x >= 0 && y >= 0 && x < Rows && y < Columns;

        private bool IsFirstMove() =>
            Field.ActiveCellsRemain == Rows * Columns - Field.BombCount;

        private void RegenerateFieldUntilCellIsEmpty(int x, int y)
        {
            while (Field.Cells[x, y].CellType != CellType.None)
            {
                DifficultyStrategy.GenerateField();
            }
        }

        private void HandleBombActivation()
        {
            if (IsSafeClick)
            {
                Field.BombCount--;
            }
            else
            {
                IsEnd = true;
            }
        }

        private void ActivateAdjacentCells(int x, int y)
        {
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if ((i != 0 || j != 0) && IsWithinBounds(x + i, y + j))
                    {
                        ActivateCell(x + i, y + j);
                    }
                }
            }
        }

        private void AdjustScore(int delta)
        {
            Score += delta;
            if (Score < 0)
                Score = 0;
        }

        private bool TryActivateFirstUnactivatedCell(CellType type)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    if (Field.Cells[i, j].CellType == type && !Field.Cells[i, j].IsActivated)
                    {
                        ActivateCell(i, j);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool TryActivateFirstUnactivatedCellNotBomb()
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    if (Field.Cells[i, j].CellType != CellType.Bomb && !Field.Cells[i, j].IsActivated)
                    {
                        ActivateCell(i, j);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
