using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace ScreenOCR
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly ILogger _logger;
        private readonly AppSettings _settings;
        private GeminiApiClient? _apiClient;
        private bool _isApiKeyModified = false;
        private const string APP_NAME = "ScreenOCR";
        private const string RUN_REGISTRY_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        
        public SettingsWindow(AppSettings settings, ILogger logger)
        {
            InitializeComponent();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize API client with the current API key
            if (!string.IsNullOrEmpty(settings.ApiKey))
            {
                _apiClient = new GeminiApiClient(settings.ApiKey, _logger);
            }

            // Load settings
            LoadSettings();
            
            _logger.LogInformation("Settings window initialized");
        }

        private void LoadSettings()
        {
            try
            {
                // Load API key
                if (!string.IsNullOrEmpty(_settings.ApiKey))
                {
                    var apiKeyPasswordBox = FindName("ApiKeyPasswordBox") as PasswordBox;
                    if (apiKeyPasswordBox != null)
                    {
                        apiKeyPasswordBox.Password = _settings.ApiKey;
                        _isApiKeyModified = false;
                    }
                }

                // Check if app is set to start with Windows
                using (var key = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_KEY, false))
                {
                    var startWithWindowsCheckBox = FindName("StartWithWindowsCheckBox") as CheckBox;
                    if (startWithWindowsCheckBox != null)
                    {
                        startWithWindowsCheckBox.IsChecked = key?.GetValue(APP_NAME) != null;
                    }
                }
                
                // Load double check feature setting
                var enableDoubleCheckCheckBox = FindName("EnableDoubleCheckCheckBox") as CheckBox;
                if (enableDoubleCheckCheckBox != null)
                {
                    enableDoubleCheckCheckBox.IsChecked = _settings.EnableDoubleCheck;
                }
                
                _logger.LogDebug("Settings loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings");
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowApiKey_Click(object sender, RoutedEventArgs e)
        {
            var apiKeyPasswordBox = FindName("ApiKeyPasswordBox") as PasswordBox;
            if (apiKeyPasswordBox != null)
            {
                MessageBox.Show(apiKeyPasswordBox.Password, "API Key", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApiKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _isApiKeyModified = true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save API key if modified
                if (_isApiKeyModified)
                {
                    var apiKeyPasswordBox = FindName("ApiKeyPasswordBox") as PasswordBox;
                    if (apiKeyPasswordBox != null)
                    {
                        string newApiKey = apiKeyPasswordBox.Password;
                        _settings.ApiKey = newApiKey;
                        _settings.SaveSettings();
                        
                        // Update or create API client with new key
                        if (_apiClient == null)
                        {
                            _apiClient = new GeminiApiClient(newApiKey, _logger);
                        }
                        else
                        {
                            // We'll handle this in the GeminiApiClient class update
                        }
                        
                        _logger.LogInformation("API key updated");
                    }
                }

                // Save double check feature setting
                var enableDoubleCheckCheckBox = FindName("EnableDoubleCheckCheckBox") as CheckBox;
                if (enableDoubleCheckCheckBox != null)
                {
                    _settings.EnableDoubleCheck = enableDoubleCheckCheckBox.IsChecked == true;
                    _settings.SaveSettings();
                    _logger.LogInformation($"Double check feature setting updated: {_settings.EnableDoubleCheck}");
                }

                // Handle startup with Windows setting
                var startWithWindowsCheckBox = FindName("StartWithWindowsCheckBox") as CheckBox;
                if (startWithWindowsCheckBox != null)
                {
                    SetStartWithWindows(startWithWindowsCheckBox.IsChecked == true);
                }
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void StartWithWindowsCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // This is handled when saving
        }

        private void EnableDoubleCheckCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // This is handled when saving
        }

        private void SetStartWithWindows(bool startWithWindows)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_KEY, true))
                {
                    if (key == null)
                    {
                        _logger.LogError("Could not open registry key for startup");
                        return;
                    }

                    if (startWithWindows)
                    {
                        string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        key.SetValue(APP_NAME, appPath);
                        _logger.LogInformation("Added application to Windows startup");
                    }
                    else
                    {
                        key.DeleteValue(APP_NAME, false);
                        _logger.LogInformation("Removed application from Windows startup");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring startup with Windows");
                throw;
            }
        }

        private async void TestApiConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable the button during testing
                var button = sender as Button;
                if (button != null)
                {
                    var originalContent = button.Content;
                    button.IsEnabled = false;
                    button.Content = "Testing...";

                    // Use current API key from password box for testing
                    var apiKeyPasswordBox = FindName("ApiKeyPasswordBox") as PasswordBox;
                    string apiKey = apiKeyPasswordBox?.Password ?? string.Empty;
                    
                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        MessageBox.Show("Please enter an API key first.", "API Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                        button.Content = originalContent;
                        button.IsEnabled = true;
                        return;
                    }

                    // Test the connection
                    bool isValid = false;
                    try
                    {
                        // Create a test client with the current API key
                        var testClient = new GeminiApiClient(apiKey, _logger);
                        
                        // Simple test - just check if we can make a basic API call
                        // We'll use a small dummy image for testing
                        using (var testImage = new System.Drawing.Bitmap(1, 1))
                        {
                            var result = await testClient.ExtractTextFromImageAsync(testImage, "Test connection");
                            isValid = !result.StartsWith("Error:");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "API connection test failed");
                        isValid = false;
                    }
                    
                    if (isValid)
                    {
                        MessageBox.Show("API connection successful! Your API key is valid.", 
                            "Connection Test", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("API connection failed. Please check your API key and try again.", 
                            "Connection Test", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    // Restore button state
                    button.Content = originalContent;
                    button.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API connection");
                MessageBox.Show($"Error testing API connection: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
