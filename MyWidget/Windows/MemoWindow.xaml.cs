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
using Microsoft.UI.Text;
using Windows.System;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage;
using Microsoft.UI.Xaml.Shapes;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyWidget.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MemoWindow : Window
    {
		public Guid id { get; set; }
		private AppWindow _appWindow;
		private OverlappedPresenter _appWindow_overlapped;
		private DisplayArea _displayArea;
		private Point _mouseDownLocation;
		private Grid _grid;
		public MemoWindow(Guid memoId)
		{
			if (!memoId.Equals(default(Guid)))
			{
				id = memoId;
			}
			this.InitializeComponent();

			var messenger = Ioc.Default.GetService<IMessenger>();

			messenger.Register<BringTop>(this, (r, m) =>
			{
				_appWindow.MoveInZOrderAtTop();
				this.SetForegroundWindow();
			});

			using (var manager = WinUIEx.WindowManager.Get(this))
			{
				manager.Width = 300;
				manager.Height = 300;
			}

			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			_appWindow = AppWindow.GetFromWindowId(wndId);
			_appWindow_overlapped = (OverlappedPresenter)_appWindow.Presenter;
			if (_appWindow is not null)
			{
				_displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Nearest);
			}

			GetMyOptions();
		}
		#region [| 기본 설정 |]
		private async void GetMyOptions()
		{
			bool isFileExist = false;
			int x = 0;
			int y = 0;
			if (!id.Equals(default(Guid)))
			{
				string path = "C:\\MyWidget\\PostItMemo";
				DirectoryInfo di = new DirectoryInfo(path);
				if (di.Exists)
				{
					FileInfo[] fileInfos = di.GetFiles("*" + id + ".rtf");
					if (fileInfos.Length > 0)
					{
						string[] data = fileInfos[0].Name.Split(',');
						Int32.TryParse(data[0], out x);
						Int32.TryParse(data[1], out y);
						Grid_Content.Background = new SolidColorBrush(Common.Style.GetColor(data[2]));
						Grid_TitleBar.Background = new SolidColorBrush(Common.Style.GetColor(data[3]));

						StorageFile file = await StorageFile.GetFileFromPathAsync(fileInfos[0].FullName);
						using (IRandomAccessStream randAccStream = await file.OpenAsync(FileAccessMode.Read))
						{
							// Load the file into the Document property of the RichEditBox.
							REB_Memo.Document.LoadFromStream(Microsoft.UI.Text.TextSetOptions.FormatRtf, randAccStream);
						}
						isFileExist = true;
					}
				}
			} else
			{
				if (Grid_Content.Background is SolidColorBrush sb)
				{
					Grid_TitleBar.Background = new SolidColorBrush(Common.Style.GetColorDarkly(sb.Color, 0.1f));
				}
			}

			if (_displayArea is not null)
			{
				var position = _appWindow.Position;
				position.X = isFileExist ? x : ((_displayArea.WorkArea.Width - _appWindow.Size.Width) / 2);
				position.Y = isFileExist ? y : ((_displayArea.WorkArea.Height - _appWindow.Size.Height) / 2);
				_appWindow.Move(position);
			}
		}
		#endregion

		#region [| 윈도우 이동 + 이동 후 파일 이름 변경 |]
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

					RenameAfterMove();
				}
			}
		}

		private async void RenameAfterMove()
		{
			try
			{
				if (!id.Equals(default(Guid)))
				{
					string path = "C:\\MyWidget";
					string folderName = "PostItMemo";
					StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(path);
					StorageFolder postItMemoFolder = await localFolder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);

					DirectoryInfo di = new DirectoryInfo(path + "\\" + folderName);
					FileInfo[] fileInfos = di.GetFiles("*" + id + ".rtf");
					string fileName = fileInfos[0].Name;

					StorageFile file = await postItMemoFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
					if (file != null)
					{
						// 변경을 완료하고 CompleteUpdateSync를 호출할 때까지
						// 파일의 원격 버전에 대한 업데이트를 방지합니다.
						CachedFileManager.DeferUpdates(file);

						fileName = _appWindow.Position.X + "," + _appWindow.Position.Y + "," + ((SolidColorBrush)Grid_Content.Background).Color.ToString() + 
							"," + ((SolidColorBrush)Grid_TitleBar.Background).Color.ToString() + "," + Convert.ToString(id) + ".rtf";
						await file.RenameAsync(fileName);

						// 다른 앱이 파일의 원격 버전을 업데이트할 수 있도록
						// 파일 변경이 완료되었음을 Windows(윈도우)에 알려줍니다.
						FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
					}
				}

			}
			catch { }
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
			App.memo_window.Remove(this);
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

		private void Grid_Fotter_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			_grid = sender as Grid;
			_grid.Background = new SolidColorBrush(Common.Style.GetColorDarkly(((SolidColorBrush)Grid_TitleBar.Background).Color, 0.05f));
		}

		private void Grid_Fotter_PointerExited(object sender, PointerRoutedEventArgs e)
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
					AniCloseColorOption.Begin();
				}
			}
		}
		#endregion

		#region [| MainWindow 열기 |]
		private void Grid_OpenMainWindow_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (App.m_window == null || App.m_window.AppWindow == null)
			{
				App.m_window = new MainWindow();
				App.m_window.ExtendsContentIntoTitleBar = true;
				App.m_window.Activate();

				// the bug test code follows
				var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);

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
				Common.WidgetOptions.SetMainWindow(true);
			}
			else if (App.m_window.AppWindow != null)
			{
				IntPtr hWnd = WindowNative.GetWindowHandle(App.m_window);
				Win32.ShowWindow(hWnd, (int)Win32.ShowWindowCommands.ShowNormal);
				Win32.SetForegroundWindow(hWnd);
			}
		}
		#endregion

		#region [| 새로운 메모 생성 |]
		private void Grid_OpenNewMemo_Tapped(object sender, TappedRoutedEventArgs e)
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
			var appWindow = AppWindow.GetFromWindowId(windowId);

			if (appWindow.Presenter is OverlappedPresenter p)
			{
				p.SetBorderAndTitleBar(false, false);
				p.IsResizable = false;
			}
		}
		#endregion

		#region [| 메모 색상 변경 열기 |]
		private void Grid_ColorChange_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (Grid_ColorOption.Visibility == Visibility.Collapsed)
			{
				Grid_ColorOption.Visibility = Visibility.Visible;
				AniOpenColorOption.Begin();
			} else
			{
				AniCloseColorOption.Begin();
				AniCloseColorOption.Completed += (_, _) => { Grid_ColorOption.Visibility = Visibility.Collapsed; };
			}
		}
		#endregion

		#region [| 메모 색상 변경 닫기 |]
		private void Grid_ColorOption_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (e.OriginalSource == sender)
			{
				AniCloseColorOption.Begin();
				AniCloseColorOption.Completed += (_, _) => { Grid_ColorOption.Visibility = Visibility.Collapsed; };
			}
		}
		#endregion

		#region [| 메모 색상 변경 |]
		private async void Grid_Color_Tapped(object sender, TappedRoutedEventArgs e)
		{
			_grid = sender as Grid;
			string color = Convert.ToString(_grid.Tag);
			if (color.Equals("#FFFFFF") || color.Equals("#e0e0e0"))
			{
				Grid_Content.Background = new SolidColorBrush(Common.Style.GetColorBrightly(Common.Style.GetColor(Convert.ToString(color)), 0.4f));
				Grid_TitleBar.Background = new SolidColorBrush(Common.Style.GetColorDarkly(Common.Style.GetColor(Convert.ToString(color)), 0.1f));
			} else
			{
				Grid_Content.Background = new SolidColorBrush(Common.Style.GetColorBrightly(Common.Style.GetColor(Convert.ToString(color)), 0.6f));
				Grid_TitleBar.Background = new SolidColorBrush(Common.Style.GetColorBrightly(Common.Style.GetColor(Convert.ToString(color)), 0.4f));
			}
			AniCloseColorOption.Begin();
			AniCloseColorOption.Completed += (_, _) => { Grid_ColorOption.Visibility = Visibility.Collapsed; };

			if (!id.Equals(default(Guid)))
			{
				string path = "C:\\MyWidget";
				string folderName = "PostItMemo";
				StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(path);
				StorageFolder postItMemoFolder = await localFolder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);

				DirectoryInfo di = new DirectoryInfo(path + "\\" + folderName);
				FileInfo[] fileInfos = di.GetFiles("*" + id + ".rtf");
				string fileName = fileInfos[0].Name;

				StorageFile file = await postItMemoFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
				if (file != null)
				{
					// 변경을 완료하고 CompleteUpdateSync를 호출할 때까지
					// 파일의 원격 버전에 대한 업데이트를 방지합니다.
					CachedFileManager.DeferUpdates(file);

					fileName = _appWindow.Position.X + "," + _appWindow.Position.Y + "," + ((SolidColorBrush)Grid_Content.Background).Color.ToString() +
						"," + ((SolidColorBrush)Grid_TitleBar.Background).Color.ToString() + "," + Convert.ToString(id) + ".rtf";
					await file.RenameAsync(fileName);

					// 다른 앱이 파일의 원격 버전을 업데이트할 수 있도록
					// 파일 변경이 완료되었음을 Windows(윈도우)에 알려줍니다.
					FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
				}
			}
		}
		#endregion

		#region [| 메모 글자 설정 변경 |]
		private void Grid_Bold_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (Convert.ToString(Grid_Bold.Tag).Equals("#00000000")) 
			{
				Grid_Bold.Background = new SolidColorBrush(((SolidColorBrush)Grid_TitleBar.Background).Color);
				Grid_Bold.Tag = Convert.ToString(((SolidColorBrush)Grid_TitleBar.Background).Color);
			}
			else
			{
				Grid_Bold.Background = new SolidColorBrush(Common.Style.GetColor("#00000000"));
				Grid_Bold.Tag = "#00000000";
			}
			REB_Memo.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;
			REB_Memo.Focus(FocusState.Programmatic);
		}

		private void Grid_Italic_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (Convert.ToString(Grid_Italic.Tag).Equals("#00000000"))
			{
				Grid_Italic.Background = new SolidColorBrush(((SolidColorBrush)Grid_TitleBar.Background).Color);
				Grid_Italic.Tag = Convert.ToString(((SolidColorBrush)Grid_TitleBar.Background).Color);
			}
			else
			{
				Grid_Italic.Background = new SolidColorBrush(Common.Style.GetColor("#00000000"));
				Grid_Italic.Tag = "#00000000";
			}
			REB_Memo.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
			REB_Memo.Focus(FocusState.Programmatic);
		}

		private void Grid_Underline_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (Convert.ToString(Grid_Underline.Tag).Equals("#00000000"))
			{
				Grid_Underline.Background = new SolidColorBrush(((SolidColorBrush)Grid_TitleBar.Background).Color);
				Grid_Underline.Tag = Convert.ToString(((SolidColorBrush)Grid_TitleBar.Background).Color);
				REB_Memo.Document.Selection.CharacterFormat.Underline = UnderlineType.Single;
			}
			else
			{
				Grid_Underline.Background = new SolidColorBrush(Common.Style.GetColor("#00000000"));
				Grid_Underline.Tag = "#00000000";
				REB_Memo.Document.Selection.CharacterFormat.Underline = UnderlineType.Undefined;
			}
			REB_Memo.Focus(FocusState.Programmatic);
		}

		private void Grid_Strikethrough_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (Convert.ToString(Grid_Strikethrough.Tag).Equals("#00000000"))
			{
				Grid_Strikethrough.Background = new SolidColorBrush(((SolidColorBrush)Grid_TitleBar.Background).Color);
				Grid_Strikethrough.Tag = Convert.ToString(((SolidColorBrush)Grid_TitleBar.Background).Color);
			}
			else
			{
				Grid_Strikethrough.Background = new SolidColorBrush(Common.Style.GetColor("#00000000"));
				Grid_Strikethrough.Tag = "#00000000";
			}
			REB_Memo.Document.Selection.CharacterFormat.Strikethrough = FormatEffect.Toggle;
			REB_Memo.Focus(FocusState.Programmatic);
		}

		private void Grid_BulletedList_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (Convert.ToString(Grid_BulletedList.Tag).Equals("#00000000"))
			{
				Grid_BulletedList.Background = new SolidColorBrush(((SolidColorBrush)Grid_TitleBar.Background).Color);
				Grid_BulletedList.Tag = Convert.ToString(((SolidColorBrush)Grid_TitleBar.Background).Color);
				REB_Memo.Document.Selection.ParagraphFormat.ListType = MarkerType.Bullet;
			}
			else
			{
				Grid_BulletedList.Background = new SolidColorBrush(Common.Style.GetColor("#00000000"));
				Grid_BulletedList.Tag = "#00000000";
				REB_Memo.Document.Selection.ParagraphFormat.ListType = MarkerType.None;
			}
			REB_Memo.Focus(FocusState.Programmatic);
		}

		private void Grid_FontSize_Tapped(object sender, TappedRoutedEventArgs e)
		{
			FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
		}

		private void NBox_FontSize_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
		{
			if (!double.IsNaN(sender.Value))
			{
				//REB_Memo.Document.Selection.CharacterFormat.Size = (float)sender.Value;
				REB_Memo.FontSize = sender.Value;
			}
		}
		#endregion

		#region [| 메모 삭제 |]
		private async void Grid_DeleteMemo_Tapped(object sender, TappedRoutedEventArgs e)
		{
			// dialog 생성
			ContentDialog dialog = new ContentDialog
			{
				Title = "알림",
				Content = "삭제하시겠습니까?",
				PrimaryButtonText = "확인",
				CloseButtonText = "취소"
			};

			dialog.XamlRoot = this.SP_Dialog.XamlRoot;

			ContentDialogResult result = await dialog.ShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				if (!id.Equals(default(Guid)))
				{
					string path = "C:\\MyWidget";
					string folderName = "PostItMemo";
					StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(path);
					StorageFolder postItMemoFolder = await localFolder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);

					DirectoryInfo di = new DirectoryInfo(path + "\\" + folderName);
					FileInfo[] fileInfos = di.GetFiles("*" + id + ".rtf");
					string fileName = fileInfos[0].Name;

					StorageFile file = await postItMemoFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
					if (file != null)
					{
						await file.DeleteAsync();
					}
				}
				App.memo_window.Remove(this);
				this.Close();
			}
		}
		#endregion

		#region [| 키보드 입력 후 메모 저장 로직 |]
		private bool isTypingFirst = true;
		private DispatcherTimer keyUpTimer;
		private void REB_Memo_KeyUp(object sender, KeyRoutedEventArgs e)
		{
			if (isTypingFirst)
			{
				keyUpTimer = new DispatcherTimer();
				keyUpTimer.Interval = TimeSpan.FromSeconds(3);
				keyUpTimer.Tick += SaveAfterTypingStop;
				isTypingFirst = false;
			}
			else
			{
				keyUpTimer.Stop();
				keyUpTimer.Start();
			}
		}

		private async void SaveAfterTypingStop(object sender, object e)
		{
			keyUpTimer.Stop();
			try
			{
				string path = "C:\\MyWidget";
				string folderName = "PostItMemo";
				StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(path);
				StorageFolder postItMemoFolder = await localFolder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);

				string fileName = string.Empty;
				DirectoryInfo di = new DirectoryInfo(path + "\\"+ folderName);
				FileInfo[] fileInfos = null;
				if (id.Equals(default(Guid)))
				{
					do
					{
						id = Guid.NewGuid();
						fileInfos = di.GetFiles("*" + id + ".rtf");
					} while (fileInfos.Length != 0);
					fileName = _appWindow.Position.X + "," + _appWindow.Position.Y + "," + Convert.ToString(id) + ".rtf";
				} else
				{
					fileInfos = di.GetFiles("*" + id + ".rtf");
					fileName = fileInfos[0].Name;
				}

				StorageFile file = await postItMemoFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
				if (file != null)
				{
					// 변경을 완료하고 CompleteUpdateSync를 호출할 때까지
					// 파일의 원격 버전에 대한 업데이트를 방지합니다.
					CachedFileManager.DeferUpdates(file);

					fileName = _appWindow.Position.X + "," + _appWindow.Position.Y + "," + ((SolidColorBrush)Grid_Content.Background).Color.ToString()
						+ "," + ((SolidColorBrush)Grid_TitleBar.Background).Color.ToString() + "," + Convert.ToString(id) + ".rtf";
					await file.RenameAsync(fileName);

					// write to file
					using (IRandomAccessStream randAccStream =
						await file.OpenAsync(FileAccessMode.ReadWrite))
					{
						REB_Memo.Document.SaveToStream(Microsoft.UI.Text.TextGetOptions.FormatRtf, randAccStream);
					}

					// 다른 앱이 파일의 원격 버전을 업데이트할 수 있도록
					// 파일 변경이 완료되었음을 Windows(윈도우)에 알려줍니다.
					FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
				}
			}
			catch { }
			finally
			{
				isTypingFirst = true;
			}

		}
		#endregion

	}
}
