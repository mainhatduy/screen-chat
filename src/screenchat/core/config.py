import os
import json
from pydantic import BaseModel, Field
from typing import List
from loguru import logger

class CustomPrompt(BaseModel):
    name: str
    text: str

class AppSettings(BaseModel):
    api_key: str = ""
    enable_double_check: bool = True
    custom_prompts: List[CustomPrompt] = [
        CustomPrompt(
            name="Default OCR", 
            text="Extract all text from the image exactly as it appears. Return ONLY the raw text without any markdown formatting, backticks, or commentary."
        ),
        CustomPrompt(
            name="Translate to English", 
            text="Extract all text from the image and translate it to English. Return ONLY the translated text."
        )
    ]
    selected_prompt_name: str = "Default OCR"

class ConfigManager:
    def __init__(self, config_file: str = "settings.json"):
        # System config directory for safety
        self.config_dir = os.path.expanduser("~/.config/screenchat")
        os.makedirs(self.config_dir, exist_ok=True)
        self.config_path = os.path.join(self.config_dir, config_file)
        self.settings = self.load()

    def load(self) -> AppSettings:
        if os.path.exists(self.config_path):
            try:
                with open(self.config_path, "r", encoding="utf-8") as f:
                    data = json.load(f)
                    return AppSettings(**data)
            except Exception as e:
                logger.error(f"Error parsing config file: {e}. Using defaults.")
        return AppSettings()

    def save(self):
        try:
            with open(self.config_path, "w", encoding="utf-8") as f:
                f.write(self.settings.model_dump_json(indent=4))
            logger.info("Saved new configuration.")
        except Exception as e:
            logger.error(f"Error saving configuration: {e}")
            
    def get_selected_prompt(self) -> str:
        for p in self.settings.custom_prompts:
            if p.name == self.settings.selected_prompt_name:
                return p.text
        return self.settings.custom_prompts[0].text
