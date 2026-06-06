from abc import ABC, abstractmethod
from .models import AppSettings

class ConfigRepository(ABC):
    @property
    @abstractmethod
    def settings(self) -> AppSettings:
        pass

    @settings.setter
    @abstractmethod
    def settings(self, val: AppSettings) -> None:
        pass

    @abstractmethod
    def load(self) -> AppSettings:
        pass

    @abstractmethod
    def save(self) -> None:
        pass

    @abstractmethod
    def get_selected_prompt(self) -> str:
        pass

class ScreenshotService(ABC):
    @abstractmethod
    def capture(self) -> str | None:
        pass

class OCRService(ABC):
    @abstractmethod
    def extract_text(self, image_path: str, prompt: str) -> str:
        pass

class ClipboardService(ABC):
    @abstractmethod
    def copy_text(self, text: str) -> None:
        pass
