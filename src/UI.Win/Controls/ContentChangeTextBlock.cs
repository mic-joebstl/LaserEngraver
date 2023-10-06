using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace LaserEngraver.UI.Win.Controls
{
	public class ContentChangeTextBlock : TextBlock
	{
		public static readonly RoutedEvent ContentChangedEvent = EventManager.RegisterRoutedEvent(
			name: "ContentChanged",
			routingStrategy: RoutingStrategy.Bubble,
			handlerType: typeof(RoutedEventHandler),
			ownerType: typeof(ContentChangeTextBlock));

		public ContentChangeTextBlock()
		{
			var dpd = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
			if (dpd != null)
			{
				dpd.AddValueChanged(this, (object? sender, EventArgs e) =>
				{
					if (Text != null)
					{
						RaiseEvent(new RoutedEventArgs(ContentChangedEvent));
					}
				});
			}
		}

		public event RoutedEventHandler ContentChanged
		{
			add { AddHandler(ContentChangedEvent, value); }
			remove { RemoveHandler(ContentChangedEvent, value); }
		}
	}
}
