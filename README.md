# LibreCanvas

LibreCanvas is a creative image editor inspired by paint applications and Photoshop—but **as of the most recent update, AI-powered editing features have been *removed* due to migration from the original backend to Pollinations.**

## What Changed?

**LibreCanvas previously offered AI-powered image editing tools (like smart inpainting, enhancement, and paint workflows), using the Gemini Nano Banana model. Due to `RESOURCE_EXHAUSTED` errors and model deprecations, I migrated the AI backend to [Pollinations](https://pollinations.ai/).**

However, the following features are now **disabled or severely limited** because of Pollinations API restrictions:

- **No AI-based image editing tools:** Smart inpainting, creative paint tools, and AI-powered enhancements are currently unavailable.
- **No reference image upload:** Uploading and using images as references is no longer possible.
- **English-only prompting:** Pollinations works reliably only with English prompts. Non-English prompts may result in irrelevant or incorrect outputs. Multilingual prompting is not currently supported and may return in the future, but no timeline is known.

> **Note:** LibreCanvas still functions as an image editor, but advanced AI-assisted features are on hold.

## Features

- **Creative paint and manipulation tools:** Inspired by paint apps and lightweight Photoshop workflows.
- **Model-agnostic backend design:** While AI features are currently disabled, LibreCanvas's design allows for possible future addition of new AI models/APIs.

## Status

LibreCanvas is fully usable as a creative editor, but AI image editing features have been disabled due to current backend/API limitations. The search for a new AI-powered model is **not urgent, but just limited** by what's available for free and supported by Pollinations or similar services. If a suitable and freely-usable AI backend becomes available, AI features may be restored.

## Background & Exhibition Story

LibreCanvas was originally developed for project of visual programming lesson and end-of-year exhibition of school before graduation, but showing it was prohibited by instructor until grading final exams. Despite four months of intensive work and multiple backend changes, most AI features were recently removed due to API instability. The core, paint-focused editing environment is still maintained.

## Seeking Alternatives

If you know of a **free AI image editing model** (API or local)—for example, Stable Diffusion (local), Hugging Face Diffusers, or a free-tier API—**please suggest it!** Contributions and suggestions are welcome, but the search is not currently urgent; rather, it's just a matter of working with available options and their limitations.

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
