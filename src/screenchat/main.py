import sys
import argparse
import pyperclip
from loguru import logger
from PyQt6.QtWidgets import QApplication, QMessageBox
from PyQt6.QtCore import QFile, QTextStream

from .core.config import ConfigManager
from .core.ipc import IPCServerThread, send_capture_signal
from .core.wayland import capture_screen_interactive
from .core.gemini import GeminiClient
from .ui.tray import TrayController
from .ui.settings import SettingsWindow
from .ui.review import ReviewWindow
from .ui.toast import ToastNotification

class ScreenChatApp:
    def __init__(self, app: QApplication):
        self.app = app
        self.config_manager = ConfigManager()
        
        # Load style.qss
        self.load_stylesheet()

        # Init UI
        self.tray = TrayController()
        self.tray.show_settings_signal.connect(self.open_settings)
        self.tray.quit_signal.connect(self.quit_app)
        self.tray.show()

        # If API Key is not set, open settings window
        if not self.config_manager.settings.api_key:
            self.open_settings()

        # Start IPC Server listening for background shortcut signals
        self.ipc_thread = IPCServerThread()
        self.ipc_thread.capture_requested.connect(self.handle_capture_request)
        self.ipc_thread.start()

    def load_stylesheet(self):
        try:
            import os
            qss_path = os.path.join(os.path.dirname(__file__), "ui", "style.qss")
            if os.path.exists(qss_path):
                with open(qss_path, "r", encoding="utf-8") as f:
                    self.app.setStyleSheet(f.read())
        except Exception as e:
            logger.error(f"Error loading QSS: {e}")

    def open_settings(self):
        self.settings_window = SettingsWindow(self.config_manager)
        self.settings_window.exec()

    def handle_capture_request(self):
        logger.info("Starting screen capture workflow...")
        if not self.config_manager.settings.api_key:
            logger.warning("API Key not configured. Skipping.")
            self.open_settings()
            return
            
        img_path = capture_screen_interactive()
        if img_path:
            # Initialize Gemini client
            gemini = GeminiClient(self.config_manager.settings.api_key)
            prompt = self.config_manager.get_selected_prompt()
            
            # TODO: Add loading indicator (flashing tray icon or loading popup)
            result_text = gemini.extract_text(img_path, prompt)
            
            if self.config_manager.settings.enable_double_check:
                # Show Review Dialog
                self.review_window = ReviewWindow(result_text)
                self.review_window.exec()
            else:
                # Copy directly to system Clipboard and show Toast
                pyperclip.copy(result_text)
                self.toast = ToastNotification()
                self.toast.show_toast()

    def quit_app(self):
        logger.info("Quitting application.")
        self.app.quit()


def run_daemon():
    logger.info("Starting ScreenChat Daemon...")
    app = QApplication(sys.argv)
    app.setQuitOnLastWindowClosed(False) # Run in background
    
    screenchat = ScreenChatApp(app)
    sys.exit(app.exec())

def run_capture():
    logger.info("Requesting screen capture from client...")
    send_capture_signal()

def main():
    parser = argparse.ArgumentParser(description="ScreenChat OCR for Fedora 44 Wayland")
    parser.add_argument("command", choices=["start", "capture"], help="Command to execute")
    
    # manual parse for arguments
    if len(sys.argv) < 2:
        parser.print_help()
        sys.exit(1)
        
    cmd = sys.argv[1]
    
    logger.add("logs/screenchat.log", rotation="10 MB")
    
    if cmd == "start":
        run_daemon()
    elif cmd == "capture":
        run_capture()
    else:
        parser.print_help()

if __name__ == "__main__":
    main()
