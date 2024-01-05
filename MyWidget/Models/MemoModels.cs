using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWidget.Models
{
	public class Memo
	{
		public Guid Id { get; set; }
		public int X { get; set; }
		public int Y { get; set; }
		public string ContentBackground { get; set; }
		public string TitleBarBackground { get; set; }
		public string Text { get; set; }
	}
}
