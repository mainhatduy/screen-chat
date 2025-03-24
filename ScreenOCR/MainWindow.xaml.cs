using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ScreenOCR;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private NativeHotkey? _hotkey;
    private readonly ILogger _logger;
    private readonly AppSettings _settings;
    private GeminiApiClient? _geminiClient;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Set up logging using Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/screenocr.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        // Create logger and settings
        _logger = new SerilogLoggerFactory(Log.Logger).CreateLogger("ScreenOCR");
        _settings = new AppSettings(_logger);
        
        // Hide window on startup
        // We'll show it once to register hotkey, then hide in Window_Loaded
        // Visibility = Visibility.Hidden;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Register hotkey (Ctrl+Shift+T)
            _hotkey = new NativeHotkey(
                this,
                1,
                NativeHotkey.ModifierKeys.Control | NativeHotkey.ModifierKeys.Shift,
                0x54, // 'T' key
                CaptureScreenshot,
                _logger
            );
            
            // Initialize Gemini API client if API key is available
            if (_settings.HasApiKey)
            {
                _geminiClient = new GeminiApiClient(_settings.ApiKey, _logger);
                _logger.LogInformation("Gemini API client initialized");
            }
            else
            {
                _logger.LogWarning("API key not found. Please set up your API key in settings.");
                ShowSettings();
            }
            
            _logger.LogInformation("ScreenOCR started successfully");
            
            // Hide window after registering hotkey
            Visibility = Visibility.Hidden;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing application");
            MessageBox.Show(
                $"Error initializing application: {ex.Message}",
                "Error",
                MessageBoxButton.OK, 
                MessageBoxImage.Error
            );
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Unregister hotkey
        _hotkey?.Unregister();
        
        // Clean up notifyicon
        NotifyIcon.Dispose();
        
        _logger.LogInformation("ScreenOCR shutting down");
    }

    private void CaptureScreenshot()
    {
        try
        {
            if (_geminiClient == null && !_settings.HasApiKey)
            {
                _logger.LogWarning("API key not set. Opening settings window.");
                Dispatcher.Invoke(ShowSettings);
                return;
            }
            
            // Initialize Gemini client if needed
            if (_geminiClient == null && _settings.HasApiKey)
            {
                _geminiClient = new GeminiApiClient(_settings.ApiKey, _logger);
            }

            // Show the screenshot capture overlay
            var overlay = new ScreenshotOverlay(_logger, ProcessScreenshot);
            overlay.ShowDialog();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing screenshot");
            MessageBox.Show(
                $"Error capturing screenshot: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
    
    private async void ProcessScreenshot(Bitmap screenshot)
    {
        try
        {
            if (_geminiClient == null)
            {
                _logger.LogError("Gemini API client not initialized");
                return;
            }
            
            // Extract text using Gemini API
            var result = await _geminiClient.ExtractTextFromImageAsync(screenshot);
            
            // Show results based on the double check feature setting
            Dispatcher.Invoke(() => 
            {
                if (_settings.EnableDoubleCheck)
                {
                    // Show the results window for user review
                    var resultsWindow = new ResultsWindow(result, _logger);
                    resultsWindow.ShowDialog();
                }
                else
                {
                    try
                    {
                        // Parse the JSON result
                        var ocrResult = System.Text.Json.JsonSerializer.Deserialize<ResultsWindow.OCRResult>(result);
                        string extractedText = ocrResult?.text ?? string.Empty;
                        
                        // Copy text directly to clipboard
                        Clipboard.SetText(extractedText);
                        
                        // Show notification
                        ToastNotification.ShowNotification("Text copied to clipboard!", _logger);
                        
                        _logger.LogInformation("Text automatically copied to clipboard (Double Check disabled)");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing result with Double Check disabled");
                        MessageBox.Show(
                            $"Error processing result: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing screenshot");
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    $"Error processing screenshot: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            });
        }
    }

    private void ShowSettings()
    {
        try
        {
            var settingsWindow = new SettingsWindow(_settings, _logger);
            var result = settingsWindow.ShowDialog();
            
            if (result == true)
            {
                // Reload API key
                if (_settings.HasApiKey)
                {
                    _geminiClient = new GeminiApiClient(_settings.ApiKey, _logger);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing settings window");
            MessageBox.Show(
                $"Error showing settings: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
    
    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        ShowSettings();
    }
    
    private void CaptureMenuItem_Click(object sender, RoutedEventArgs e)
    {
        CaptureScreenshot();
    }
    
    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}