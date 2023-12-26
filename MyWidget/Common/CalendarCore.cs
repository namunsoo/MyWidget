using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MyWidget.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyWidget.Common
{
	public class CalendarCore
	{
		private static string _applicationName;
		private static string _myEmail;
		CalendarCore()
		{
			FileInfo fi = new FileInfo("C:\\MyWidget\\apiData.txt");
			if (fi.Exists)
			{
				using (StreamReader reader = new StreamReader("C:\\MyWidget\\apiData.txt"))
				{
					string[] data = reader.ReadToEnd().Split(',');
					_applicationName = data[0];
					_myEmail = data[1];
				}
			}
		}
		/// <summary>
		/// 달력 앞뒤 일부 포함해서 가져오기
		/// </summary>
		/// <param name="today">오늘 날짜</param>
		/// <returns></returns>
		public static List<CalendarDay> GetMonth(DateTime today)
		{
			DateTime beforMonth = today.AddMonths(-1); // 아직 날짜는 변경 전
			DateTime nextMonth = today.AddMonths(1); // 아직 날짜는 변경 전
			int beforeMonthDays = DaysByMonth(beforMonth.Year)[beforMonth.Month - 1];
			int thisMonthDays = DaysByMonth(today.Year)[today.Month - 1];
			int nextMonthDays = DaysByMonth(nextMonth.Year)[nextMonth.Month - 1];
			int startDay = CalendarCore.FristWeekStart(DateTime.Today);
			int remainCount = 42 - (startDay + thisMonthDays);
			CalendarDay calendarDay = new CalendarDay();
			List<CalendarDay> listDay = new List<CalendarDay>();
			beforMonth = new DateTime(beforMonth.Year, beforMonth.Month, beforeMonthDays - startDay + 1);
			nextMonth = new DateTime(nextMonth.Year, nextMonth.Month, remainCount);
			int i = 0;

			List<CalendarTimetableItem> googleCalendar = GetGoogleCalendarData(beforMonth, nextMonth);
			//List<CalendarTimetableItem> googleCalendar = new List<CalendarTimetableItem>();

			FileInfo fi = null;
			string filePath = string.Empty;
			string localMemo = string.Empty;
			#region [| 이전달(일부) |]
			for (i = startDay; i > 0; i--)
			{
				calendarDay = new CalendarDay();
				calendarDay.Year = beforMonth.Year;
				calendarDay.Month = beforMonth.Month;
				calendarDay.Day = beforeMonthDays - i + 1;
				calendarDay.Timetable = googleCalendar.FindAll(item => item.Month.Equals(calendarDay.Month) && item.Day.Equals(calendarDay.Day));
				if (calendarDay.Timetable != null && calendarDay.Timetable.Count != 0)
				{
					foreach (CalendarTimetableItem calendarTimetableItem in calendarDay.Timetable)
					{
						calendarDay.Memo += calendarTimetableItem.Summary + "\r";
					}
				}
				try
				{
					filePath = "C:\\MyWidget\\CalendarMemo\\" + calendarDay.Year + "\\" + calendarDay.Month + "\\" + calendarDay.Day + ".txt";
					fi = new FileInfo(filePath);
					if (fi.Exists)
					{
						using (StreamReader reader = new StreamReader(filePath))
						{
							localMemo = reader.ReadToEnd();
							calendarDay.Memo += localMemo;
							calendarDay.LocalMemo += localMemo;
						}
					}
				}
				catch { }
				listDay.Add(calendarDay);
			}
			#endregion

			#region [| 이번달 |]
			for (i = 1; i <= thisMonthDays; i++)
			{
				calendarDay = new CalendarDay();
				calendarDay.Year = today.Year;
				calendarDay.Month = today.Month;
				calendarDay.Day = i;
				calendarDay.Timetable = googleCalendar.FindAll(item => item.Month.Equals(calendarDay.Month) && item.Day.Equals(calendarDay.Day));
				if (calendarDay.Timetable != null && calendarDay.Timetable.Count != 0)
				{
					foreach (CalendarTimetableItem calendarTimetableItem in calendarDay.Timetable)
					{
						calendarDay.Memo += calendarTimetableItem.Summary + "\r";
					}
				}
				try
				{
					filePath = "C:\\MyWidget\\CalendarMemo\\" + calendarDay.Year + "\\" + calendarDay.Month + "\\" + calendarDay.Day + ".txt";
					fi = new FileInfo(filePath);
					if (fi.Exists)
					{
						using (StreamReader reader = new StreamReader(filePath))
						{
							localMemo = reader.ReadToEnd();
							calendarDay.Memo += localMemo;
							calendarDay.LocalMemo += localMemo;
						}
					}
				}
				catch { }
				listDay.Add(calendarDay);
			}
			#endregion

			#region [| 다음달(일부) |]
			for (i = 1; i <= remainCount; i++)
			{
				calendarDay = new CalendarDay();
				calendarDay.Year = nextMonth.Year;
				calendarDay.Month = nextMonth.Month;
				calendarDay.Day = i;
				calendarDay.Timetable = googleCalendar.FindAll(item => item.Month.Equals(calendarDay.Month) && item.Day.Equals(calendarDay.Day));
				if (calendarDay.Timetable != null && calendarDay.Timetable.Count != 0)
				{
					foreach (CalendarTimetableItem calendarTimetableItem in calendarDay.Timetable)
					{
						calendarDay.Memo += calendarTimetableItem.Summary + "\r";
					}
				}
				try
				{
					filePath = "C:\\MyWidget\\CalendarMemo\\" + calendarDay.Year + "\\" + calendarDay.Month + "\\" + calendarDay.Day + ".txt";
					fi = new FileInfo(filePath);
					if (fi.Exists)
					{
						using (StreamReader reader = new StreamReader(filePath))
						{
							localMemo = reader.ReadToEnd();
							calendarDay.Memo += localMemo;
							calendarDay.LocalMemo += localMemo;
						}
					}
				}
				catch { }
				listDay.Add(calendarDay);
			}
			#endregion
			return listDay;
		}

		/// <summary>
		/// 이번달 1일이 시작 요일 (일요일 = 0)
		/// </summary>
		/// <param name="today">오늘 날짜</param>
		/// <returns></returns>
		public static int FristWeekStart(DateTime today)
		{
			int[] days = DaysByMonth(today.Year);
			int countDay = 0;
			for (int i = 0; i < today.Month - 1; i++)
			{
				countDay += days[i];
			}
			return (today.Year + (today.Year / 4 - today.Year / 100 + today.Year / 400) + countDay) % 7;
		}

		/// <summary>
		/// 달별 총 날짜 객수
		/// </summary>
		/// <param name="year">년도</param>
		/// <returns></returns>
		public static int[] DaysByMonth(int year)
		{
			int[] days = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
			if ((year % 4 == 0) && ((year % 100 != 0) || (year % 400 == 0)))
			{
				days[1] = 29;
			}
			return days;
		}

		/// <summary>
		/// 구글 캘린더 api 연결
		/// </summary>
		/// <returns></returns>
		public static CalendarService GetGoogleApiService()
		{
			string[] Scopes = { CalendarService.Scope.Calendar };
			string ApplicationName = _applicationName;
			UserCredential credential;

			using (var stream =
				new FileStream("C:\\MyWidget\\client_secret.json", FileMode.Open, FileAccess.Read))
			{
				string credPath = "C:\\MyWidget\\token";
				credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
					GoogleClientSecrets.Load(stream).Secrets,
					Scopes,
					"user",
					CancellationToken.None,
					new FileDataStore(credPath, true)).Result;
			}

			// Create Google Calendar API service.
			var service = new CalendarService(new BaseClientService.Initializer()
			{
				HttpClientInitializer = credential,
				ApplicationName = ApplicationName,
			});

			return service;
		}


		/// <summary>
		/// 구글 일정 가져오기
		/// </summary>
		/// <param name="startDate">검색 시작 날짜</param>
		/// <param name="endDate">검색 마직막 날짜</param>
		/// <returns></returns>
		public static List<CalendarTimetableItem> GetGoogleCalendarData(DateTime startDate, DateTime endDate)
		{
			var service = GetGoogleApiService();

			// Define the time range in the request
			EventsResource.ListRequest request = service.Events.List("primary");
			request.TimeMin = startDate;
			request.TimeMax = endDate;
			request.ShowDeleted = false;
			request.SingleEvents = true;
			request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

			// Example: List the user's upcoming events
			List<CalendarTimetableItem> myItems = new List<CalendarTimetableItem>();
			CalendarTimetableItem item = null;
			Events events = request.Execute();
			if (events.Items != null && events.Items.Count > 0)
			{
				foreach (Event eventItem in events.Items)
				{
					if (eventItem.Start.DateTime != null && eventItem.End.DateTime != null)
					{
						item = new CalendarTimetableItem();
						item.Id = eventItem.Id;
						item.Year = eventItem.Start.DateTime.Value.Year;
						item.Month = eventItem.Start.DateTime.Value.Month;
						item.Day = eventItem.Start.DateTime.Value.Day;
						item.StartTime = eventItem.Start.DateTime;
						item.EndTime = eventItem.End.DateTime;
						item.Description = eventItem.Description;
						item.Summary = eventItem.Summary;
						myItems.Add(item);
					}
				}
			}
			return myItems;
		}

		/// <summary>
		/// 구글 일정 새성
		/// </summary>
		/// <param name="summary">제목</param>
		/// <param name="description">설명</param>
		/// <param name="start">시작 시간</param>
		/// <param name="end">끝나는 시간</param>
		public static void CreateGoogleCalendarData(string summary, string description, DateTime start, DateTime end)
		{
			var service = GetGoogleApiService();

			// Create a new event
			Event newEvent = new Event()
			{
				Summary = summary,
				Description = description,
				Start = new EventDateTime()
				{
					DateTime = start,
					TimeZone = "Asia/Seoul"
				},
				End = new EventDateTime()
				{
					DateTime = end,
					TimeZone = "Asia/Seoul"
				},
				Attendees = new List<EventAttendee>()
				{
					new EventAttendee() { Email = _myEmail }
				}
			};

			// Insert the new event into the calendar
			EventsResource.InsertRequest request = service.Events.Insert(newEvent, "primary");
			Event createdEvent = request.Execute();
		}

		/// <summary>
		/// 구글 일정 수정
		/// </summary>
		/// <param name="id">일정 아이디</param>
		/// <param name="summary">제목</param>
		/// <param name="description">설명</param>
		/// <param name="start">시작 시간</param>
		/// <param name="end">끝나는 시간</param>
		public static void UpdateGoogleCalendarData(string id, string summary, string description, DateTime start, DateTime end)
		{
			var service = GetGoogleApiService();

			// Fetch the existing event
			EventsResource.GetRequest getRequest = service.Events.Get("primary", id);
			Event existingEvent = getRequest.Execute();

			// Update the event details
			existingEvent.Summary = summary;
			existingEvent.Description = description;
			existingEvent.Start.DateTime = start;
			existingEvent.End.DateTime = end;

			// Update the event in the calendar
			EventsResource.UpdateRequest updateRequest = service.Events.Update(existingEvent, "primary", id);
			Event updatedEvent = updateRequest.Execute();
		}

		/// <summary>
		/// 구글 일정 삭제
		/// </summary>
		/// <param name="id">일정 아이디</param>
		public static void DeleteGoogleCalendarData(string id)
		{
			var service = GetGoogleApiService();

			// Delete the event
			EventsResource.DeleteRequest deleteRequest = service.Events.Delete("primary", id);
			deleteRequest.Execute();
		}
	}
}
