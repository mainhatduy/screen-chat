using System;
using System.Windows;

namespace ScreenOCR
{
    /// <summary>
    /// Interaction logic for PromptDialog.xaml
    /// </summary>
    public partial class PromptDialog : Window
    {
        public string PromptName { get; private set; } = string.Empty;
        public string PromptText { get; private set; } = string.Empty;
        
        public PromptDialog()
        {
            InitializeComponent();
        }
        
        public PromptDialog(string name, string text) : this()
        {
            PromptNameTextBox.Text = name;
            PromptTextBox.Text = text;
        }
        
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(PromptNameTextBox.Text))
            {
                MessageBox.Show("Please enter a name for the prompt.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(PromptTextBox.Text))
            {
                MessageBox.Show("Please enter text for the prompt.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Set properties and close dialog
            PromptName = PromptNameTextBox.Text.Trim();
            PromptText = PromptTextBox.Text.Trim();
            
            DialogResult = true;
            Close();
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}