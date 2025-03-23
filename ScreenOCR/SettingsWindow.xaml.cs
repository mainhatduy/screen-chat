using System;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ScreenOCR
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly ILogger _logger;
        private readonly AppSettings _settings;
        private bool _isApiKeyModified = false;
        private const string APP_NAME = "ScreenOCR";
        private const string RUN_REGISTRY_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public SettingsWindow(AppSettings settings, ILogger logger)
        {
            InitializeComponent();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                    ApiKeyPasswordBox.Password = _settings.ApiKey;
                    _isApiKeyModified = false;
                }

                // Check if app is set to start with Windows
                using (var key = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_KEY, false))
                {
                    StartWithWindowsCheckBox.IsChecked = key?.GetValue(APP_NAME) != null;
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
            MessageBox.Show(ApiKeyPasswordBox.Password, "API Key", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    _settings.ApiKey = ApiKeyPasswordBox.Password;
                    _settings.SaveSettings();
                    _logger.LogInformation("API key updated");
                }

                // Handle startup with Windows setting
                SetStartWithWindows(StartWithWindowsCheckBox.IsChecked == true);
                
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
    }
}
