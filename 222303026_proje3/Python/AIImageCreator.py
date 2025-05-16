import os
import sys
from google import genai
from google.genai import types
from PIL import Image
from io import BytesIO
import base64

def create(inputText, apiKey, width, height):
    client = genai.Client(api_key=apiKey)

    contents = (inputText)

    response = client.models.generate_content(
        model="gemini-2.0-flash-preview-image-generation",
        contents=contents,
        config=types.GenerateContentConfig(
          response_modalities=['TEXT', 'IMAGE']
        )
    )
    aspect_ratio = width / height

    for part in response.candidates[0].content.parts:
        if part.inline_data is not None:
            image = Image.open(BytesIO(part.inline_data.data))
            # Save image as bytes
            output = BytesIO()
            image.save(output, format="PNG")
            img_bytes = output.getvalue()
            # Print as base64 to stdout
            print(base64.b64encode(img_bytes).decode())
            return

if __name__ == "__main__":
    prompt = sys.argv[1]
    api_key = sys.argv[2]
    width = int(sys.argv[3])
    height = int(sys.argv[4])
    create(prompt, api_key, width, height)