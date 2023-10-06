using Newtonsoft.Json.Linq;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml;

namespace LaserPathEngraver.UI.Win.Resources.Localization
{
	public static class TextBlockHelper
	{
		public static readonly DependencyProperty FormattedTextProperty =
			DependencyProperty.RegisterAttached("FormattedText",
			typeof(string),
			typeof(TextBlockHelper),
			new UIPropertyMetadata("", FormattedTextChanged));

		public static string GetFormattedText(DependencyObject obj)
		{
			return (string)obj.GetValue(FormattedTextProperty);
		}

		public static void SetFormattedText(DependencyObject obj, string value)
		{
			obj.SetValue(FormattedTextProperty, value);
		}

		private static void FormattedTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is TextBlock textBlock)
			{
				textBlock.Inlines.Clear();
				textBlock.Inlines.Add(Process(e.NewValue as string));
			}
		}

		static Inline Process(string? value)
		{
			var span = new Span();

			if (value != null)
			{
				try
				{
					var doc = new XmlDocument();
					doc.LoadXml(value);
					var rootNode = doc.ChildNodes.Count > 0 ? doc.ChildNodes[0] : null;
					if (rootNode != null)
					{
						InternalProcess(span, rootNode);
					}
				}
				catch (Exception)
				{
					//TODO: log
					span.Inlines.Add(new Run(value));
				}
			}

			return span;
		}

		private static void InternalProcess(Span span, XmlNode xmlNode)
		{
			foreach (XmlNode child in xmlNode)
			{
				if (child is XmlText)
				{
					span.Inlines.Add(new Run(child.InnerText));
				}
				else if (child is XmlElement)
				{
					Span spanItem = new Span();
					InternalProcess(spanItem, child);
					switch (child.Name.ToUpper())
					{
						case "BR":
						case "P":
							span.Inlines.Add(new LineBreak());
							span.Inlines.Add(spanItem);
							break;
						case "B":
						case "BOLD":
							Bold bold = new Bold(spanItem);
							span.Inlines.Add(bold);
							break;
						case "I":
						case "ITALIC":
							Italic italic = new Italic(spanItem);
							span.Inlines.Add(italic);
							break;
						case "U":
						case "UNDERLINE":
							Underline underline = new Underline(spanItem);
							span.Inlines.Add(underline);
							break;
					}
				}
			}
		}
	}
}