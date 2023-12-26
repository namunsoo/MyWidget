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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyWidget
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainWindow : Window
	{
		private AppWindow m_AppWindow;
		private OverlappedPresenter m_AppWindow_overlapped;
		private DisplayArea _displayArea;
		private Point _mouseDownLocation;
		private Grid _grid;
		public MainWindow()
		{
			this.InitializeComponent();

			var manager = WinUIEx.WindowManager.Get(this);
			manager.Width = 400;
			manager.Height = 600;

			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			m_AppWindow = AppWindow.GetFromWindowId(wndId);
			m_AppWindow_overlapped = (OverlappedPresenter)m_AppWindow.Presenter;
			if (m_AppWindow is not null)
			{
				_displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Nearest);
				if (_displayArea is not null)
				{
					var position = m_AppWindow.Position;
					position.X = ((_displayArea.WorkArea.Width - m_AppWindow.Size.Width) / 2);
					position.Y = ((_displayArea.WorkArea.Height - m_AppWindow.Size.Height) / 2);
					m_AppWindow.Move(position);
				}
			}
			if(Grid_Main.Background is SolidColorBrush sb)
			{
				Grid_TitleBar.Background = new SolidColorBrush(Common.Style.GetColorDarkly(sb.Color, 0.1f));
			}
			SB_CloseNewCalendar.Begin();
			SB_CloseNewMemo.Begin();
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

		private void Grid_TitleBar_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Grid grid && e.OriginalSource == sender)
			{
				if (e.GetCurrentPoint(grid).Properties.IsLeftButtonPressed)
				{
					POINT mousePosition;
					var position = m_AppWindow.Position;
					if (GetCursorPos(out mousePosition))
					{
						_mouseDownLocation.X = mousePosition.X - position.X;
						_mouseDownLocation.Y = mousePosition.Y - position.Y;
					}
				}
			}
		}

		private void Grid_TitleBar_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Grid grid && e.OriginalSource == sender)
			{
				if (e.GetCurrentPoint(grid).Properties.IsLeftButtonPressed)
				{
					POINT mousePosition;
					double point_X = 0;
					double point_y = 0;
					if (GetCursorPos(out mousePosition) && _displayArea is not null)
					{
						point_X = mousePosition.X - _mouseDownLocation.X;
						point_y = mousePosition.Y - _mouseDownLocation.Y;
						var position = m_AppWindow.Position;
						position.X = (int)point_X;
						position.Y = (int)point_y;
						m_AppWindow.Move(position);
					}
				}
			}
		}
		#endregion

		#region [| 윈도우 사이즈 변경 |]
		private void Grid_WidgetControlMinimize_Tapped(object sender, RoutedEventArgs e)
		{
			m_AppWindow_overlapped.Minimize();
		}

		private void Grid_WidgetControlMaximize_Tapped(object sender, RoutedEventArgs e)
		{
			if (m_AppWindow_overlapped.IsMaximizable)
			{
				m_AppWindow_overlapped.Restore();
				m_AppWindow_overlapped.IsMaximizable = false;
				FI_Maximize.Glyph = "\uE922";
			} else
			{
				m_AppWindow_overlapped.Maximize();
				m_AppWindow_overlapped.IsMaximizable = true;
				FI_Maximize.Glyph = "\uE923";
			}
		}

		private void Grid_TitleBar_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (e.OriginalSource == sender)
			{
				if (m_AppWindow_overlapped.IsMaximizable)
				{
					m_AppWindow_overlapped.Restore();
					m_AppWindow_overlapped.IsMaximizable = false;
					FI_Maximize.Glyph = "\uE922";
				}
				else
				{
					m_AppWindow_overlapped.Maximize();
					m_AppWindow_overlapped.IsMaximizable = true;
					FI_Maximize.Glyph = "\uE923";
				}
			}
		}
		#endregion

		#region [| 윈도우 닫기 |]
		private void Grid_WidgetControlClose_Tapped(object sender, RoutedEventArgs e)
		{
			App.m_window.Close();
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

		private void GridAdd_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			_grid = sender as Grid;
			_grid.Background = new SolidColorBrush(Common.Style.GetColorDarkly(((SolidColorBrush)ContentFrame.Background).Color, 0.05f));
		}

		private void GridAdd_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			_grid = sender as Grid;
			_grid.Background = new SolidColorBrush(Common.Style.GetColor("#00000000"));
		}
		#endregion

		#region [| 새로운 위젯 Open animation |]
		private static bool isOpenNewWidgets = false;
		private void Grid_OpenNewWidgets_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (!isOpenNewWidgets)
			{
				Grid_NewWidgets.Visibility = Visibility.Visible;
				SB_OpenNewCalendar.Begin();
				SB_OpenNewCalendar.Completed += (_, _) => { SB_OpenNewMemo.Begin(); };
				isOpenNewWidgets = true;
			} else
			{
				SB_CloseNewMemo.Begin();
				SB_CloseNewMemo.Completed += (_, _) => { SB_CloseNewCalendar.Begin(); };
				SB_CloseNewCalendar.Completed += (_, _) => { Grid_NewWidgets.Visibility = Visibility.Collapsed; };
				isOpenNewWidgets = false;
			}
		}
		#endregion

		#region [| 캘린더 위젯 추가 |]
		private void Grid_NewCalendar_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (App.calendar_window == null || App.calendar_window.AppWindow == null)
			{
				App.calendar_window = new CalendarWindow();
				App.calendar_window.ExtendsContentIntoTitleBar = true;
				App.calendar_window.Activate();
				// the bug test code follows
				var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.calendar_window);

				// Retrieve the WindowId that corresponds to hWnd.
				Microsoft.UI.WindowId windowId =
					Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

				// Lastly, retrieve the AppWindow for the current (XAML) WinUI 3 window.
				var appWindow = AppWindow.GetFromWindowId(windowId);

				if (appWindow.Presenter is OverlappedPresenter p)
				{
					p.SetBorderAndTitleBar(false, false);
					p.IsResizable = false;
				}
			}
            else if (App.calendar_window.AppWindow != null)
            {
				IntPtr hWnd = WindowNative.GetWindowHandle(App.calendar_window);
				Win32.ShowWindow(hWnd, (int)Win32.ShowWindowCommands.ShowNormal);
				Win32.SetForegroundWindow(hWnd);
            }
		}
		#endregion

		#region [| 메모 위젯 추가 |]
		private void Grid_NewMemo_Tapped(object sender, TappedRoutedEventArgs e)
		{
			MemoWindow memoWindow = new MemoWindow();
			App.memo_window.Add(memoWindow);
			memoWindow.ExtendsContentIntoTitleBar = true;
			memoWindow.Activate();
			// the bug test code follows
			var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(memoWindow);

			// Retrieve the WindowId that corresponds to hWnd.
			Microsoft.UI.WindowId windowId =
				Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

			// Lastly, retrieve the AppWindow for the current (XAML) WinUI 3 window.
			var appWindow = AppWindow.GetFromWindowId(windowId);

			if (appWindow.Presenter is OverlappedPresenter p)
			{
				p.SetBorderAndTitleBar(false, false);
				p.IsResizable = false;
			}
		}
		#endregion
	}
}
