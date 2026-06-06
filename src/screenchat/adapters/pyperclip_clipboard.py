import pyperclip
from ..domain.interfaces import ClipboardService

class PyperclipClipboardService(ClipboardService):
    def copy_text(self, text: str) -> None:
        pyperclip.copy(text)
