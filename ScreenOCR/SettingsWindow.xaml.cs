using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Linq;

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
        private bool _isPromptsModified = false;
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
                
                // Load custom prompts
                LoadCustomPrompts();
                
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

                // Save selected prompt if modified
                if (_isPromptsModified)
                {
                    var promptsComboBox = FindName("PromptsComboBox") as ComboBox;
                    if (promptsComboBox != null && promptsComboBox.SelectedItem is CustomPrompt selectedPrompt)
                    {
                        _settings.SelectedPromptName = selectedPrompt.Name;
                        _settings.SaveSettings();
                        _logger.LogInformation($"Selected prompt updated: {selectedPrompt.Name}");
                    }
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
        
        #region Custom Prompts Management
        
        private void LoadCustomPrompts()
        {
            try
            {
                var promptsComboBox = FindName("PromptsComboBox") as ComboBox;
                if (promptsComboBox != null)
                {
                    promptsComboBox.Items.Clear();
                    
                    // Add default prompt
                    var defaultPrompt = new CustomPrompt { Name = "Default", Text = GeminiApiClient.DEFAULT_PROMPT };
                    promptsComboBox.Items.Add(defaultPrompt);
                    
                    // Add custom prompts
                    foreach (var prompt in _settings.CustomPrompts)
                    {
                        promptsComboBox.Items.Add(prompt);
                    }
                    
                    // Select the current prompt
                    if (!string.IsNullOrEmpty(_settings.SelectedPromptName))
                    {
                        var selectedPrompt = _settings.CustomPrompts.FirstOrDefault(p => p.Name == _settings.SelectedPromptName);
                        if (selectedPrompt != null)
                        {
                            promptsComboBox.SelectedItem = selectedPrompt;
                        }
                        else
                        {
                            promptsComboBox.SelectedIndex = 0; // Select default
                        }
                    }
                    else
                    {
                        promptsComboBox.SelectedIndex = 0; // Select default
                    }
                    
                    _isPromptsModified = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading custom prompts");
                MessageBox.Show($"Error loading custom prompts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void PromptsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is CustomPrompt)
            {
                _isPromptsModified = true;
            }
        }
        
        private void AddPrompt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new PromptDialog();
                dialog.Owner = this;
                
                if (dialog.ShowDialog() == true)
                {
                    // Check if a prompt with this name already exists
                    if (_settings.CustomPrompts.Any(p => p.Name == dialog.PromptName))
                    {
                        MessageBox.Show($"A prompt with the name '{dialog.PromptName}' already exists. Please choose a different name.", 
                            "Duplicate Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    
                    // Add the new prompt
                    var newPrompt = new CustomPrompt { Name = dialog.PromptName, Text = dialog.PromptText };
                    _settings.CustomPrompts.Add(newPrompt);
                    _settings.SaveSettings();
                    
                    // Reload prompts and select the new one
                    LoadCustomPrompts();
                    var promptsComboBox = FindName("PromptsComboBox") as ComboBox;
                    if (promptsComboBox != null)
                    {
                        var addedPrompt = _settings.CustomPrompts.FirstOrDefault(p => p.Name == dialog.PromptName);
                        if (addedPrompt != null)
                        {
                            promptsComboBox.SelectedItem = addedPrompt;
                            _isPromptsModified = true;
                        }
                    }
                    
                    _logger.LogInformation($"Added new prompt: {dialog.PromptName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding custom prompt");
                MessageBox.Show($"Error adding custom prompt: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void EditPrompt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var promptsComboBox = FindName("PromptsComboBox") as ComboBox;
                if (promptsComboBox != null && promptsComboBox.SelectedItem is CustomPrompt selectedPrompt)
                {
                    // Don't allow editing the default prompt
                    if (selectedPrompt.Name == "Default")
                    {
                        MessageBox.Show("The default prompt cannot be edited.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    
                    var dialog = new PromptDialog(selectedPrompt.Name, selectedPrompt.Text);
                    dialog.Owner = this;
                    
                    if (dialog.ShowDialog() == true)
                    {
                        // Check if the name was changed and if it conflicts with an existing prompt
                        if (dialog.PromptName != selectedPrompt.Name && 
                            _settings.CustomPrompts.Any(p => p.Name == dialog.PromptName))
                        {
                            MessageBox.Show($"A prompt with the name '{dialog.PromptName}' already exists. Please choose a different name.", 
                                "Duplicate Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        
                        // Update the prompt
                        var promptToUpdate = _settings.CustomPrompts.FirstOrDefault(p => p.Name == selectedPrompt.Name);
                        if (promptToUpdate != null)
                        {
                            // If the name was changed, update the selected prompt name in settings
                            if (_settings.SelectedPromptName == promptToUpdate.Name)
                            {
                                _settings.SelectedPromptName = dialog.PromptName;
                            }
                            
                            promptToUpdate.Name = dialog.PromptName;
                            promptToUpdate.Text = dialog.PromptText;
                            _settings.SaveSettings();
                            
                            // Reload prompts and select the updated one
                            LoadCustomPrompts();
                            var updatedPrompt = _settings.CustomPrompts.FirstOrDefault(p => p.Name == dialog.PromptName);
                            if (updatedPrompt != null)
                            {
                                promptsComboBox.SelectedItem = updatedPrompt;
                                _isPromptsModified = true;
                            }
                            
                            _logger.LogInformation($"Updated prompt: {dialog.PromptName}");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please select a prompt to edit.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing custom prompt");
                MessageBox.Show($"Error editing custom prompt: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void DeletePrompt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var promptsComboBox = FindName("PromptsComboBox") as ComboBox;
                if (promptsComboBox != null && promptsComboBox.SelectedItem is CustomPrompt selectedPrompt)
                {
                    // Don't allow deleting the default prompt
                    if (selectedPrompt.Name == "Default")
                    {
                        MessageBox.Show("The default prompt cannot be deleted.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    
                    var result = MessageBox.Show($"Are you sure you want to delete the prompt '{selectedPrompt.Name}'?", 
                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // Delete the prompt
                        if (_settings.DeletePrompt(selectedPrompt.Name))
                        {
                            _settings.SaveSettings();
                            
                            // Reload prompts
                            LoadCustomPrompts();
                            _isPromptsModified = true;
                            
                            _logger.LogInformation($"Deleted prompt: {selectedPrompt.Name}");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Please select a prompt to delete.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting custom prompt");
                MessageBox.Show($"Error deleting custom prompt: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        #endregion
    }
}
