using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaserEngraver.Core.Configurations
{
	public interface IWritableOptions<out T> : IOptions<T> where T : class, new()
	{
		void Update(Action<T> applyChanges);
	}
	public class WritableJsonOptions<T> : IWritableOptions<T> where T : class, new()
	{
		private readonly string _basePath;
		private readonly IOptionsMonitor<T> _options;
		private readonly IConfigurationRoot _configuration;
		private readonly string _section;
		private readonly string _file;

		public WritableJsonOptions(
			string basePath,
			IOptionsMonitor<T> options,
			IConfigurationRoot configuration,
			string section,
			string file)
		{
			_basePath = basePath;
			_options = options;
			_configuration = configuration;
			_section = section;
			_file = file;
		}

		public T Value => _options.CurrentValue;
		public T Get(string name) => _options.Get(name);

		public void Update(Action<T> applyChanges)
		{
			var filePath = Path.Combine(_basePath, _file);
			var fileText = File.Exists(filePath) ? File.ReadAllText(filePath) : "{}";

			var jObject = JsonConvert.DeserializeObject<JObject>(fileText);
			var sectionObject = jObject.TryGetValue(_section, out JToken? section) ?
				JsonConvert.DeserializeObject<T>(section.ToString()) ?? Value : Value;

			applyChanges(sectionObject);

			jObject[_section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));

			File.WriteAllText(filePath, JsonConvert.SerializeObject(jObject, Formatting.Indented));
			_configuration.Reload();
		}
	}

	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection ConfigureWritableJson<T>(
			this IServiceCollection services,
			IConfigurationSection section,
			string basePath,
			string file = "appsettings.json") where T : class, new()
		{
			return services
				.Configure<T>(section)
				.AddTransient<IWritableOptions<T>>(provider =>
				{
					var configuration = (IConfigurationRoot)provider.GetRequiredService<IConfiguration>();
					var options = provider.GetRequiredService<IOptionsMonitor<T>>();
					return new WritableJsonOptions<T>(basePath, options, configuration, section.Key, file);
				});
		}
	}
}