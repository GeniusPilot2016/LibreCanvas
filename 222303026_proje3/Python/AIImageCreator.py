import os
from google import genai
from google.genai import types
from PIL import Image
from io import BytesIO
import base64

def create(inputText, apiKey):
    client = genai.Client(api_key=apiKey)

    contents = (inputText)

    response = client.models.generate_content(
        model="gemini-2.0-flash-preview-image-generation",
        contents=contents,
        config=types.GenerateContentConfig(
          response_modalities=['TEXT', 'IMAGE']
        )
    )

    for part in response.candidates[0].content.parts:
        if part.inline_data is not None:
            image = Image.open(BytesIO(part.inline_data.data))
            # Save image as bytes
            output = BytesIO()
            image.save(output, format="PNG")
            return output.getvalue()