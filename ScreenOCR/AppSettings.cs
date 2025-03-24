using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ScreenOCR
{
    public class AppSettings
    {
        private readonly ILogger _logger;
        private readonly string _settingsFilePath;

        public string ApiKey { get; set; } = string.Empty;
        public bool HasApiKey => !string.IsNullOrEmpty(ApiKey);
        public bool EnableDoubleCheck { get; set; } = true; // Default to true for backward compatibility

        public AppSettings(ILogger logger)
        {
            _logger = logger;
            
            // Set path to settings file in the db folder
            var dbFolderPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "db"
            );
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(dbFolderPath))
            {
                Directory.CreateDirectory(dbFolderPath);
            }
            
            _settingsFilePath = Path.GetFullPath(Path.Combine(dbFolderPath, "apikey.json"));
            _logger.LogDebug($"Settings file path: {_settingsFilePath}");
            
            // Load settings
            LoadSettings();
        }

        public void LoadSettings()
        {
            try
            {
                _logger.LogDebug($"Attempting to load settings from: {_settingsFilePath}");
                if (File.Exists(_settingsFilePath))
                {
                    _logger.LogDebug("File exists, loading content");
                    var json = File.ReadAllText(_settingsFilePath);
                    _logger.LogDebug($"File content: {json}");
                    
                    try
                    {
                        // Use a strongly-typed deserializer to avoid null reference issues
                        var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                        if (dataDict != null)
                        {
                            // Load API Key
                            if (dataDict.TryGetValue("ApiKey", out var apiKeyValue) && apiKeyValue != null && !string.IsNullOrEmpty(apiKeyValue.ToString()))
                            {
                                ApiKey = apiKeyValue.ToString() ?? string.Empty;
                                _logger.LogInformation($"API key loaded successfully from {_settingsFilePath}");
                                _logger.LogDebug($"HasApiKey is now: {HasApiKey}");
                            }
                            else
                            {
                                _logger.LogWarning("Failed to extract API key from JSON file - key not found or empty");
                            }
                            
                            // Load EnableDoubleCheck setting
                            if (dataDict.TryGetValue("EnableDoubleCheck", out var doubleCheckValue) && doubleCheckValue != null)
                            {
                                if (bool.TryParse(doubleCheckValue.ToString(), out bool enableDoubleCheck))
                                {
                                    EnableDoubleCheck = enableDoubleCheck;
                                    _logger.LogInformation($"EnableDoubleCheck setting loaded successfully: {EnableDoubleCheck}");
                                }
                            }
                        }
                    }
                    catch
                    {
                        _logger.LogWarning("Failed to parse JSON using Dictionary, trying dynamic approach");
                        
                        // Fallback to dynamic approach with null checks
                        var data = JsonConvert.DeserializeObject<dynamic>(json);
                        if (data != null)
                        {
                            try
                            {
                                // Load API Key
                                string? apiKey = data?.ApiKey?.ToString();
                                if (!string.IsNullOrEmpty(apiKey))
                                {
                                    ApiKey = apiKey;
                                    _logger.LogInformation($"API key loaded successfully using dynamic approach");
                                    _logger.LogDebug($"HasApiKey is now: {HasApiKey}");
                                }
                                else
                                {
                                    _logger.LogWarning("API key property is null or empty in JSON");
                                }
                                
                                // Load EnableDoubleCheck setting
                                try
                                {
                                    bool? enableDoubleCheck = data?.EnableDoubleCheck;
                                    if (enableDoubleCheck.HasValue)
                                    {
                                        EnableDoubleCheck = enableDoubleCheck.Value;
                                        _logger.LogInformation($"EnableDoubleCheck setting loaded successfully: {EnableDoubleCheck}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Error accessing EnableDoubleCheck property");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error accessing ApiKey property");
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogInformation($"Settings file does not exist at {_settingsFilePath}, using default settings");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading settings from {_settingsFilePath}");
            }
        }

        public void SaveSettings()
        {
            try
            {
                // Create a simple object with just the properties we want to save
                var dataToSave = new { ApiKey = this.ApiKey, EnableDoubleCheck = this.EnableDoubleCheck };
                var json = JsonConvert.SerializeObject(dataToSave, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
                _logger.LogInformation($"Settings saved successfully to {_settingsFilePath}");
                _logger.LogDebug($"Saved content: {json}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving settings to {_settingsFilePath}");
            }
        }
    }
}
