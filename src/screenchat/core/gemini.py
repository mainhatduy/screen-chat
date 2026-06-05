import os
from google import genai
from google.genai import types
from loguru import logger
import json

class GeminiClient:
    def __init__(self, api_key: str):
        self.api_key = api_key
        # Initialize new client according to google-genai
        self.client = genai.Client(api_key=self.api_key)
        self.model = "gemini-3.1-flash-lite"

    def extract_text(self, image_path: str, prompt: str) -> str:
        """
        Sends image and prompt to Gemini API to get OCR results.
        """
        logger.info(f"Sending request to Gemini API for image: {image_path} using model {self.model}")
        
        try:
            import mimetypes
            mime_type, _ = mimetypes.guess_type(image_path)
            if not mime_type:
                mime_type = "image/png"
                
            with open(image_path, "rb") as f:
                img_bytes = f.read()
            
            # Initialize contents
            contents = [
                types.Content(
                    role="user",
                    parts=[
                        types.Part.from_text(text=prompt),
                        types.Part.from_bytes(
                            data=img_bytes,
                            mime_type=mime_type
                        ),
                    ],
                ),
            ]
            
            # Configure options to get precise results
            generate_content_config = types.GenerateContentConfig(
                temperature=0.1,  # High accuracy, low creativity
                thinking_config=types.ThinkingConfig(
                    thinking_level="MINIMAL",
                ),
                response_mime_type="application/json",
                response_schema=types.Schema(
                    type=types.Type.OBJECT,
                    required=["output_text"],
                    properties={
                        "output_text": types.Schema(
                            type=types.Type.STRING,
                        ),
                    },
                ),
            )
            
            response = self.client.models.generate_content(
                model=self.model,
                contents=contents,
                config=generate_content_config,
            )
            
            # Parse returned JSON result
            result_json = json.loads(response.text)
            return result_json.get("output_text", "")
            
        except Exception as e:
            logger.error(f"Error calling Gemini API: {e}")
            return f"API Error: {e}"

