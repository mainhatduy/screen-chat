import os
import json
from loguru import logger

from ..domain.models import AppSettings
from ..domain.interfaces import ConfigRepository

class JsonConfigRepository(ConfigRepository):
    def __init__(self, config_file: str = "settings.json"):
        # System config directory for safety
        self.config_dir = os.path.expanduser("~/.config/screenchat")
        os.makedirs(self.config_dir, exist_ok=True)
        self.config_path = os.path.join(self.config_dir, config_file)
        self._settings = self.load()

    @property
    def settings(self) -> AppSettings:
        return self._settings

    @settings.setter
    def settings(self, val: AppSettings) -> None:
        self._settings = val

    def load(self) -> AppSettings:
        if os.path.exists(self.config_path):
            try:
                with open(self.config_path, "r", encoding="utf-8") as f:
                    data = json.load(f)
                    return AppSettings(**data)
            except Exception as e:
                logger.error(f"Error parsing config file: {e}. Using defaults.")
        return AppSettings()

    def save(self) -> None:
        try:
            with open(self.config_path, "w", encoding="utf-8") as f:
                f.write(self._settings.model_dump_json(indent=4))
            logger.info("Saved new configuration.")
        except Exception as e:
            logger.error(f"Error saving configuration: {e}")
            
    def get_selected_prompt(self) -> str:
        for p in self._settings.custom_prompts:
            if p.name == self._settings.selected_prompt_name:
                return p.text
        return self._settings.custom_prompts[0].text
