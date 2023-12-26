using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinUIEx;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using MyWidget.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyWidget.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MemoWindow : Window
    {
		private AppWindow _appWindow;
		private OverlappedPresenter _appWindow_overlapped;
		private DisplayArea _displayArea;
		private Point _mouseDownLocation;
		private Grid _grid;
		public MemoWindow()
		{
			this.InitializeComponent();

			var messenger = Ioc.Default.GetService<IMessenger>();

			messenger.Register<BringTop>(this, (r, m) =>
			{
				_appWindow.MoveInZOrderAtTop();
				this.SetForegroundWindow();
			});

			var manager = WinUIEx.WindowManager.Get(this);
			manager.Width = 400;
			manager.Height = 600;

			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			_appWindow = AppWindow.GetFromWindowId(wndId);
			_appWindow_overlapped = (OverlappedPresenter)_appWindow.Presenter;
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
			if (Grid_Main.Background is SolidColorBrush sb)
			{
				Grid_TitleBar.Background = new SolidColorBrush(Common.Style.GetColorDarkly(sb.Color, 0.1f));
			}
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
					var position = _appWindow.Position;
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
						var position = _appWindow.Position;
						position.X = (int)point_X;
						position.Y = (int)point_y;
						_appWindow.Move(position);
					}
				}
			}
		}
		#endregion

		#region [| 윈도우 사이즈 변경 |]
		private void Grid_TitleBar_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (e.OriginalSource == sender)
			{
				if (_appWindow_overlapped.IsMaximizable)
				{
					_appWindow_overlapped.Restore();
					_appWindow_overlapped.IsMaximizable = false;
				}
				else
				{
					_appWindow_overlapped.Maximize();
					_appWindow_overlapped.IsMaximizable = true;
				}
			}
		}
		#endregion

		#region [| 윈도우 닫기 |]
		private void Grid_WidgetControlClose_Tapped(object sender, RoutedEventArgs e)
		{
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

		private void Grid_Colors_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			_grid = sender as Grid;
			_grid.Background = new SolidColorBrush(Common.Style.GetColorDarkly(((SolidColorBrush)_grid.Background).Color, 0.05f));
		}

		private void Grid_Colors_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			_grid = sender as Grid;
			_grid.Background = new SolidColorBrush(Common.Style.GetColor(Convert.ToString(_grid.Tag)));
		}
		#endregion

		#region [| window focus? 효과 |]
		private void Window_Activated(object sender, WindowActivatedEventArgs args)
		{
			if (this.Show())
			{
				if (args.WindowActivationState == WindowActivationState.CodeActivated) { 
					AniOpenSetting.Begin();
				} else
				{
					AniCloseSetting.Begin();
					Grid_ColorOption.Visibility = Visibility.Collapsed;
					ColorOptionClose.Begin();
				}
			}
		}
		#endregion

		private void Grid_ColorChange_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (Grid_ColorOption.Visibility == Visibility.Collapsed)
			{
				Grid_ColorOption.Visibility = Visibility.Visible;
				ColorOptionOpen.Begin();
			} else
			{
				ColorOptionClose.Begin();
				ColorOptionClose.Completed += (_, _) => { Grid_ColorOption.Visibility = Visibility.Collapsed; };
			}
		}

		private void Grid_ColorOption_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (e.OriginalSource == sender)
			{
				ColorOptionClose.Begin();
				ColorOptionClose.Completed += (_, _) => { Grid_ColorOption.Visibility = Visibility.Collapsed; };
			}
		}

		private void Grid_Color_Tapped(object sender, TappedRoutedEventArgs e)
		{
			_grid = sender as Grid;

			Grid_Content.Background = new SolidColorBrush(Common.Style.GetColor(Convert.ToString(_grid.Tag)));
			Grid_TitleBar.Background = new SolidColorBrush(Common.Style.GetColorDarkly(Common.Style.GetColor(Convert.ToString(_grid.Tag)), 0.1f));
		}
	}
}
