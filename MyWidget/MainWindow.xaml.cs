using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;
using WinUIEx;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Hosting;
using MyWidget.Windows;
using MyWidget.Helpers;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading;
using Windows.Devices.Input;
using Microsoft.UI.Input;
using System.Reflection;
using Windows.UI.WindowManagement;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyWidget
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainWindow : Window
	{
		private Microsoft.UI.Windowing.AppWindow _appWindow;
		private OverlappedPresenter _appWindow_overlapped;
		private DisplayArea _displayArea;
		private Point _mouseDownLocation;
		private Grid _grid;
		public MainWindow()
		{
			this.InitializeComponent();

			var messenger = Ioc.Default.GetService<IMessenger>();

			messenger.Register<BringTop>(this, (r, m) =>
			{
				_appWindow.MoveInZOrderAtTop();
				this.SetForegroundWindow();
			});

			using (var manager = WinUIEx.WindowManager.Get(this))
			{
				manager.Width = 414;
				manager.Height = 614;
				manager.MinWidth = 414;
				manager.MinHeight = 614;
			}

			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			_appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(wndId);
			_appWindow_overlapped = (OverlappedPresenter)_appWindow.Presenter;
			_appWindow_overlapped.IsMaximizable = false;
			if (_appWindow is not null)
			{
				_displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Nearest);
				if (_displayArea is not null)
				{
					var position = _appWindow.Position;
					position.X = ((_displayArea.WorkArea.Width - _appWindow.Size.Width) / 2);
					position.Y = ((_displayArea.WorkArea.Height - _appWindow.Size.Height) / 2);
					_appWindow.Move(position);
				}
			}

			if (ContentFrame.Background is SolidColorBrush sb)
			{
				Grid_TitleBar.Background = new SolidColorBrush(Common.Style.GetColorDarkly(sb.Color, 0.1f));
			}

			//new Thread(() =>
			//{
			//	while (true)
			//	{
			//		POINT mousePosition;
			//		GetCursorPos(out mousePosition);

			//		if (mousePosition.X < _appWindow.Position.X - 10)
			//		{
			//			Cursor.Current = Cursors.Hand;
			//			// Mouse is on the left side
			//			// Do something...
			//		}

			//		// Check if the mouse is on the right side
			//		if (mousePosition.X > _appWindow.Position.X + _appWindow.Size.Width + 10)
			//		{
			//			// Mouse is on the right side
			//			// Do something...
			//		}

			//		// Check if the mouse is on the top side
			//		if (mousePosition.Y < _appWindow.Position.Y - 10)
			//		{
			//			Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeWestEast, 1);
			//			// Mouse is on the top side
			//			// Do something...
			//		}

			//		// Check if the mouse is on the bottom side
			//		if (mousePosition.Y > _appWindow.Position.Y + _appWindow.Size.Height + 10)
			//		{
			//			// Mouse is on the bottom side
			//			// Do something...
			//		}

			//		Thread.Sleep(100);
			//	}
			//}).Start();
		}

		#region [| 윈도우 이동 |]
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetCursorPos(out POINT lpPoint);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetAsyncKeyState(int vKey);
		const int VK_LBUTTON = 0x01;
		private bool IsLeftMouseButtonPressed()
		{
			return GetAsyncKeyState(VK_LBUTTON) & 0x8000 != 0;
		}

		private void Grid_TitleBar_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Grid grid && e.OriginalSource == sender)
			{
				if (e.GetCurrentPoint(grid).Properties.IsLeftButtonPressed)
				{
					POINT mousePosition;
					double point_X = 0;
					double point_y = 0;
					var position = _appWindow.Position;
					if (GetCursorPos(out mousePosition))
					{
						_mouseDownLocation.X = mousePosition.X - position.X;
						_mouseDownLocation.Y = mousePosition.Y - position.Y;
					}

					while (IsLeftMouseButtonPressed())
					{
						GetCursorPos(out mousePosition);
						point_X = mousePosition.X - _mouseDownLocation.X;
						point_y = mousePosition.Y - _mouseDownLocation.Y;
						position.X = (int)point_X;
						position.Y = (int)point_y;
						_appWindow.Move(position);
					}
				}
			}
		}
		#endregion

		#region [| 윈도우 사이즈 변경 |]
		private void Grid_WidgetControlMinimize_Tapped(object sender, RoutedEventArgs e)
		{
			_appWindow_overlapped.Minimize();
		}

		private void Grid_WidgetControlMaximize_Tapped(object sender, RoutedEventArgs e)
		{
			if (_appWindow_overlapped.IsMaximizable)
			{
				Grid_Main.Padding = new Thickness(7);
				_appWindow_overlapped.Restore();
				_appWindow_overlapped.IsMaximizable = false;
				FI_Maximize.Glyph = "\uE922";
			} else
			{
				Grid_Main.Padding = new Thickness(0);
				_appWindow_overlapped.Maximize();
				_appWindow_overlapped.IsMaximizable = true;
				FI_Maximize.Glyph = "\uE923";
			}
		}

		private void Grid_TitleBar_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (e.OriginalSource == sender)
			{
				if (_appWindow_overlapped.IsMaximizable)
				{
					Grid_Main.Padding = new Thickness(7);
					_appWindow_overlapped.Restore();
					_appWindow_overlapped.IsMaximizable = false;
					FI_Maximize.Glyph = "\uE922";
				}
				else
				{
					Grid_Main.Padding = new Thickness(0);
					_appWindow_overlapped.Maximize();
					_appWindow_overlapped.IsMaximizable = true;
					FI_Maximize.Glyph = "\uE923";
				}
			}
		}
		#endregion

		#region [| 윈도우 닫기 |]
		private void Grid_WidgetControlClose_Tapped(object sender, RoutedEventArgs e)
		{
			Common.WidgetOptions.SetMainWindow(false);
			this.Close();
		}
		#endregion

		#region [| gird mouse over 효과 |]
		private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			_grid = sender as Grid;
			_grid.Background = new SolidColorBrush(Common.Style.GetColorDarkly(((SolidColorBrush)Grid_TitleBar.Background).Color, 0.05f));
		}

		private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			_grid = sender as Grid;
			_grid.Background = new SolidColorBrush(Common.Style.GetColor("#00000000"));
		}
		#endregion

		#region [| 메모 위젯 추가 |]
		private void Grid_NewMemo_Tapped(object sender, TappedRoutedEventArgs e)
		{
			MemoWindow memoWindow = new MemoWindow(default(Guid));
			App.memo_window.Add(memoWindow);
			memoWindow.ExtendsContentIntoTitleBar = true;
			memoWindow.Activate();
			// the bug test code follows
			var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(memoWindow);

			// Retrieve the WindowId that corresponds to hWnd.
			Microsoft.UI.WindowId windowId =
				Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

			// Lastly, retrieve the AppWindow for the current (XAML) WinUI 3 window.
			var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

			if (appWindow.Presenter is OverlappedPresenter p)
			{
				p.SetBorderAndTitleBar(false, false);
				p.IsResizable = false;
			}
		}
		#endregion

		//const int SPI_SETCURSORS = 0x0057;
		//const int IDC_HAND = 32649;
		//const int IDC_ARROW = 32512;

		//[DllImport("user32.dll", SetLastError = true)]
		//static extern bool SystemParametersInfo(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);

		//[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		//static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

		//public InputCursor ProtectedCursor { get; private set; }

		//private void Grid_Main_PointerEntered(object sender, PointerRoutedEventArgs e)
		//{
		//	IntPtr handCursor = LoadCursor(IntPtr.Zero, IDC_HAND);
		//	// Set the cursor system-wide
		//	SystemParametersInfo(SPI_SETCURSORS, 0, handCursor, 0);

		//	// Do your other operations here

		//	// Change the cursor back to the default cursor if needed
		//	//SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, 0);
		//}

		//public void ChangeCursor(InputCursor cursor)
		//{
		//	this.ProtectedCursor = cursor;
		//}

		private void Grid_Main_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (!_appWindow_overlapped.IsMaximizable)
			{
				GridMain gridMain = sender as GridMain;
				POINT mousePosition;
				GetCursorPos(out mousePosition);

				if (mousePosition.X < _appWindow.Position.X + 7)
				{
					if (mousePosition.Y < _appWindow.Position.Y + 14)
					{
						gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast));
					}
					else if (mousePosition.Y > _appWindow.Position.Y + _appWindow.Size.Height - 14)
					{
						gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest));
					}
					else
					{
						gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
					}
				}
				else if (mousePosition.X > _appWindow.Position.X + _appWindow.Size.Width - 7)
				{
					if (mousePosition.Y < _appWindow.Position.Y + 14)
					{
						gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest));
					}
					else if (mousePosition.Y > _appWindow.Position.Y + _appWindow.Size.Height - 14)
					{
						gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast));
					}
					else
					{
						gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
					}
				}
				else if (mousePosition.Y < _appWindow.Position.Y + 7)
				{
					if (mousePosition.X < _appWindow.Position.X + 14)
					{
						gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast));
					}
					else if (mousePosition.X > _appWindow.Position.X + _appWindow.Size.Width - 14)
					{
						gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest));
					}
					else
					{
						gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth));
					}
				}
				else if (mousePosition.Y > _appWindow.Position.Y + _appWindow.Size.Height - 7)
				{
					if (mousePosition.X < _appWindow.Position.X + 14)
					{
						gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest));
					}
					else if (mousePosition.X > _appWindow.Position.X + _appWindow.Size.Width - 14)
					{
						gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast));
					}
					else
					{
						gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth));
					}
				}
				else
				{
					gridMain.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
				}
			}

		}

		private async void Grid_Main_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is GridMain gridMain && e.OriginalSource == sender)
			{
				if (e.GetCurrentPoint(gridMain).Properties.IsLeftButtonPressed)
				{
					int windowWidth, windowHeight;
					using (var manager = WinUIEx.WindowManager.Get(this))
					{
						windowWidth = (int)manager.Width;
						windowHeight = (int)manager.Height;

					}

					var position = _appWindow.Position;
					Point pressed = e.GetCurrentPoint(gridMain).Position;
					int padding_x = windowWidth - (int)pressed.X;
					int padding_y = windowHeight - (int)pressed.Y;
					int width, height;
					POINT mousePosition;
					while (IsLeftMouseButtonPressed())
					{
						GetCursorPos(out mousePosition);
						width = mousePosition.X - position.X + padding_x;
						height = mousePosition.Y - position.Y + padding_y;
						await Task.Run(() => {
							_appWindow.MoveAndResize(new RectInt32(position.X, position.Y, width, height));
						});
					}
					_appWindow_overlapped = (OverlappedPresenter)_appWindow.Presenter;
				}
			}
		}

		private InputSystemCursor GetCursorShape(POINT mousePosition)
		{
			if (mousePosition.X < _appWindow.Position.X + 7)
			{
				if (mousePosition.Y < _appWindow.Position.Y + 14)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast);
				}
				else if (mousePosition.Y > _appWindow.Position.Y + _appWindow.Size.Height - 14)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest);
				}
				else
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
				}
			}
			else if (mousePosition.X > _appWindow.Position.X + _appWindow.Size.Width - 7)
			{
				if (mousePosition.Y < _appWindow.Position.Y + 14)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest);
				}
				else if (mousePosition.Y > _appWindow.Position.Y + _appWindow.Size.Height - 14)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast);
				}
				else
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
				}
			}
			else if (mousePosition.Y < _appWindow.Position.Y + 7)
			{
				if (mousePosition.X < _appWindow.Position.X + 14)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast);
				}
				else if (mousePosition.X > _appWindow.Position.X + _appWindow.Size.Width - 14)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest);
				}
				else
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
				}
			}
			else if (mousePosition.Y > _appWindow.Position.Y + _appWindow.Size.Height - 7)
			{
				if (mousePosition.X < _appWindow.Position.X + 14)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest);
				}
				else if (mousePosition.X > _appWindow.Position.X + _appWindow.Size.Width - 14)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast);
				}
				else
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
				}
			}
			else
			{
				return InputSystemCursor.Create(InputSystemCursorShape.Arrow);
			}
		}
	}
	public class GridMain : Grid
	{
		public GridMain()
		{
		}
		public void ChangeCursor(InputCursor cursor)
		{
			this.ProtectedCursor = cursor;
		}

		public InputCursor GetCursor()
		{
			return this.ProtectedCursor;
		}
	}
}