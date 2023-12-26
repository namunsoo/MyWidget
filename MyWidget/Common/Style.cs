using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace MyWidget.Common
{
	public static class Style
	{
		/// <summary>
		/// 문자열 색상 코드 변환 <br/>
		/// string "#FFFFFFFF" => Windows.UI.Color
		/// </summary>
		/// <param name="hex">색상 코드 [ex: "#FFFFFFFF"]</param>
		/// <returns></returns>
		public static Color GetColor(string hex)
		{
			hex = hex.Replace("#", string.Empty);
			byte a = (byte)(Convert.ToUInt32(hex.Substring(0, 2), 16));
			byte r = (byte)(Convert.ToUInt32(hex.Substring(2, 2), 16));
			byte g = (byte)(Convert.ToUInt32(hex.Substring(4, 2), 16));
			byte b = (byte)(Convert.ToUInt32(hex.Substring(6, 2), 16));
			return Color.FromArgb(a, r, g, b);
		}

		/// <summary>
		/// 색상 어둡게 변환
		/// </summary>
		/// <param name="hex">색상</param>
		/// <param name="brightness">밝기 [ex: 0.1f (10%어둡게)]</param>
		/// <returns></returns>
		public static Color GetColorDarkly(Color color, float brightness)
		{
			brightness = 1 - brightness;
			float r = (float)color.R * brightness;
			float g = (float)color.G * brightness;
			float b = (float)color.B * brightness;
			return Color.FromArgb(color.A, (byte)r, (byte)g, (byte)b);
		}

	}
}
