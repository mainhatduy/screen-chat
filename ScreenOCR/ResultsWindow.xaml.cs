using System;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace ScreenOCR
{
    /// <summary>
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ResultsWindow : Window
    {
        private readonly ILogger _logger;

        public ResultsWindow(string extractedText, ILogger logger)
        {
            InitializeComponent();
            _logger = logger;
            
            ResultTextBox.Text = extractedText;
            _logger.LogInformation("Results window displayed");
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ResultTextBox.Text);
                _logger.LogInformation("Text copied to clipboard");
                MessageBox.Show("Text copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying to clipboard");
                MessageBox.Show($"Error copying to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
