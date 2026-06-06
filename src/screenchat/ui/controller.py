import os
from loguru import logger
from PyQt6.QtWidgets import QApplication
from PyQt6.QtCore import QObject

from .tray import TrayController
from .settings import SettingsWindow
from .review import ReviewWindow
from .toast import ToastNotification
from ..application.use_cases import CaptureAndProcessUseCase, SettingsUseCase
from ..domain.interfaces import ClipboardService
from ..adapters.ipc_server import IPCServerThread

class ScreenChatApp(QObject):
    def __init__(
        self, 
        app: QApplication, 
        capture_use_case: CaptureAndProcessUseCase, 
        settings_use_case: SettingsUseCase,
        clipboard_svc: ClipboardService,
        ipc_thread: IPCServerThread
    ):
        super().__init__()
        self.app = app
        self.capture_use_case = capture_use_case
        self.settings_use_case = settings_use_case
        self.clipboard_svc = clipboard_svc
        self.ipc_thread = ipc_thread
        
        # Load style.qss
        self.load_stylesheet()

        # Init UI
        self.tray = TrayController()
        self.tray.show_settings_signal.connect(self.open_settings)
        self.tray.quit_signal.connect(self.quit_app)
        self.tray.show()

        # Open settings window on startup to notify user that the application is running
        self.open_settings()

        # Connect IPC Server capture requested signal
        self.ipc_thread.capture_requested.connect(self.handle_capture_request)
        # The main.py compositions starts the thread, but we ensure it's running here or started
        if not self.ipc_thread.isRunning():
            self.ipc_thread.start()

    def load_stylesheet(self):
        try:
            qss_path = os.path.join(os.path.dirname(__file__), "style.qss")
            if os.path.exists(qss_path):
                with open(qss_path, "r", encoding="utf-8") as f:
                    qss_content = f.read()
                
                # Resolve resources directory path dynamically
                resources_dir = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "resources"))
                resources_dir_url = resources_dir.replace("\\", "/")
                qss_content = qss_content.replace("{{RESOURCES_DIR}}", resources_dir_url)
                
                self.app.setStyleSheet(qss_content)
            else:
                logger.warning(f"Stylesheet not found at {qss_path}")
        except Exception as e:
            logger.error(f"Error loading QSS: {e}")

    def open_settings(self):
        self.settings_window = SettingsWindow(self.settings_use_case)
        self.settings_window.exec()

    def handle_capture_request(self):
        logger.info("Starting screen capture workflow...")
        try:
            result_text, double_check = self.capture_use_case.execute()
        except ValueError as e:
            logger.warning(f"Configuration error: {e}")
            self.open_settings()
            return

        if result_text is not None:
            if double_check:
                # Show Review Dialog
                self.review_window = ReviewWindow(result_text)
                if self.review_window.exec() == ReviewWindow.DialogCode.Accepted:
                    edited_text = self.review_window.get_text()
                    self.clipboard_svc.copy_text(edited_text)
            else:
                # Copying is done in Use Case directly.
                # Just show Toast
                self.toast = ToastNotification()
                self.toast.show_toast()

    def quit_app(self):
        logger.info("Quitting application.")
        self.app.quit()
