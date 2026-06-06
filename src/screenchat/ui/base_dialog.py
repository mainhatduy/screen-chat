from PyQt6.QtWidgets import QDialog, QVBoxLayout, QHBoxLayout, QLabel, QPushButton, QWidget, QFrame
from PyQt6.QtCore import Qt, QPoint
from PyQt6.QtGui import QMouseEvent

class ModernDialog(QDialog):
    def __init__(self, parent=None, title="", stays_on_top=False):
        super().__init__(parent)
        
        # Frameless window and translucent background
        flags = Qt.WindowType.FramelessWindowHint
        if stays_on_top:
            flags |= Qt.WindowType.WindowStaysOnTopHint
        self.setWindowFlags(flags)
        self.setAttribute(Qt.WidgetAttribute.WA_TranslucentBackground)
        
        self.drag_position = QPoint()

        # Outer layout to hold the custom decorated frame
        outer_layout = QVBoxLayout(self)
        outer_layout.setContentsMargins(4, 4, 4, 4)  # Small margin to prevent clipping border
        outer_layout.setSpacing(0)

        # The main container frame with border and rounded corners
        self.container_frame = QFrame()
        self.container_frame.setObjectName("ModernDialogOuterFrame")
        outer_layout.addWidget(self.container_frame)

        # Layout inside the container
        self.container_layout = QVBoxLayout(self.container_frame)
        self.container_layout.setContentsMargins(0, 0, 0, 0)
        self.container_layout.setSpacing(0)

        # Title bar
        self.title_bar = QWidget()
        self.title_bar.setObjectName("ModernDialogTitleBar")
        self.title_bar.setFixedHeight(44)

        title_bar_layout = QHBoxLayout(self.title_bar)
        title_bar_layout.setContentsMargins(16, 0, 16, 0)
        title_bar_layout.setSpacing(10)

        self.title_label = QLabel(title)
        self.title_label.setObjectName("ModernDialogTitle")
        self.title_label.setAlignment(Qt.AlignmentFlag.AlignCenter)

        self.close_btn = QPushButton("×")
        self.close_btn.setObjectName("ModernDialogCloseBtn")
        self.close_btn.setFixedSize(24, 24)
        self.close_btn.clicked.connect(self.reject)

        # Spacer to keep title centered relative to the close button
        title_bar_layout.addSpacing(24)
        title_bar_layout.addWidget(self.title_label, 1)
        title_bar_layout.addWidget(self.close_btn)

        self.container_layout.addWidget(self.title_bar)

        # Content area widget
        self.content_widget = QWidget()
        self.content_widget.setObjectName("ModernDialogContentArea")
        self.content_layout = QVBoxLayout(self.content_widget)
        self.content_layout.setContentsMargins(20, 20, 20, 20)
        self.content_layout.setSpacing(16)
        
        self.container_layout.addWidget(self.content_widget)

    def mousePressEvent(self, event: QMouseEvent):
        if event.button() == Qt.MouseButton.LeftButton:
            # Check if the click occurred within the bounds of the title bar using local coordinates
            local_pos = event.position().toPoint()
            title_bar_pos = self.title_bar.mapFrom(self, local_pos)
            if self.title_bar.rect().contains(title_bar_pos):
                window_handle = self.window().windowHandle()
                if window_handle:
                    window_handle.startSystemMove()
                else:
                    self.drag_position = event.globalPosition().toPoint()
                event.accept()

    def mouseMoveEvent(self, event: QMouseEvent):
        if event.buttons() == Qt.MouseButton.LeftButton and not self.drag_position.isNull():
            global_pos = event.globalPosition().toPoint()
            delta = global_pos - self.drag_position
            self.move(self.pos() + delta)
            self.drag_position = global_pos
            event.accept()

    def mouseReleaseEvent(self, event: QMouseEvent):
        self.drag_position = QPoint()

