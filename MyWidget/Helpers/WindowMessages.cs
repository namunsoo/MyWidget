using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWidget.Helpers
{
	public class BringTopMessage{ }

	public class MemoMessage {
		public Guid MemoId { get; set; }
		public bool IsCreate { get; set; }
		public bool IsUpdate { get; set; }
		public bool IsDelete { get; set; }
		public string RtfText { get; set; }
		public string BackgroundColor { get; set; }
		public MemoMessage(Guid value, bool isCreate, bool isUpdate, bool isDelete, string rtfText, string backgroundColor)
		{
			MemoId = value;
			IsCreate = isCreate;
			IsUpdate = isUpdate;
			IsDelete = isDelete;
			RtfText = rtfText;
			BackgroundColor = backgroundColor;
		}
	}
}
