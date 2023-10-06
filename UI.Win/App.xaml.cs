using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LaserPathEngraver.Core.Configurations;

namespace LaserPathEngraver.UI.Win
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public IServiceProvider ServiceProvider { get; private set; }

		public IConfiguration Configuration { get; private set; }

		public App()
		{
			Configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.Build();
			
			
			ServiceProvider = ConfigureServices(new ServiceCollection())
				.BuildServiceProvider();
		}

		private IServiceCollection ConfigureServices(IServiceCollection services)
		{
			var basePath = Directory.GetCurrentDirectory();
			
			return services
				.AddSingleton(Configuration)
				.ConfigureWritableJson<DeviceConfiguration>(Configuration.GetSection(nameof(DeviceConfiguration)), basePath)
				.ConfigureWritableJson<UserConfiguration>(Configuration.GetSection(nameof(UserConfiguration)), basePath)
				.AddSingleton<MainWindow>()
				.AddSingleton<Space>();
		}

		private void OnStartup(object sender, StartupEventArgs e)
		{
			var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
			mainWindow.Show();
		}
	}
}
