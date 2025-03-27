using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace ScreenOCR
{
    public class ToastNotification
    {
        private readonly ILogger _logger;
        private readonly Window _notificationWindow;
        private readonly DispatcherTimer _timer;
        
        public ToastNotification(string message, ILogger logger)
        {
            _logger = logger;
            
            // Create a simple window for notification
            _notificationWindow = new Window
            {
                Title = "Notification",
                Width = 300,
                Height = 80,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.Height
            };
            
            // Create border with rounded corners
            var border = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(100, 73, 217, 169)),
                Padding = new Thickness(10)
            };
            
            // Create text block for message
            var textBlock = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            // Add text to border
            border.Child = textBlock;
            
            // Set window content
            _notificationWindow.Content = border;
            
            // Position the window in the bottom right corner
            PositionWindow();
            
            // Create a timer to close the notification after a few seconds
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _timer.Tick += Timer_Tick;
            
            _logger.LogInformation($"Toast notification created: {message}");
        }
        
        private void PositionWindow()
        {
            // Position the window in the bottom right corner with some padding
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            
            _notificationWindow.Left = screenWidth - _notificationWindow.Width - 20;
            _notificationWindow.Top = screenHeight - _notificationWindow.Height - 20;
        }
        
        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();
            
            // Add fade-out animation
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            fadeOut.Completed += (s, _) => _notificationWindow.Close();
            _notificationWindow.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
        
        public void Show()
        {
            // Add fade-in animation
            _notificationWindow.Opacity = 0;
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            _notificationWindow.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            
            // Show the window and start the timer
            _notificationWindow.Show();
            _timer.Start();
        }
        
        public static void ShowNotification(string message, ILogger logger)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var notification = new ToastNotification(message, logger);
                notification.Show();
            });
        }
    }
}
