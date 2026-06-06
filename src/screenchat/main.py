import sys
import argparse
from loguru import logger
from PyQt6.QtWidgets import QApplication

# Application Use Cases
from .application.use_cases import CaptureAndProcessUseCase, SettingsUseCase

# Adapters
from .adapters.json_config import JsonConfigRepository
from .adapters.gemini_ocr import GeminiOCRService
from .adapters.wayland_screenshot import WaylandScreenshotService
from .adapters.pyperclip_clipboard import PyperclipClipboardService
from .adapters.ipc_server import IPCServerThread, send_capture_signal

# UI Controller
from .ui.controller import ScreenChatApp

def run_daemon():
    import os
    from PyQt6.QtGui import QIcon
    logger.info("Starting ScreenChat Daemon...")
    app = QApplication(sys.argv)
    app.setStyle("Fusion")
    app.setQuitOnLastWindowClosed(False) # Run in background
    
    # Set global window icon
    logo_path = os.path.abspath(os.path.join(os.path.dirname(__file__), "resources", "logo.svg"))
    if os.path.exists(logo_path):
        app.setWindowIcon(QIcon(logo_path))
        logger.info(f"Loaded application logo from {logo_path}")
    else:
        logger.warning(f"Logo not found at {logo_path}")
    
    # 1. Instantiate Adapters
    config_repo = JsonConfigRepository()
    screenshot_svc = WaylandScreenshotService()
    ocr_svc = GeminiOCRService(api_key_provider=lambda: config_repo.settings.api_key)
    clipboard_svc = PyperclipClipboardService()
    ipc_thread = IPCServerThread()
    
    # 2. Instantiate Use Cases
    capture_use_case = CaptureAndProcessUseCase(
        config_repo=config_repo,
        screenshot_svc=screenshot_svc,
        ocr_svc=ocr_svc,
        clipboard_svc=clipboard_svc
    )
    settings_use_case = SettingsUseCase(config_repo=config_repo)
    
    # 3. Instantiate UI Controller & Inject Dependencies
    screenchat = ScreenChatApp(
        app=app,
        capture_use_case=capture_use_case,
        settings_use_case=settings_use_case,
        clipboard_svc=clipboard_svc,
        ipc_thread=ipc_thread
    )
    
    sys.exit(app.exec())

def run_capture():
    logger.info("Requesting screen capture from client...")
    send_capture_signal()

def main():
    parser = argparse.ArgumentParser(description="ScreenChat OCR for Fedora 44 Wayland")
    parser.add_argument("command", nargs="?", choices=["start", "capture"], default="start", help="Command to execute")
    
    args = parser.parse_args()
    cmd = args.command
    
    import os
    log_dir = os.path.expanduser("~/.config/screenchat")
    os.makedirs(log_dir, exist_ok=True)
    log_file = os.path.join(log_dir, "screenchat.log")
    logger.add(log_file, rotation="10 MB")
    
    if cmd == "start":
        run_daemon()
    elif cmd == "capture":
        run_capture()
    else:
        parser.print_help()

if __name__ == "__main__":
    main()
