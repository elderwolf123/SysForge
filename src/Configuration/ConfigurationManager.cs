using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RamOptimizer.Configuration
{
    public class ConfigurationManager
    {
        private readonly string _configFilePath;
        private readonly Dictionary<string, object> _configuration;
        private readonly object _lockObject = new object();

        public ConfigurationManager(string configFilePath = "config/appsettings.json")
        {
            _configFilePath = configFilePath;
            _configuration = LoadConfiguration();
        }

        private Dictionary<string, object> LoadConfiguration()
        {
            lock (_lockObject)
            {
                if (File.Exists(_configFilePath))
                {
                    try
                    {
                        var json = File.ReadAllText(_configFilePath);
                        var options = new JsonSerializerOptions
                        {
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true
                        };
                        return JsonSerializer.Deserialize<Dictionary<string, object>>(json, options) ?? new Dictionary<string, object>();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load configuration: {ex.Message}");
                    }
                }
                
                // Return default configuration
                return GetDefaultConfiguration();
            }
        }

        private Dictionary<string, object> GetDefaultConfiguration()
        {
            return new Dictionary<string, object>
            {
                ["Application"] = new Dictionary<string, object>
                {
                    ["Name"] = "RAM Optimizer Pro",
                    ["Version"] = "1.0.0",
                    ["Author"] = "RamOptimizer Team"
                },
                ["Optimization"] = new Dictionary<string, object>
                {
                    ["DefaultProfile"] = "Balanced",
                    ["AutoOptimize"] = false,
                    ["AutoOptimizeInterval"] = 30, // minutes
                    ["AggressionLevel"] = 3
                },
                ["Monitoring"] = new Dictionary<string, object>
                {
                    ["UpdateInterval"] = 1000, // milliseconds
                    ["ShowNotifications"] = true,
                    ["CpuThreshold"] = 80,
                    ["MemoryThreshold"] = 85,
                    ["DiskThreshold"] = 70
                },
                ["Compression"] = new Dictionary<string, object>
                {
                    ["DefaultAlgorithm"] = "LZMA",
                    ["DefaultQuality"] = 100,
                    ["BackgroundCompression"] = true,
                    ["CompressInactiveFiles"] = true
                },
                ["SystemTray"] = new Dictionary<string, object>
                {
                    ["ShowInTray"] = true,
                    ["MinimizeToTray"] = true
                },
                ["Logging"] = new Dictionary<string, object>
                {
                    ["MinimumLevel"] = "Info",
                    ["LogToFile"] = true,
                    ["MaxLogFileSizeMB"] = 10
                }
            };
        }

        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            lock (_lockObject)
            {
                // Handle nested keys (e.g., "Optimization.DefaultProfile")
                var keys = key.Split('.');
                object current = _configuration;

                foreach (var k in keys)
                {
                    if (current is Dictionary<string, object> dict && dict.TryGetValue(k, out var value))
                    {
                        current = value;
                    }
                    else
                    {
                        return defaultValue;
                    }
                }

                // Try to convert the value to the requested type
                return ConvertValue<T>(current, defaultValue);
            }
        }

        private T ConvertValue<T>(object value, T defaultValue)
        {
            if (value is T directValue)
            {
                return directValue;
            }

            try
            {
                if (value == null)
                {
                    return defaultValue;
                }

                // Handle JsonElement (from JSON deserialization)
                if (value is JsonElement jsonElement)
                {
                    var json = jsonElement.GetRawText();
                    return JsonSerializer.Deserialize<T>(json);
                }

                // Handle string to other type conversions
                if (typeof(T) == typeof(bool) && value is string strBool)
                {
                    if (bool.TryParse(strBool, out bool result))
                    {
                        return (T)(object)result;
                    }
                }
                else if (typeof(T) == typeof(int) && value is string strInt)
                {
                    if (int.TryParse(strInt, out int result))
                    {
                        return (T)(object)result;
                    }
                }
                else if (typeof(T) == typeof(double) && value is string strDouble)
                {
                    if (double.TryParse(strDouble, out double result))
                    {
                        return (T)(object)result;
                    }
                }

                // Try using Convert.ChangeType as a last resort
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public void SetValue<T>(string key, T value)
        {
            lock (_lockObject)
            {
                // Handle nested keys
                var keys = key.Split('.');
                var currentDict = _configuration;

                for (int i = 0; i < keys.Length - 1; i++)
                {
                    var k = keys[i];
                    if (!currentDict.ContainsKey(k) || !(currentDict[k] is Dictionary<string, object>))
                    {
                        currentDict[k] = new Dictionary<string, object>();
                    }
                    currentDict = (Dictionary<string, object>)currentDict[k];
                }

                // Set the final value
                currentDict[keys[keys.Length - 1]] = value;

                // Save configuration
                SaveConfiguration();
            }
        }

        public void SetValue<T>(string section, string key, T value)
        {
            SetValue($"{section}.{key}", value);
        }

        private void SaveConfiguration()
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(_configuration, options);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save configuration: {ex.Message}");
            }
        }

        public Dictionary<string, object> GetSection(string sectionName)
        {
            lock (_lockObject)
            {
                if (_configuration.TryGetValue(sectionName, out var section) && section is Dictionary<string, object> dict)
                {
                    return dict;
                }
                return new Dictionary<string, object>();
            }
        }

        public void ReloadConfiguration()
        {
            lock (_lockObject)
            {
                var newConfig = LoadConfiguration();
                _configuration.Clear();
                foreach (var kvp in newConfig)
                {
                    _configuration[kvp.Key] = kvp.Value;
                }
            }
        }

        public void ResetToDefaults()
        {
            lock (_lockObject)
            {
                var defaultConfig = GetDefaultConfiguration();
                _configuration.Clear();
                foreach (var kvp in defaultConfig)
                {
                    _configuration[kvp.Key] = kvp.Value;
                }
                SaveConfiguration();
            }
        }
    }
}