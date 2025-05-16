from google import genai
from google.genai import types
from PIL import Image
from io import BytesIO

import PIL.Image

def edit(self, inputImage, inputText):
	image = inputImage

    client = genai.Client()

    text_input = (inputText,)

    response = client.models.generate_content(
        model="gemini-2.0-flash-preview-image-generation",
        contents=[text_input, image],
        config=types.GenerateContentConfig(
          response_modalities=['TEXT', 'IMAGE']
        )
    )

    for part in response.candidates[0].content.parts:
      if part.text is not None:
        print(part.text)
      elif part.inline_data is not None:
        image = Image.open(BytesIO(part.inline_data.data))
        return image