using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MyWidget.Pages.Calendar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;
using MyWidget.Helpers;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyWidget.Windows
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class CalendarWindow : Window
	{
		private AppWindow _appWindow;
		private OverlappedPresenter _appWindow_overlapped;
		private DisplayArea _displayArea;
		private Point _mouseDownLocation;
		private Grid _grid;
		public CalendarWindow()
		{
			this.InitializeComponent();

			var messenger = Ioc.Default.GetService<IMessenger>();

			messenger.Register<BringTop>(this, (r, m) =>
			{
				_appWindow.MoveInZOrderAtTop();
			});

			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			_appWindow = AppWindow.GetFromWindowId(wndId);
			_appWindow_overlapped = (OverlappedPresenter)_appWindow.Presenter;
			if (_appWindow is not null)
			{
				_displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Nearest);
			}
			Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(
				Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
				new Microsoft.UI.Dispatching.DispatcherQueueHandler(() =>
				{
					WindowHelper.SetMica(true, false);
					WindowHelper.SetAcrylic(false, false);
					Grid_Main.Background = new SolidColorBrush(Colors.Transparent);
					//Helpers.Window.SetBlur(true, false);
					GetMyOptions();
				}));
			ContentFrame.Navigate(typeof(Month));
		}
		#region [| 기본 설정 가져오기 |]
		private void GetMyOptions()
		{
			var manager = WinUIEx.WindowManager.Get(this);
			string[] data = { "blur_true", "#00000000", "800", "800", };
			FileInfo fi = new FileInfo("C:\\MyWidget\\CalendarOptions.txt");
			if (fi.Exists)
			{
				using (StreamReader reader = new StreamReader("C:\\MyWidget\\CalendarOptions.txt"))
				{
					data = reader.ReadToEnd().Split(',');
				}
			}
			else
			{
				using (StreamWriter writer = new StreamWriter("C:\\MyWidget\\CalendarOptions.txt"))
				{
					writer.Write("blur_true,#00000000,800,750,");
					writer.Write(_displayArea == null ? "0,0" : ((_displayArea.WorkArea.Width - _appWindow.Size.Width) / 2) + "," + ((_displayArea.WorkArea.Height - _appWindow.Size.Height) / 2));
				}
			}

			TogSw_Blur.IsOn = data[0].Equals("blur_true");
			Grid_Main.Background = new SolidColorBrush(Common.Style.GetColor(data[1]));
			manager.Width = Convert.ToInt32(data[2]);
			manager.Height = Convert.ToInt32(data[3]);
			if (_appWindow is not null && _displayArea is not null)
			{
				var position = _appWindow.Position;
				position.X = data.Length == 4 ? ((_displayArea.WorkArea.Width - _appWindow.Size.Width) / 2) : Convert.ToInt32(data[4]);
				position.Y = data.Length == 4 ? ((_displayArea.WorkArea.Height - _appWindow.Size.Height) / 2) : Convert.ToInt32(data[5]);
				_appWindow.Move(position);
			}

			if (Grid_Main.Background is SolidColorBrush sb)
			{
				Grid_TitleBar.Background = new SolidColorBrush(Common.Style.GetColorDarkly(sb.Color, 0.1f));
			}
		}
		#endregion

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

		private void Grid_TitleBar_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			if (sender is Grid grid && e.OriginalSource == sender)
			{
				if (!e.GetCurrentPoint(grid).Properties.IsLeftButtonPressed)
				{
					FileInfo fi = new FileInfo("C:\\MyWidget\\CalendarOptions.txt");
					if (fi.Exists)
					{
						string[] data = null;
						using (StreamReader reader = new StreamReader("C:\\MyWidget\\CalendarOptions.txt"))
						{
							data = reader.ReadToEnd().Split(',');
						}
						using (StreamWriter writer = new StreamWriter("C:\\MyCalendarAssets\\myOptions.txt"))
						{
							data[4] = Convert.ToString(_appWindow.Position.X);
							data[5] = Convert.ToString(_appWindow.Position.Y);
							for (int i = 0; i < data.Length; i++)
							{
								writer.Write(data[i]);
								if (i+1 != data.Length)
								{
									writer.Write(",");
								}
							}
						}
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
				_appWindow_overlapped.Restore();
				_appWindow_overlapped.IsMaximizable = false;
				FI_Maximize.Glyph = "\uE922";
			}
			else
			{
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
					_appWindow_overlapped.Restore();
					_appWindow_overlapped.IsMaximizable = false;
					FI_Maximize.Glyph = "\uE922";
				}
				else
				{
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
			App.calendar_window.Close();
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

		#region [| 캘린더 설정 변경 |]
		private void Grid_CalendarOptions_Tapped(object sender, TappedRoutedEventArgs e)
		{
			Grid_Options_Space.Visibility = Visibility.Visible;
		}

		private void Grid_Options_Space_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (sender is Grid grid && e.OriginalSource == sender)
			{ 
				Grid_Options_Space.Visibility = Visibility.Collapsed;
			}
		}

		private void TogSw_Blur_Toggled(object sender, RoutedEventArgs e)
		{
			ToggleSwitch toggleSwitch = sender as ToggleSwitch;
			if (toggleSwitch != null)
			{
				WindowHelper.hWnd = WindowHelper.GetHWnd(this);
				if (toggleSwitch.IsOn == true)
				{
					WindowHelper.SetBlur(true, false);
				}
				else
				{
					WindowHelper.SetBlur(false, false);
				}
			}
		}

		private void SizeAndPoint_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
		{
			if (NBox_WindowWith.IsLoaded && NBox_WindowHeight.IsLoaded)
			{
				var manager = WinUIEx.WindowManager.Get(this);
				manager.Width = NBox_WindowWith.Value < 800 ? 800 : NBox_WindowWith.Value;
				manager.Height = NBox_WindowHeight.Value < 800 ? 800 : NBox_WindowHeight.Value;

			}
		}

		private void CP_BackgroundColor_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
		{
			Grid_Main.Background = new SolidColorBrush(sender.Color);
			Grid_TitleBar.Background = new SolidColorBrush(Common.Style.GetColorDarkly(sender.Color, 0.1f));
		}

		private void Btn_OptionSave_Click(object sender, RoutedEventArgs e)
		{
			FileInfo fi = new FileInfo("C:\\MyWidget\\CalendarOptions.txt");
			string options = string.Empty;
			options += TogSw_Blur.IsOn ? "blur_true," : "blur_false,";
			options += CP_BackgroundColor.Color.ToString() + ",";
			options += NBox_WindowWith.Value + ",";
			options += NBox_WindowHeight.Value + ",";
			options += Convert.ToString(_appWindow.Position.X) + ",";
			options += Convert.ToString(_appWindow.Position.Y);
			using (StreamWriter writer = new StreamWriter("C:\\MyWidget\\CalendarOptions.txt"))
			{
				writer.Write(options);
			}
		}
		#endregion
	}
}
