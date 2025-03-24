using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace ScreenOCR
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationWindow : Window
    {
        private readonly ILogger _logger;
        private readonly DispatcherTimer _timer;
        
        public NotificationWindow(string message, ILogger logger)
        {
            InitializeComponent();
            _logger = logger;
            
            // Set the notification message
            NotificationText.Text = message;
            
            // Position the window in the bottom right corner
            PositionWindow();
            
            // Create a timer to close the notification after a few seconds
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            
            // Add fade-in animation
            Opacity = 0;
            DoubleAnimation fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            BeginAnimation(OpacityProperty, fadeIn);
            
            _logger.LogInformation($"Notification displayed: {message}");
        }
        
        private void PositionWindow()
        {
            // Position the window in the bottom right corner with some padding
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            
            Left = screenWidth - Width - 20;
            Top = screenHeight - Height - 20;
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
            fadeOut.Completed += (s, _) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }
        
        public static void ShowNotification(string message, ILogger logger)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var notification = new NotificationWindow(message, logger);
                notification.Show();
            });
        }
    }
}
