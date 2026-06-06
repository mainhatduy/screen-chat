from typing import Tuple
from ..domain.interfaces import ConfigRepository, ScreenshotService, OCRService, ClipboardService
from ..domain.models import AppSettings

class CaptureAndProcessUseCase:
    def __init__(
        self, 
        config_repo: ConfigRepository, 
        screenshot_svc: ScreenshotService, 
        ocr_svc: OCRService, 
        clipboard_svc: ClipboardService
    ):
        self.config_repo = config_repo
        self.screenshot_svc = screenshot_svc
        self.ocr_svc = ocr_svc
        self.clipboard_svc = clipboard_svc

    def execute(self) -> Tuple[str | None, bool]:
        """
        Orchestrates the screenshot capture, OCR processing, and optional direct copying.
        Returns:
            Tuple[extracted_text, enable_double_check] or (None, False) if capture cancelled.
        Raises:
            ValueError if API Key is not configured.
        """
        settings = self.config_repo.settings
        if not settings.api_key:
            raise ValueError("API Key not configured")

        img_path = self.screenshot_svc.capture()
        if not img_path:
            return None, False

        prompt = self.config_repo.get_selected_prompt()
        result_text = self.ocr_svc.extract_text(img_path, prompt)

        if not settings.enable_double_check:
            self.clipboard_svc.copy_text(result_text)

        return result_text, settings.enable_double_check

class SettingsUseCase:
    def __init__(self, config_repo: ConfigRepository):
        self.config_repo = config_repo

    def get_settings(self) -> AppSettings:
        return self.config_repo.settings

    def save_settings(self, api_key: str, enable_double_check: bool, selected_prompt_name: str) -> None:
        settings = self.config_repo.settings
        settings.api_key = api_key
        settings.enable_double_check = enable_double_check
        settings.selected_prompt_name = selected_prompt_name
        self.config_repo.save()
