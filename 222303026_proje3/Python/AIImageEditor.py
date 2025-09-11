import os
import sys
import base64
from google import genai
from google.genai import types
from PIL import Image
from io import BytesIO
import json

import PIL.Image

def edit(inputImage, inputText, apiKey):
    try:
        image = PIL.Image.open(inputImage)

        client = genai.Client(api_key=apiKey)

        text_input = (inputText)

        response = client.models.generate_content(
            model="gemini-2.0-flash-preview-image-generation",
            #model="gemini-2.5-flash-image-preview",
            contents=[text_input, image],
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
    image = sys.argv[1]
    prompt = sys.argv[2]
    api_key = sys.argv[3]
    edit(image, prompt, api_key)