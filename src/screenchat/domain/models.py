from pydantic import BaseModel
from typing import List

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
