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
			var mainInstance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey("MyWidget");

			if (!mainInstance.IsCurrent)
			{
				// Redirect the activation (and args) to the "main" instance, and exit.
				var activatedEventArgs =
					Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
				await mainInstance.RedirectActivationToAsync(activatedEventArgs);
				System.Diagnostics.Process.GetCurrentProcess().Kill();
				return;
			}

			Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Activated += AppActivated;

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
				p.SetBorderAndTitleBar(false, false);
				p.IsResizable = false;
			}
		}

		private void AppActivated(object sender, AppActivationArguments e)
		{
			IntPtr hWnd = new IntPtr();
			if (m_window != null)
			{
				hWnd = WindowNative.GetWindowHandle(m_window);
				Win32.ShowWindow(hWnd, (int)Win32.ShowWindowCommands.ShowNormal);
				Win32.SetForegroundWindow(hWnd);
			}
			if (calendar_window != null)
			{
				hWnd = WindowNative.GetWindowHandle(calendar_window);
				Win32.ShowWindow(hWnd, (int)Win32.ShowWindowCommands.ShowNormal);
				Win32.SetForegroundWindow(hWnd);
			}
		}

		public static Window m_window { get; set; }
		public static Window calendar_window { get; set; }
		public static List<Window> memo_window = new List<Window>();
	}
}
