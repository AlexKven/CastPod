using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncPod.Services
{
    public class SettingsManager
    {
        private const string SettingsFileName = "Settings.json";
        private Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        public T GetValue<T>(string name, T defaultValue)
        {
            if (Settings.TryGetValue(name, out var result))
            {
                if (result is T typed)
                    return typed;
                else
                    return defaultValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public void SetValue<T>(string name, T value)
        {
            Settings[name] = value;
        }

        public async Task LoadSettings()
        {
            var currentPath = System.AppDomain.CurrentDomain.BaseDirectory;
            var settingsPath = Path.Combine(currentPath, SettingsFileName);
            if (File.Exists(settingsPath))
            {
                using (var stream = File.OpenRead(settingsPath))
                using (var reader = new StreamReader(stream))
                {
                    var json = await reader.ReadToEndAsync();
                    Settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                }
            }

        }

        public async Task SaveSettings()
        {
            var currentPath = System.AppDomain.CurrentDomain.BaseDirectory;
            var settingsPath = Path.Combine(currentPath, SettingsFileName);

            using (var writer = new StreamWriter(settingsPath))
            {
                var json = JsonConvert.SerializeObject(Settings);
                await writer.WriteAsync(json);
            }
        }
    }
}
