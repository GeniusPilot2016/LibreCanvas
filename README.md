# LibreCanvas

LibreCanvas is a creative image editor inspired by paint applications and Photoshop—but **as of the most recent update, AI-powered editing features have been *removed* due to migration from the original backend and API limitations.**

## Why "LibreCanvas"? (Rebranding from ArtFusion)

Previously, this project was named **ArtFusion**. However, a search for "ArtFusion" reveals numerous existing products, services, and digital platforms that use the same name—especially apps, web tools, and design platforms. This led to frequent confusion and discovery issues, as well as brand ambiguity in communities and search engines. To avoid these conflicts and improve recognition, the project was rebranded to LibreCanvas. The new name reflects the project's open, creative spirit while ensuring unique identity.

## What Changed?

**LibreCanvas previously offered AI-powered image editing tools (like smart inpainting, enhancement, and paint workflows), using the Gemini Nano Banana model. Due to `RESOURCE_EXHAUSTED` errors and model/API limitations, these features have been discontinued.**

However, the following features are now **disabled or severely limited** because of Pollinations API restrictions:

- **No AI-based image editing tools:** Smart inpainting, creative paint tools, and AI-powered enhancements are currently unavailable.
- **No reference image upload:** Uploading and using images as references is no longer possible.
- **English-only prompting:** Pollinations works reliably only with English prompts. Non-English prompts may result in irrelevant or incorrect outputs. Multilingual prompting is not currently supported.

> **Note:** LibreCanvas still functions as an image editor, but advanced AI-assisted features are on hold.

## Features

- **Creative paint and manipulation tools:** Inspired by paint apps and lightweight Photoshop workflows.
- **Model-agnostic backend design:** While AI features are currently disabled, LibreCanvas's design allows for possible future addition of new AI models/APIs.

## Status

LibreCanvas is fully usable as a creative editor, but AI image editing features have been disabled due to current backend/API limitations. The search for a new AI-powered model is **not urgent, but just in case contributors wish to help**.

## Background & Exhibition Story

LibreCanvas was originally developed for project of visual programming lesson and end-of-year exhibition of school before graduation, but showing it was prohibited by instructor until grading final exhibition.

## Seeking Alternatives

If you know of a **free AI image editing model** (API or local)—for example, Stable Diffusion (local), Hugging Face Diffusers, or a free-tier API—**please suggest it!** Contributions and suggestions are welcome.

## Usage

1. Clone the repo:
   ```
   git clone https://github.com/GeniusPilot2016/LibreCanvas.git
   ```
2. Install requirements *(see project files)*.
3. Run the editor. AI features are currently disabled due to backend/API restrictions.

## Contributing

Suggestions for new AI model integrations and backend contributions are very welcome.  
Open an issue or pull request to help bring AI-based image editing back to LibreCanvas!
