using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Extensions.Logging;
using System.Windows.Interop;
using System.Windows.Forms;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using Rectangle = System.Windows.Shapes.Rectangle;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using System.Runtime.InteropServices;

namespace ScreenOCR
{
    /// <summary>
    /// Interaction logic for ScreenshotOverlay
    /// </summary>
    public partial class ScreenshotOverlay : Window
    {
        private Point _startPoint;
        private bool _isSelecting;
        private readonly ILogger _logger;
        private readonly Action<Bitmap> _onScreenshotCaptured;

        // Selection state variables
        private bool _isShiftDown;
        private Point _shiftPoint;
        private double _selectLeft;
        private double _selectTop;
        private double _xShiftDelta;
        private double _yShiftDelta;

        // Define UI elements
        private readonly System.Windows.Controls.Canvas _mainCanvas;
        private readonly Rectangle _selectionRectangle;
        private readonly TextBlock _helperText;
        private readonly SolidColorBrush _backgroundBrush;
        private const double ActiveOpacity = 0.4;
        private System.Windows.Shapes.Path? _backgroundPath;

        // Import Windows API for cursor clipping
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetClipCursor(out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClipCursor(ref RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClipCursor(IntPtr lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public ScreenshotOverlay(ILogger logger, Action<Bitmap> onScreenshotCaptured)
        {
            _logger = logger;
            _onScreenshotCaptured = onScreenshotCaptured;

            // Initialize UI elements directly in the constructor
            _mainCanvas = new System.Windows.Controls.Canvas();
            _selectionRectangle = new Rectangle();
            _helperText = new TextBlock();
            _backgroundBrush = new SolidColorBrush(Color.FromArgb(102, 0, 0, 0));
            _backgroundPath = new System.Windows.Shapes.Path();

            // Set up UI
            SetupUI();

            _logger.LogInformation("Screenshot overlay initialized");
        }

        // Create UI programmatically without relying on XAML
        private void SetupUI()
        {
            // Set window properties
            this.Title = "Screenshot";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Topmost = true;
            this.WindowState = WindowState.Maximized;
            this.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            this.Cursor = System.Windows.Input.Cursors.Cross;

            // Configure main canvas
            // Chỉnh sửa: đặt nền Transparent để nhận sự kiện chuột trên toàn bộ canvas
            _mainCanvas.Background = new SolidColorBrush(Colors.Transparent);

            // Configure selection rectangle
            Color borderColor = Color.FromArgb(255, 40, 118, 126); // Màu tương tự PowerOCR
            _selectionRectangle.Stroke = new SolidColorBrush(borderColor);
            _selectionRectangle.StrokeThickness = 1;
            _selectionRectangle.Fill = null;
            _selectionRectangle.Visibility = Visibility.Hidden;

            // Create a semi-transparent overlay that will have a "hole" in it
            _backgroundPath = new System.Windows.Shapes.Path();
            _backgroundPath.Fill = _backgroundBrush;
            _backgroundPath.IsHitTestVisible = false; // Cho phép các sự kiện chuột đi qua
            _mainCanvas.Children.Add(_backgroundPath);
_backgroundPath.Data = new RectangleGeometry(new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight));

            // Configure helper text
            _helperText.Foreground = new SolidColorBrush(Colors.White);
            _helperText.Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
            _helperText.Padding = new Thickness(5);
            _helperText.Text = "Click and drag to select an area to capture - Press Esc to cancel - Hold Shift to adjust selection";

            // Set the canvas as child of the window
            this.Content = _mainCanvas;

            // Add elements to canvas
            System.Windows.Controls.Canvas.SetTop(_helperText, 10);
            System.Windows.Controls.Canvas.SetLeft(_helperText, 10);
            _mainCanvas.Children.Add(_selectionRectangle);
            _mainCanvas.Children.Add(_helperText);

            // Set up keyboard events
            KeyDown += ScreenshotOverlay_KeyDown;
            KeyUp += ScreenshotOverlay_KeyUp;

            // Set up mouse events
            _mainCanvas.MouseDown += MainCanvas_MouseDown;
            _mainCanvas.MouseMove += MainCanvas_MouseMove;
            _mainCanvas.MouseUp += MainCanvas_MouseUp;
        }

        private void ScreenshotOverlay_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _logger.LogInformation("Screenshot cancelled by user (Escape key)");
                DialogResult = false;
                Close();
            }
        }

        private void ScreenshotOverlay_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftShift:
                case Key.RightShift:
                    _isShiftDown = false;
                    _startPoint = new Point(_startPoint.X + _xShiftDelta, _startPoint.Y + _yShiftDelta);
                    break;
                default:
                    break;
            }
        }

        private void MainCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            _helperText.Visibility = Visibility.Collapsed;
            _mainCanvas.CaptureMouse();

            // Clip cursor to window boundaries
            ClipCursorToWindow();

            _startPoint = e.GetPosition(_mainCanvas);
            _isSelecting = true;
            _selectionRectangle.Visibility = Visibility.Visible;

            // Reset selection rectangle
            _selectionRectangle.Width = 1;
            _selectionRectangle.Height = 1;

            System.Windows.Controls.Canvas.SetLeft(_selectionRectangle, _startPoint.X);
            System.Windows.Controls.Canvas.SetTop(_selectionRectangle, _startPoint.Y);

            _logger.LogDebug($"Started selection at {_startPoint.X}, {_startPoint.Y}");
        }

        private void MainCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isSelecting) return;

            Point movingPoint = e.GetPosition(_mainCanvas);

            // Check if shift is pressed for adjusting selection
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                if (!_isShiftDown)
                {
                    _shiftPoint = movingPoint;
                    _selectLeft = System.Windows.Controls.Canvas.GetLeft(_selectionRectangle);
                    _selectTop = System.Windows.Controls.Canvas.GetTop(_selectionRectangle);
                }

                _isShiftDown = true;
                _xShiftDelta = movingPoint.X - _shiftPoint.X;
                _yShiftDelta = movingPoint.Y - _shiftPoint.Y;

                double leftValue = _selectLeft + _xShiftDelta;
                double topValue = _selectTop + _yShiftDelta;

                System.Windows.Controls.Canvas.SetLeft(_selectionRectangle, leftValue);
                System.Windows.Controls.Canvas.SetTop(_selectionRectangle, topValue);
                return;
            }

            _isShiftDown = false;

            // Calculate rectangle dimensions
            double left = Math.Min(_startPoint.X, movingPoint.X);
            double top = Math.Min(_startPoint.Y, movingPoint.Y);
            double width = Math.Abs(movingPoint.X - _startPoint.X);
            double height = Math.Abs(movingPoint.Y - _startPoint.Y);

            // Update rectangle
            System.Windows.Controls.Canvas.SetLeft(_selectionRectangle, left);
            System.Windows.Controls.Canvas.SetTop(_selectionRectangle, top);
            _selectionRectangle.Width = width;
            _selectionRectangle.Height = height;

            // Update background path
            GeometryGroup geometry = new GeometryGroup();
            geometry.FillRule = FillRule.EvenOdd; // Tạo hiệu ứng "lỗ hổng"
            geometry.Children.Add(new RectangleGeometry(new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight)));
            geometry.Children.Add(new RectangleGeometry(new Rect(left, top, width, height)));
            if (_backgroundPath != null)
            {
                _backgroundPath.Data = geometry;
            }
        }

        private void MainCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_isSelecting) return;

            _isSelecting = false;
            _mainCanvas.ReleaseMouseCapture();

            // Unclip cursor
            UnClipCursor();

            _helperText.Visibility = Visibility.Visible;

            var endPoint = e.GetPosition(_mainCanvas);

            // Ensure minimum size
            if (Math.Abs(endPoint.X - _startPoint.X) < 10 || Math.Abs(endPoint.Y - _startPoint.Y) < 10)
            {
                _logger.LogInformation("Selection too small, cancelled");
                DialogResult = false;
                Close();
                return;
            }

            // Capture the selected area
            try
            {
                CaptureSelectedArea();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing screenshot");
                DialogResult = false;
            }

            Close();
        }

        private void CaptureSelectedArea()
        {
            // Calculate the bounds of the selection
            double x = System.Windows.Controls.Canvas.GetLeft(_selectionRectangle);
            double y = System.Windows.Controls.Canvas.GetTop(_selectionRectangle);
            double width = _selectionRectangle.Width;
            double height = _selectionRectangle.Height;

            // Account for DPI scaling
            var presentationSource = PresentationSource.FromVisual(this);
            if (presentationSource != null && presentationSource.CompositionTarget != null)
            {
                Matrix m = presentationSource.CompositionTarget.TransformToDevice;
                double dpiX = m.M11;
                double dpiY = m.M22;

                int scaledX = (int)(x * dpiX);
                int scaledY = (int)(y * dpiY);
                int scaledWidth = (int)(width * dpiX);
                int scaledHeight = (int)(height * dpiY);

                // Round to whole pixels
                scaledX = (int)Math.Round((double)scaledX);
                scaledY = (int)Math.Round((double)scaledY);
                scaledWidth = (int)Math.Round((double)scaledWidth);
                scaledHeight = (int)Math.Round((double)scaledHeight);

                _logger.LogDebug($"Capturing area at {scaledX},{scaledY} with size {scaledWidth}x{scaledHeight}");

                // Create a bitmap to store the screenshot
                using var bitmap = new Bitmap(scaledWidth, scaledHeight);
                using var graphics = Graphics.FromImage(bitmap);

                // Capture the screen
                graphics.CopyFromScreen(
                    scaledX,
                    scaledY,
                    0,
                    0,
                    new System.Drawing.Size(scaledWidth, scaledHeight)
                );

                _logger.LogInformation($"Captured screenshot of size {scaledWidth}x{scaledHeight}");

                // Pass bitmap to callback
                _onScreenshotCaptured(bitmap);
            }
        }

        private void ClipCursorToWindow()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            Screen screen = Screen.FromHandle(handle);
            RECT rect = new RECT
            {
                Left = screen.Bounds.Left,
                Top = screen.Bounds.Top,
                Right = screen.Bounds.Right,
                Bottom = screen.Bounds.Bottom
            };

            ClipCursor(ref rect);
        }

        private void UnClipCursor()
        {
            ClipCursor(IntPtr.Zero);
        }
    }
}
