from PyQt6.QtDBus import QDBusConnection, QDBusMessage
from PyQt6.QtCore import QEventLoop, QUrl, QObject, pyqtSlot
from loguru import logger

from ..domain.interfaces import ScreenshotService

class _PortalListener(QObject):
    def __init__(self, loop):
        super().__init__()
        self.loop = loop
        self.result_uri = None

    @pyqtSlot(QDBusMessage)
    def on_response(self, msg):
        args = msg.arguments()
        if len(args) >= 2:
            response_code = args[0]
            results = args[1]
            if response_code == 0 and isinstance(results, dict):
                self.result_uri = results.get("uri")
        self.loop.quit()

class WaylandScreenshotService(ScreenshotService):
    def __init__(self, output_path: str = "/tmp/screenchat_capture.png"):
        self.output_path = output_path

    def capture(self) -> str | None:
        """
        Triggers the Wayland default screen capture interface via XDG Desktop Portal.
        Returns the file path or None if cancelled by the user.
        """
        logger.info("Requesting screen capture via XDG Desktop Portal...")
        
        bus = QDBusConnection.sessionBus()
        if not bus.isConnected():
            logger.error("Cannot connect to Session DBus.")
            return None

        msg = QDBusMessage.createMethodCall(
            "org.freedesktop.portal.Desktop",
            "/org/freedesktop/portal/desktop",
            "org.freedesktop.portal.Screenshot",
            "Screenshot"
        )
        msg << ""  # parent_window
        msg << {"interactive": True} # options
        
        reply = bus.call(msg)
        if reply.type() == QDBusMessage.MessageType.ErrorMessage:
            logger.error(f"Portal call error: {reply.errorMessage()}")
            return None
            
        request_path = reply.arguments()[0]
        
        loop = QEventLoop()
        listener = _PortalListener(loop)
        
        bus.connect(
            "org.freedesktop.portal.Desktop", 
            request_path, 
            "org.freedesktop.portal.Request", 
            "Response", 
            listener.on_response
        )
        
        logger.info("Waiting for user to capture screen...")
        loop.exec()
        
        if listener.result_uri:
            real_path = QUrl(listener.result_uri).toLocalFile()
            logger.info(f"Screenshot captured successfully! Saved at: {real_path}")
            return real_path
        
        logger.info("User cancelled screen capture.")
        return None
