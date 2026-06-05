from PyQt6.QtWidgets import QWidget, QLabel, QVBoxLayout
from PyQt6.QtCore import Qt, QTimer, QPropertyAnimation, QRect
from PyQt6.QtGui import QGuiApplication

class ToastNotification(QWidget):
    def __init__(self, message="Copied to Clipboard!", duration=3000):
        super().__init__()
        # Frameless, Always on Top, does not steal focus
        self.setWindowFlags(
            Qt.WindowType.FramelessWindowHint |
            Qt.WindowType.WindowStaysOnTopHint |
            Qt.WindowType.Tool |
            Qt.WindowType.WindowDoesNotAcceptFocus
        )
        self.setAttribute(Qt.WidgetAttribute.WA_ShowWithoutActivating)
        
        # Style directly for Toast according to Industrial Design (green border)
        self.setStyleSheet("""
            QWidget {
                background-color: #1E1E1E;
                border: 1px solid #333333;
                border-left: 4px solid #00E676; 
                color: #F0F0F0;
            }
            QLabel {
                font-family: 'Inter', sans-serif;
                font-size: 14px;
                font-weight: bold;
                border: none;
            }
        """)

        layout = QVBoxLayout()
        label = QLabel(message)
        layout.addWidget(label)
        self.setLayout(layout)
        
        self.resize(300, 60)
        self.duration = duration

    def show_toast(self):
        screen = QGuiApplication.primaryScreen().availableGeometry()
        
        # Start off-screen at the bottom right
        start_x = screen.width()
        start_y = screen.height() - self.height() - 50
        
        # End (Visible position)
        end_x = screen.width() - self.width() - 20
        end_y = start_y
        
        self.setGeometry(start_x, start_y, self.width(), self.height())
        self.show()
        
        # Slide in animation
        self.anim_in = QPropertyAnimation(self, b"geometry")
        self.anim_in.setDuration(200)
        self.anim_in.setStartValue(QRect(start_x, start_y, self.width(), self.height()))
        self.anim_in.setEndValue(QRect(end_x, end_y, self.width(), self.height()))
        self.anim_in.start()

        # Timer to hide
        QTimer.singleShot(self.duration, self.hide_toast)

    def hide_toast(self):
        self.close()
