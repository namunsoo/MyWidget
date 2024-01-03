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
		private Grid _grid;
		private int _Grid_Main_Padding;
		private int _realWindowMinWidth, _realWindowMinHeight;
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

		public MainWindow()
		{
			this.InitializeComponent();

			var messenger = Ioc.Default.GetService<IMessenger>();

			messenger.Register<BringTopMessage>(this, (r, m) =>
			{
				_appWindow.MoveInZOrderAtTop();
				this.SetForegroundWindow();
			});

			using (var manager = WinUIEx.WindowManager.Get(this))
			{
				manager.Width = 414;
				manager.Height = 614;
			}

			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			_appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(wndId);
			_realWindowMinWidth = _appWindow.Size.Width;
			_realWindowMinHeight = _appWindow.Size.Height;
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

			if (Grid_Content.Background is SolidColorBrush sb)
			{
				Grid_TitleBar.Background = new SolidColorBrush(Common.Style.GetColorDarkly(sb.Color, 0.1f));
			}

			_Grid_Main_Padding = (int)Grid_Main.Padding.Top;
		}


		#region [| 윈도우 이동 |]
		private void Grid_TitleBar_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Grid grid && e.OriginalSource == sender)
			{
				if (e.GetCurrentPoint(grid).Properties.IsLeftButtonPressed)
				{
					POINT mousePosition;
					POINT mouseDownPosition;
					var position = _appWindow.Position;

					GetCursorPos(out mousePosition);
					mouseDownPosition.X = mousePosition.X - position.X;
					mouseDownPosition.Y = mousePosition.Y - position.Y;

					while (IsLeftMouseButtonPressed())
					{
						GetCursorPos(out mousePosition);
						position.X = mousePosition.X - mouseDownPosition.X;
						position.Y = mousePosition.Y - mouseDownPosition.Y;
						_appWindow.Move(position);
					}
				}
			}
		}
		#endregion

		#region [| 윈도우 사이즈 변경 |]

		#region [| 윈도우 최소화 |]
		private void Grid_WidgetControlMinimize_Tapped(object sender, RoutedEventArgs e)
		{
			_appWindow_overlapped.Minimize();
		}
		#endregion

		#region [| 윈도우 최대화 |]
		private void Grid_WidgetControlMaximize_Tapped(object sender, RoutedEventArgs e)
		{
			if (_appWindow_overlapped.IsMaximizable)
			{
				Grid_Main.Padding = new Thickness(_Grid_Main_Padding);
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
		#endregion

		#region [| TitelBar 더블클릭 |]
		private void Grid_TitleBar_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (e.OriginalSource == sender)
			{
				if (_appWindow_overlapped.IsMaximizable)
				{
					Grid_Main.Padding = new Thickness(_Grid_Main_Padding);
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

		#region [| 테두리 포인터 변경 |]
		private void Grid_Main_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (!_appWindow_overlapped.IsMaximizable)
			{
				GridMain gridMain = sender as GridMain;
				POINT mousePosition;
				GetCursorPos(out mousePosition);

				gridMain.ChangeCursor(GetCursorShape(mousePosition));
			}
		}
		#endregion

		#region [| 윈도우 크기 변경 |]
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
					int realWindowWidth = _appWindow.Size.Width;
					int realWindowHeight = _appWindow.Size.Height;

					POINT windowPosition = new POINT { X = _appWindow.Position.X, Y = _appWindow.Position.Y };
					POINT pressedPosition = new POINT { X = (int)e.GetCurrentPoint(gridMain).Position.X, Y = (int)e.GetCurrentPoint(gridMain).Position.Y };
					bool isLeft = pressedPosition.X <= 2 * _Grid_Main_Padding;
					bool isTop = pressedPosition.Y <= 2 * _Grid_Main_Padding;
					bool isOnlyWidth = !isTop && pressedPosition.Y <= windowHeight - 2 * _Grid_Main_Padding;
					bool isOnlyHeight = !isLeft && pressedPosition.X <= windowWidth - 2 * _Grid_Main_Padding;
					int padding_x = windowWidth - pressedPosition.X;
					int padding_y = windowHeight - pressedPosition.Y;
					int width, height;
					POINT mousePosition;
					POINT windowMovePostion = new POINT();
					while (IsLeftMouseButtonPressed())
					{
						GetCursorPos(out mousePosition);
						if (isOnlyWidth)
						{
							width = isLeft ? realWindowWidth + windowPosition.X - mousePosition.X + pressedPosition.X : mousePosition.X - windowPosition.X + padding_x;
							height = realWindowHeight;
							windowMovePostion.X = isLeft ? mousePosition.X - pressedPosition.X : windowPosition.X;
							windowMovePostion.Y = windowPosition.Y;
						}
						else if (isOnlyHeight)
						{
							width = realWindowWidth;
							height = isTop ? realWindowHeight + windowPosition.Y - mousePosition.Y + pressedPosition.Y : mousePosition.Y - windowPosition.Y + padding_y;
							windowMovePostion.X = windowPosition.X;
							windowMovePostion.Y = isTop ? mousePosition.Y - pressedPosition.Y : windowPosition.Y;
						}
						else
						{
							width = isLeft ? realWindowWidth + windowPosition.X - mousePosition.X + pressedPosition.X : mousePosition.X - windowPosition.X + padding_x;
							height = isTop ? realWindowHeight + windowPosition.Y - mousePosition.Y + pressedPosition.Y : mousePosition.Y - windowPosition.Y + padding_y;
							windowMovePostion.X = isLeft ? mousePosition.X - pressedPosition.X : windowPosition.X;
							windowMovePostion.Y = isTop ? mousePosition.Y - pressedPosition.Y : windowPosition.Y;
						}
						await Task.Run(() => {
							_appWindow.MoveAndResize(new RectInt32(
								_realWindowMinWidth > width ? _appWindow.Position.X : windowMovePostion.X,
								_realWindowMinHeight > height ? _appWindow.Position.Y : windowMovePostion.Y,
								_realWindowMinWidth > width ? _realWindowMinWidth : width,
								_realWindowMinHeight > height ? _realWindowMinHeight : height
								));
						});
					}
					_appWindow_overlapped = (OverlappedPresenter)_appWindow.Presenter;
				}
			}
		}
		#endregion

		#region [| 마우스 포인터 아이콘 가져오기 |]
		private InputSystemCursor GetCursorShape(POINT mousePosition)
		{
			if (mousePosition.X < _appWindow.Position.X + _Grid_Main_Padding)
			{
				if (mousePosition.Y < _appWindow.Position.Y + 2 * _Grid_Main_Padding)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast);
				}
				else if (mousePosition.Y > _appWindow.Position.Y + _appWindow.Size.Height - 2 * _Grid_Main_Padding)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest);
				}
				else
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
				}
			}
			else if (mousePosition.X > _appWindow.Position.X + _appWindow.Size.Width - _Grid_Main_Padding)
			{
				if (mousePosition.Y < _appWindow.Position.Y + 2 * _Grid_Main_Padding)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest);
				}
				else if (mousePosition.Y > _appWindow.Position.Y + _appWindow.Size.Height - 2 * _Grid_Main_Padding)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast);
				}
				else
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
				}
			}
			else if (mousePosition.Y < _appWindow.Position.Y + _Grid_Main_Padding)
			{
				if (mousePosition.X < _appWindow.Position.X + 2 * _Grid_Main_Padding)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast);
				}
				else if (mousePosition.X > _appWindow.Position.X + _appWindow.Size.Width - 2 * _Grid_Main_Padding)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest);
				}
				else
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
				}
			}
			else if (mousePosition.Y > _appWindow.Position.Y + _appWindow.Size.Height - _Grid_Main_Padding)
			{
				if (mousePosition.X < _appWindow.Position.X + 2 * _Grid_Main_Padding)
				{
					return InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest);
				}
				else if (mousePosition.X > _appWindow.Position.X + _appWindow.Size.Width - 2 * _Grid_Main_Padding)
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
		#endregion

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