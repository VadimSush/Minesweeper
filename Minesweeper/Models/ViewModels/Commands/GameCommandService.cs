﻿using Minesweeper.Models.DbModels;
using Minesweeper.Models.ViewModels.ObserverModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Minesweeper.Models.ViewModels.Commands
{
    public static class GameCommandService
    {
        public class CellActionService
        {
            private readonly MainViewModel _vm;

            public CellActionService(MainViewModel vm)
            {
                _vm = vm;
            }

            public void CheckBombAt(int index)
            {
                var cell = _vm.Cells[index];
                _vm.GameManager.ActivateCell(cell.X, cell.Y);
                cell.Clicked = true;

                if (_vm.GameManager.Field.Cells[cell.X, cell.Y].CellType == Field.CellType.None)
                {
                    _vm.UpdateGameStatus(true);
                }
                else
                {
                    _vm.UpdateImageByType(cell);
                    _vm.UpdateGameStatus(false);

                    if (cell.Flaged)
                    {
                        cell.Flaged = false;
                        _vm.GameStatus.BombCellCount++;
                    }
                }
            }

            public void ToggleFlagAt(int index)
            {
                var cell = _vm.Cells[index];

                if (!cell.Clicked)
                {
                    if (cell.Flaged)
                    {
                        _vm.UpdateImage(cell, "block");
                        _vm.GameStatus.BombCellCount++;
                    }
                    else
                    {
                        _vm.UpdateImage(cell, "flag");
                        _vm.GameStatus.BombCellCount--;
                    }

                    cell.Flaged = !cell.Flaged;
                }
            }

            public bool CanCheck(int index) =>
                !_vm.Cells[index].Clicked && !_vm.GameStatus.IsGameEnded;

            public bool CanToggleFlag(int index) =>
                (_vm.GameStatus.BombCellCount > 0 || _vm.Cells[index].Flaged) && !_vm.GameStatus.IsGameEnded;
        }

        public static RelayCommand CreateCheckBombCommand(MainViewModel vm)
        {
            var service = new CellActionService(vm);

            return new RelayCommand(
                execute: o => service.CheckBombAt((int)o),
                canExecute: o => service.CanCheck((int)o)
            );
        }

        public static RelayCommand CreateFlagCommand(MainViewModel vm)
        {
            var service = new CellActionService(vm);

            return new RelayCommand(
                execute: o => service.ToggleFlagAt((int)o),
                canExecute: o => service.CanToggleFlag((int)o)
            );
        }


        public static RelayCommand CreateShowFreeCellCommand(MainViewModel vm)
        {
            var execute = (object o) =>
            {
                vm.GameManager.ShowFreeCellBonus();
                vm.UpdateGameStatus(true);
            };

            var canExecute = (object o) =>
            {
                return vm.GameManager.ShowFreeCellBonusQuantity > 0;
            };

            return new RelayCommand(execute, canExecute);
        }

        public static RelayCommand CreateShowBombCommand(MainViewModel vm)
        {
            var execute = (object o) =>
            {
                vm.GameManager.ShowBombBonus();

                foreach (var cell in vm.Cells)
                {
                    if (!cell.Flaged && vm.GameManager.Field.Cells[cell.X, cell.Y].IsBomb)
                    {
                        cell.Flaged = true;
                        vm.UpdateImage(cell, "flag");
                        vm.GameStatus.BombCellCount--;
                        break;
                    }
                }
            };

            var canExecute = (object o) =>
            {
                return vm.GameStatus?.BombCellCount > 0 && vm.GameManager?.ShowBombBonusQuantity > 0 && vm.Cells.Where(x => x.Clicked).Any();
            };

            return new RelayCommand(execute, canExecute);
        }

        public static RelayCommand CreateSafeClickCommand(MainViewModel vm)
        {
            var execute = (object o) =>
            {
                vm.GameManager.SafeClickBonus();
            };

            var canExecute = (object o) =>
            {
                return vm.GameManager.SafeClickBonusQuantity > 0;
            };

            return new RelayCommand(execute, canExecute);
        }

        public static RelayCommand CreateInitializeGameCommand(MainViewModel vm)
        {
            var execute = (object o) =>
            {
                vm.InitializeGame(vm.Rows, vm.Columns, vm.Difficulty);
            };

            return new RelayCommand(execute);
        }

        public static RelayCommand CreateBackToMenuCommand(MainViewModel vm)
        {
            var execute = (object o) =>
            {
                vm.Menu.InitializeProp(false);
            };

            var canExecute = (object o) =>
            {
                return !vm.Menu.IsMenu;
            };

            return new RelayCommand(execute, canExecute);
        }

        private static bool IsLoginAndPasswordValid(MenuViewModel vm)
        {
            return !string.IsNullOrWhiteSpace(vm.Login) && !string.IsNullOrWhiteSpace(vm.Password);
        }

        public static RelayCommand CreateRegisterCommand(MenuViewModel vm, IGameRepository repository)
        {
            var execute = (object o) =>
            {
                if (IsLoginAndPasswordValid(vm))
                {
                    int result = repository.RegisterUser(vm.Login, vm.Password);

                    if (result > 0)
                    {
                        vm.UserId = result;
                        vm.IsLoginScreen = false;
                    }
                    else
                    {
                        vm.Error = "Користувач з таким логіном вже існує!";
                    }
                }
                else
                {
                    vm.Error = "Заповніть всі поля!";
                }
            };

            return new RelayCommand(execute);
        }


        public static RelayCommand CreateLoginCommand(MenuViewModel vm, IGameRepository repository)
        {
            var execute = (object o) =>
            {
                if (IsLoginAndPasswordValid(vm))
                {
                    int result = repository.LoginUser(vm.Login, vm.Password);

                    if (result > 0)
                    {
                        vm.UserId = result;
                        vm.IsLoginScreen = false;
                    }
                    else
                    {
                        vm.Error = "Неправильний логін та/або пароль!";
                    }
                }
                else
                {
                    vm.Error = "Заповніть всі поля!";
                }
            };

            return new RelayCommand(execute);
        }

        public static RelayCommand CreateChangeLoginAndRegisterCommand(MenuViewModel vm)
        {
            var execute = (object o) =>
            {
                vm.Login = "";
                vm.Password = "";
                vm.Error = "";
                vm.IsLogin = !vm.IsLogin;
            };

            return new RelayCommand(execute);
        }

        public static RelayCommand CreateChooseDifficultyCommand(MenuViewModel vm)
        {
            var execute = (object o) =>
            {
                vm.Rows = "";
                vm.Columns = "";
                vm.IsChoosingDifficulty = !vm.IsChoosingDifficulty;
            };

            return new RelayCommand(execute);
        }

        public static RelayCommand CreateStartCommand(MenuViewModel vm, MainViewModel mainVm)
        {
            var execute = (object o) =>
            {
                if (vm.Rows == "" || vm.Columns == "")
                {
                    vm.Error = "Заповніть всі поля!";
                }
                else
                {
                    int.TryParse(vm.Rows, out int rows);
                    int.TryParse(vm.Columns, out int columns);

                    if (rows < 5 || columns < 5 || rows > 40 || columns > 40)
                    {
                        vm.Error = "Введіть коректні значення: від 5 до 40";
                    }
                    else
                    {
                        string difficulty = (string)o;
                        vm.IsChoosingDifficulty = false;
                        vm.IsMenu = false;

                        mainVm.InitializeGame(rows, columns, difficulty);
                    }
                }
            };

            return new RelayCommand(execute);
        }

        public static RelayCommand CreateHistoryCommand(MenuViewModel vm, IGameRepository repository)
        {
            var execute = (object o) =>
            {
                if ((string)o == "True")
                {
                    vm.GameResults = repository.GetResults(vm.UserId);
                }
                vm.IsHistory = !vm.IsHistory;
            };

            return new RelayCommand(execute);
        }

        public static RelayCommand CreateExitAccountCommand(MenuViewModel vm)
        {
            var execute = (object o) =>
            {
                vm.InitializeProp(true);
            };

            return new RelayCommand(execute);
        }

        public static RelayCommand CreateExitCommand(MenuViewModel vm)
        {
            var execute = (object o) =>
            {
                vm.InvokeExitRequest();
            };

            return new RelayCommand(execute);
        }
    }
}
