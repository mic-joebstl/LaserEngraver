using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace LaserPathEngraver.UI.Win.Controls
{
	public class ContentChangeLabel : Label
	{
		// Register a custom routed event using the Bubble routing strategy.
		public static readonly RoutedEvent ContentChangedEvent = EventManager.RegisterRoutedEvent(
			name: "ContentChanged",
			routingStrategy: RoutingStrategy.Bubble,
			handlerType: typeof(RoutedEventHandler),
			ownerType: typeof(ContentChangeLabel));

		public ContentChangeLabel()
		{
			var dpd = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(Label.ContentProperty, typeof(Label));
			if (dpd != null)
			{
				dpd.AddValueChanged(this, (object? sender, EventArgs e) =>
				{
					if (Content != null)
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
