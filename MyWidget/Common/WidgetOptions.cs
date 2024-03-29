﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWidget.Common
{
	public static class WidgetOptions
	{
		/// <summary>
		/// MainWindow Open 여부 설정
		/// </summary>
		/// <param name="isOpen"></param>
		public static void SetMainWindow(bool isOpen)
		{
            DirectoryInfo di = new DirectoryInfo("C:\\MyWidget");
            if (di.Exists == false)
            {
                di.Create();
            }

            FileInfo fi = new FileInfo("C:\\MyWidget\\MainOptions.txt");
			if (fi.Exists)
			{
				string data = string.Empty;
				using (StreamReader reader = new StreamReader("C:\\MyWidget\\MainOptions.txt"))
				{
					data = reader.ReadToEnd();
				}
				data = isOpen ? data.Replace("MainWindow_Close", "MainWindow_Open") : data.Replace("MainWindow_Open", "MainWindow_Close");
				using (StreamWriter writer = new StreamWriter("C:\\MyWidget\\MainOptions.txt"))
				{
					writer.Write(data);
				}
			}
			else
			{
				using (StreamWriter writer = new StreamWriter("C:\\MyWidget\\MainOptions.txt"))
				{
					writer.Write("MainWindow_Open,Calendar_Close");
				}
			}
		}
	}
}
