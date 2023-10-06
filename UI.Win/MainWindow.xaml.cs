using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LaserPathEngraver.UI.Win
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private MainWindowViewModel _ViewModel;

		public MainWindow()
		{
			System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("de");
			InitializeComponent();
			_ViewModel = new MainWindowViewModel();
			DataContext = _ViewModel;
			SizeChanged += (o, e) => { RaisePropertyChanged(nameof(TutorialViewBox)); };
			_TutorialBox.SizeChanged += (o, e) => { RaisePropertyChanged(nameof(TutorialViewBox)); };
		}

		public Rect TutorialViewBox
		{
			get
			{
				Point relativePoint = _TutorialBox.TransformToAncestor(this).Transform(new Point(-5, -5));
				return new Rect(relativePoint, new Size(_TutorialBox.ActualWidth, _TutorialBox.ActualHeight));
			}
		}


		private void ContentControl_KeyDown(object sender, KeyEventArgs e)
		{
			_ViewModel.OnSpaceKeyDown(sender, e);
		}

		private void ContentControl_KeyUp(object sender, KeyEventArgs e)
		{
			_ViewModel.OnSpaceKeyUp(sender, e);
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		private void RaisePropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
