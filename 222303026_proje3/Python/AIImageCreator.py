import os
import sys
from google import genai
from google.genai import types
from PIL import Image
from io import BytesIO
import base64
import json

def create(inputText, apiKey, width, height, imagePath):
    try:
        client = genai.Client(api_key=apiKey)

        image = None
        if imagePath:
            image = Image.open(imagePath)

        text_input = inputText

        if image is not None:
            contents = [text_input, image]
        else:
            contents = [text_input]

        response = client.models.generate_content(
            model="gemini-2.0-flash-preview-image-generation",
            #model="gemini-2.5-flash-image-preview",
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
    except Exception as e:
        error_response = {
            "error_type": type(e).__name__,
            "error_message": str(e)
        }
        print(json.dumps(error_response), file=sys.stderr) #print to sterr
        sys.exit(1)

if __name__ == "__main__":
    prompt = sys.argv[1]
    api_key = sys.argv[2]
    width = int(sys.argv[3])
    height = int(sys.argv[4])
    imagePath = sys.argv[5] if len(sys.argv) > 5 else None
    create(prompt, api_key, width, height, imagePath)