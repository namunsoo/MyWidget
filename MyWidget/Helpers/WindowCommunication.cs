using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWidget.Helpers
{
	public class WindowCommunication : ValueChangedMessage<ElementTheme>
	{
		public WindowCommunication(ElementTheme value) : base(value)
		{
			
		}
	}
}
