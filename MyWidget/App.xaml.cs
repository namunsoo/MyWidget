using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.AppLifecycle;
using MyWidget.Helpers;
using MyWidget.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.WindowManagement;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MyWidget
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public partial class App : Application
	{
		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// Invoked when the application is launched.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
		{

			#region [| 이미 앱이 실행줄일 경우 프로그램 종료 |]
			var mainInstance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey("MyWidget");

			if (!mainInstance.IsCurrent)
			{
				var activatedEventArgs =
					Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
				await mainInstance.RedirectActivationToAsync(activatedEventArgs);
				System.Diagnostics.Process.GetCurrentProcess().Kill();
				return;
			}
			#endregion

			// 이미 앱이 실행줄일때 같은 앱을 실행할경우 연결할 이벤트
			Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Activated += AppActivated;

			// 연결된 모든 통신 확인
			Ioc.Default.ConfigureServices
			(new ServiceCollection()
				.AddSingleton<IMessenger>(WeakReferenceMessenger.Default)
				.BuildServiceProvider()
			);

			OpenWindows();
		}

		#region [| 윈도우 열기 |]
		private void OpenWindows()
		{
			bool OtherWindow = false;
			DirectoryInfo di = new DirectoryInfo("C:\\MyWidget");
			if (di.Exists == false)
			{
				di.Create();
			}

			string[] data = { "MainWindow_Open", "Calendar_Close" };
			FileInfo fi = new FileInfo("C:\\MyWidget\\MainOptions.txt");
			if (fi.Exists)
			{
				using (StreamReader reader = new StreamReader("C:\\MyWidget\\MainOptions.txt"))
				{
					data = reader.ReadToEnd().Split(',');
				}
			}
			else
			{
				using (StreamWriter writer = new StreamWriter("C:\\MyWidget\\MainOptions.txt"))
				{
					writer.Write("MainWindow_Open,Calendar_Close");
				}
			}

			#region [| PostItMemo 열기 |]
			di = new DirectoryInfo("C:\\MyWidget\\PostItMemo");
			if (di.Exists)
			{
				FileInfo[] files = di.GetFiles("*.rtf").OrderByDescending(f => f.CreationTime).ToArray();
				MemoWindow memoWindow = null;
				string guid = string.Empty;
				foreach (FileInfo file in files)
				{
					try
					{
						guid = file.Name.Split('.')[0].Split(",")[4];
						memoWindow = new MemoWindow(new Guid(guid));
						memo_window.Add(memoWindow);
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
						OtherWindow = true;
					}
					catch { }
				}
			}
			#endregion

			#region [| MainWindow 열기 |]
			if (data[0].Equals("MainWindow_Open") || !OtherWindow)
			{
				m_window = new MainWindow();
				m_window.ExtendsContentIntoTitleBar = true;
				m_window.Activate();

				// the bug test code follows
				var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(m_window);

				// Retrieve the WindowId that corresponds to hWnd.
				Microsoft.UI.WindowId windowId =
					Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

				// Lastly, retrieve the AppWindow for the current (XAML) WinUI 3 window.
				var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

				if (appWindow.Presenter is OverlappedPresenter p)
				{
					p.SetBorderAndTitleBar(true, false);
					p.IsResizable = false;
				}
			}
			#endregion
		}
		#endregion

		#region [| 앱이 실행줄일때 같은 앱을 실행할경우 이벤트 |]
		private void AppActivated(object sender, AppActivationArguments e)
		{
			// 실행중인 모든 윈도우 위로 호출
			Ioc.Default.GetService<IMessenger>().Send(new BringTop());
		}
		#endregion

		public static Window m_window { get; set; }

		public static List<Window> memo_window = new List<Window>();
	}
}
