using Minesweeper.Models.DbModels;
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
						UnflagCell(cell);
					else
						FlagCell(cell);

					cell.Flaged = !cell.Flaged;
				}
			}

			private void UnflagCell(CellViewModel cell)
			{
				cell.Flaged = false;
				_vm.GameStatus.BombCellCount++;
				_vm.UpdateImage(cell, "block");
			}

			private void FlagCell(CellViewModel cell)
			{
				cell.Flaged = true;
				_vm.GameStatus.BombCellCount--;
				_vm.UpdateImage(cell, "flag");
			}

			public bool CanCheck(int index) =>
				!_vm.Cells[index].Clicked && !_vm.GameStatus.IsGameEnded;

			public bool CanToggleFlag(int index) =>
				(_vm.GameStatus.BombCellCount > 0 || _vm.Cells[index].Flaged) && !_vm.GameStatus.IsGameEnded;
		}

		public static RelayCommand CreateCheckBombCommand(MainViewModel vm)
		{
			var service = new CellActionService(vm);
			return CreateCommand(o => service.CheckBombAt((int)o), o => service.CanCheck((int)o));
		}

		public static RelayCommand CreateFlagCommand(MainViewModel vm)
		{
			var service = new CellActionService(vm);
			return CreateCommand(o => service.ToggleFlagAt((int)o), o => service.CanToggleFlag((int)o));
		}

		public static RelayCommand CreateShowFreeCellCommand(MainViewModel vm) =>
			BonusCommand(() => vm.GameManager.ShowFreeCellBonusQuantity > 0 && !vm.GameManager.IsEnd,
						 () => { vm.GameManager.ShowFreeCellBonus(); vm.UpdateGameStatus(true); });

		public static RelayCommand CreateShowBombCommand(MainViewModel vm)
		{
			return BonusCommand(
				() => vm.GameStatus?.BombCellCount > 0 && vm.GameManager?.ShowBombBonusQuantity > 0 && vm.Cells.Any(x => x.Clicked),
				() =>
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
				});
		}

		public static RelayCommand CreateSafeClickCommand(MainViewModel vm) =>
			BonusCommand(() => vm.GameManager.SafeClickBonusQuantity > 0,
						 () => vm.GameManager.SafeClickBonus());

		public static RelayCommand CreateInitializeGameCommand(MainViewModel vm) =>
			CreateCommand(o => vm.InitializeGame(vm.Rows, vm.Columns, vm.Difficulty));

		public static RelayCommand CreateBackToMenuCommand(MainViewModel vm) =>
			CreateCommand(o => vm.Menu.InitializeProp(false), o => !vm.Menu.IsMenu);

		public static RelayCommand CreateRegisterCommand(MenuViewModel vm, IGameRepository repository) =>
			CreateCommand(o => HandleAuth(vm, repository.RegisterUser, "Користувач з таким логіном вже існує!"));

		public static RelayCommand CreateLoginCommand(MenuViewModel vm, IGameRepository repository) =>
			CreateCommand(o => HandleAuth(vm, repository.LoginUser, "Неправильний логін та/або пароль!"));

		public static RelayCommand CreateChangeLoginAndRegisterCommand(MenuViewModel vm) =>
			CreateCommand(o =>
			{
				vm.Login = "";
				vm.Password = "";
				vm.Error = "";
				vm.IsLogin = !vm.IsLogin;
			});

		public static RelayCommand CreateChooseDifficultyCommand(MenuViewModel vm) =>
			CreateCommand(o =>
			{
				vm.Rows = "";
				vm.Columns = "";
				vm.IsChoosingDifficulty = !vm.IsChoosingDifficulty;
			});

		public static RelayCommand CreateStartCommand(MenuViewModel vm, MainViewModel mainVm) =>
			CreateCommand(o =>
			{
				if (vm.Rows == "" || vm.Columns == "")
				{
					vm.Error = "Заповніть всі поля!";
				}
				else if (int.TryParse(vm.Rows, out int rows) && int.TryParse(vm.Columns, out int columns))
				{
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
				else
				{
					vm.Error = "Введіть коректні числові значення!";
				}
			});

		public static RelayCommand CreateHistoryCommand(MenuViewModel vm, IGameRepository repository) =>
			CreateCommand(o =>
			{
				if ((string)o == "True")
				{
					vm.GameResults = repository.GetResults(vm.UserId);
				}
				vm.IsHistory = !vm.IsHistory;
			});

		public static RelayCommand CreateExitAccountCommand(MenuViewModel vm) =>
			CreateCommand(o => vm.InitializeProp(true));

		public static RelayCommand CreateExitCommand(MenuViewModel vm) =>
			CreateCommand(o => vm.InvokeExitRequest());


		private static RelayCommand CreateCommand(Action<object> execute, Func<object, bool>? canExecute = null) =>
			new RelayCommand(execute, canExecute);

		private static RelayCommand BonusCommand(Func<bool> canExecute, Action execute) =>
			new RelayCommand(o => execute(), o => canExecute());

		private static void HandleAuth(MenuViewModel vm, Func<string, string, int> authFunc, string errorOnFail)
		{
			if (!string.IsNullOrWhiteSpace(vm.Login) && !string.IsNullOrWhiteSpace(vm.Password))
			{
				int result = authFunc(vm.Login, vm.Password);
				if (result > 0)
				{
					vm.UserId = result;
					vm.IsLoginScreen = false;
				}
				else
				{
					vm.Error = errorOnFail;
				}
			}
			else
			{
				vm.Error = "Заповніть всі поля!";
			}
		}
	}
}
