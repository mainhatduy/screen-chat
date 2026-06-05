from PyQt6.QtWidgets import QSystemTrayIcon, QMenu
from PyQt6.QtGui import QIcon, QAction
from PyQt6.QtCore import pyqtSignal, QObject
from loguru import logger

class TrayController(QObject):
    show_settings_signal = pyqtSignal()
    quit_signal = pyqtSignal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self.tray_icon = QSystemTrayIcon(parent)
        
        # Use default system icon for camera
        icon = QIcon.fromTheme("camera-photo")
        self.tray_icon.setIcon(icon)
        
        self.menu = QMenu()
        
        self.settings_action = QAction("Settings", self)
        self.settings_action.triggered.connect(self.show_settings_signal.emit)
        self.menu.addAction(self.settings_action)
        
        self.menu.addSeparator()
        
        self.quit_action = QAction("Quit ScreenChat", self)
        self.quit_action.triggered.connect(self.quit_signal.emit)
        self.menu.addAction(self.quit_action)
        
        self.tray_icon.setContextMenu(self.menu)
        
    def show(self):
        self.tray_icon.show()
        logger.info("Tray icon displayed.")
