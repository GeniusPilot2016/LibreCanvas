# ArtFusion

ArtFusion is an AI-powered image editor combining paint-style creativity and Photoshop-inspired tools. It enables intuitive, intelligent image editing, with features like smart inpainting and automated enhancements powered by state-of-the-art AI.

> **Note:** ArtFusion currently uses the Gemini Nano Banana model for its AI features. The project is limited as the free API tier often returns `RESOURCE_EXHAUSTED` and older models are deprecated.
>
> Layers and transformations are not included; the focus is on paint and AI-assisted editing.

## Features

- **AI-powered image editing:** Smart inpainting, enhancement, and creative paint tools.
- **Paint/Photoshop blend workflow:** Designed for easy creative manipulation with AI assistance.
- **Model-agnostic AI backend:** The AI model can be replaced with any suitable image model or API when available.

## Status

ArtFusion is still functional, but the AI features may be unavailable due to frequent `RESOURCE_EXHAUSTED` errors with the current free API tier. The project is seeking free and modern AI models (API or local) for continued development.

## Background & Exhibition Story

ArtFusion was originally created for exhibition at the school's end-of-year showcase. However, exhibition was prohibited by the instructor until final exams were graded. After spending four months developing and adding new features, this setback was deeply disappointing and led to the project being nearly abandoned.

## Searching for a Free AI Model

If you know of a free AI image editing model (API or local)—for example, Stable Diffusion (local), Hugging Face Diffusers, or free-tier APIs—please suggest it! Contributors can help revive ArtFusion by integrating a suitable model.

## Usage

1. Clone the repo:
   ```
   git clone https://github.com/GeniusPilot2016/ArtFusion.git
   ```
2. Install requirements *(see project files)*.
3. Run the editor; AI features may be temporarily unavailable if API resources are exhausted.

## Contributing

Suggestions for free AI models and contributions to backend integration are welcome.  
Open an issue or pull request to help improve ArtFusion!
