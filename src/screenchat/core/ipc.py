import os
import socket
from loguru import logger
from PyQt6.QtCore import QThread, pyqtSignal

SOCKET_PATH = "/tmp/screenchat_ipc.sock"

class IPCServerThread(QThread):
    """
    Runs in background in the Daemon to listen for signals from the CLI command.
    Emits a signal to the Main GUI Thread for processing.
    """
    capture_requested = pyqtSignal()

    def run(self):
        if os.path.exists(SOCKET_PATH):
            os.remove(SOCKET_PATH)
            
        server = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        server.bind(SOCKET_PATH)
        server.listen(1)
        logger.info(f"IPC Server listening at {SOCKET_PATH}")
        
        while True:
            try:
                conn, addr = server.accept()
                data = conn.recv(1024).decode('utf-8')
                if data.strip() == "CAPTURE":
                    logger.info("Received CAPTURE signal via IPC.")
                    self.capture_requested.emit()
                conn.close()
            except Exception as e:
                logger.error(f"IPC Server error: {e}")

def send_capture_signal():
    """
    Called from `screenchat capture` command to send a signal to the Daemon.
    """
    try:
        client = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        client.connect(SOCKET_PATH)
        client.sendall(b"CAPTURE")
        client.close()
        logger.info("Successfully sent screen capture command (CAPTURE) to Daemon.")
    except Exception as e:
        logger.error(f"Cannot connect to daemon. Is `screenchat start` running? Error: {e}")
