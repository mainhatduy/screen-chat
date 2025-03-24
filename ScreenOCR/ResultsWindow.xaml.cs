using System;
using System.Windows;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ScreenOCR
{
    /// <summary>
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ResultsWindow : Window
    {
        private readonly ILogger _logger;

        public class OCRResult
        {
            public required string text { get; set; }
        }


        public ResultsWindow(string extractedText, ILogger logger)
        {
            InitializeComponent();
            _logger = logger;
            
            try
            {
                // Parse chuỗi JSON
                var ocrResult = JsonSerializer.Deserialize<OCRResult>(extractedText);
                // Gán giá trị của thuộc tính "text" cho TextBox
                ResultTextBox.Text = ocrResult?.text ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing JSON extractedText");
                ResultTextBox.Text = extractedText; // Hoặc xử lý theo cách khác nếu parse thất bại
            }
            
            _logger.LogInformation("Results window displayed");
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ResultTextBox.Text);
                _logger.LogInformation("Text copied to clipboard");
                // MessageBox.Show("Text copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying to clipboard");
                // MessageBox.Show($"Error copying to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
