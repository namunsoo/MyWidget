using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWidget.Models
{
	public class CalendarDay
	{
		public int Year { get; set; }
		public int Month { get; set; }
		public int Day { get; set; }
		public string Memo { get; set; }
		public string LocalMemo { get; set; }
		public List<CalendarTimetableItem> Timetable { get; set; }
	}

	public class CalendarTimetableItem
	{
		public string Id { get; set; }
		public int Year { get; set; }
		public int Month { get; set; }
		public int Day { get; set; }
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
		public string Description { get; set; }
		public string Summary { get; set; }
	}
}
