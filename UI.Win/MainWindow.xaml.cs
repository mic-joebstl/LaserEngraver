﻿using CommunityToolkit.Mvvm.Input;
using LaserPathEngraver.Core.Configurations;
using LaserPathEngraver.Core.Devices;
using Microsoft.Extensions.Options;
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
		private MainWindowViewModel _viewModel;

		public MainWindow(IWritableOptions<UserConfiguration> userConfiguration, Space space, DeviceDispatcherService deviceDispatcher)
		{
			System.Threading.Thread.CurrentThread.CurrentUICulture = userConfiguration.Value.Culture;
			DataContext = _viewModel = new MainWindowViewModel(userConfiguration, space, deviceDispatcher);
			InitializeComponent();
			SizeChanged += (o, e) =>
			{
				RaisePropertyChanged(nameof(TutorialViewBox));
				RaisePropertyChanged(nameof(ActionsViewBox));
			};
			_TutorialBox.SizeChanged += (o, e) =>
			{
				RaisePropertyChanged(nameof(TutorialViewBox));
			};
			_ActionsViewBox.SizeChanged += (o, e) =>
			{
				RaisePropertyChanged(nameof(ActionsViewBox));
			};
		}

		public Rect TutorialViewBox
		{
			get
			{
				Point relativePoint = _TutorialBox.TransformToAncestor(this).Transform(new Point(-5, -5));
				return new Rect(relativePoint, new Size(_TutorialBox.ActualWidth, _TutorialBox.ActualHeight));
			}
		}

		public Rect ActionsViewBox
		{
			get
			{
				Point relativePoint = _ActionsViewBox.TransformToAncestor(this).Transform(new Point(-5, -5));
				return new Rect(relativePoint, new Size(_ActionsViewBox.ActualWidth, _ActionsViewBox.ActualHeight));
			}
		}

		private void ContentControl_KeyDown(object sender, KeyEventArgs e)
		{
			_viewModel.OnSpaceKeyDown(sender, e);
		}

		private void ContentControl_KeyUp(object sender, KeyEventArgs e)
		{
			_viewModel.OnSpaceKeyUp(sender, e);
		}

		private void ContentControl_DragOver(object sender, DragEventArgs e)
		{
			_viewModel.OnSpaceDragOver(sender, e);
		}

		private void ContentControl_Drop(object sender, DragEventArgs e)
		{
			_viewModel.OnSpaceDrop(sender, e);
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
