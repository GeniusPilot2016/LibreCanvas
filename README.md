# LibreCanvas

![image](https://github.com/user-attachments/assets/11df6401-335f-4add-b92c-40d80ec5798e)

[![GitHub stars](https://img.shields.io/github/stars/GeniusPilot2016/LibreCanvas?style=social)](https://github.com/GeniusPilot2016/LibreCanvas/stargazers)
[![License](https://img.shields.io/github/license/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas/blob/main/LICENSE)
[![Top language](https://img.shields.io/github/languages/top/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas)
[![AI Powered](https://img.shields.io/badge/AI-Powered-7C4DFF?style=flat&logo=artificialintelligence&logoColor=white)](https://github.com/GeniusPilot2016/LibreCanvas)
[![Last commit](https://img.shields.io/github/last-commit/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas/commits/master)
[![PRs](https://img.shields.io/github/issues-pr/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas/pulls)
[![GitHub forks](https://img.shields.io/github/forks/GeniusPilot2016/LibreCanvas?style=social)](https://github.com/GeniusPilot2016/LibreCanvas/network/members)
[![Alpha](https://img.shields.io/badge/status-alpha-orange?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas)

LibreCanvas is a creative image editor inspired by classic paint applications and Photoshop, with built-in **AI-powered image generation and editing features**.

After a period of reduced AI functionality caused by backend migration and API limitations, **AI features are functional again with the new backend integration**.

## Why "LibreCanvas"? (Rebranding from ArtFusion)

Previously, this project was named **ArtFusion**. However, a search for "ArtFusion" reveals numerous existing products, services, and digital platforms using the same name—especially apps, web tools, and design platforms.

This caused confusion, discovery issues, and brand ambiguity in communities and search engines. To avoid these conflicts and establish a more recognizable identity, the project was rebranded as **LibreCanvas**.

The new name reflects the project's open and creative spirit while giving the project a more distinctive identity.

## AI Features Are Back

LibreCanvas previously offered AI-powered image editing through the Gemini Nano Banana model. Those features were temporarily discontinued because of `RESOURCE_EXHAUSTED` errors, API restrictions, and the migration away from the original backend.

LibreCanvas has now moved to a new backend implementation, and **AI-powered image generation and editing are available again**.

Current AI capabilities include:

- **AI image generation:** Generate images directly from text prompts.
- **AI image-to-image editing:** Use an existing image as input and transform it using natural-language instructions.
- **AI-assisted image editing:** Perform creative edits and modifications directly inside LibreCanvas.
- **Background removal:** Isolate the main object or person and produce a transparent-background result.
- **Reference image support:** Existing images can be passed to the AI backend for image-guided editing.
- **Integrated editing workflow:** Generated results can be used directly in the LibreCanvas canvas and editing tools.

AI functionality depends on the availability and limitations of the currently configured backend service.

## Features

- **Creative painting and image manipulation tools:** Inspired by traditional paint applications and lightweight Photoshop workflows.
- **AI text-to-image generation:** Create new images from written descriptions.
- **AI image-to-image editing:** Modify an existing image using prompts.
- **AI-assisted background removal:** Isolate foreground subjects and create transparent images.
- **Selection-based AI editing:** Apply AI operations to selected regions without replacing the entire canvas.
- **Undo and editing workflow integration:** AI-generated modifications work alongside the editor's existing editing workflow.
- **Model-agnostic backend design:** LibreCanvas is designed so the AI backend can be replaced or expanded as new models and APIs become available.

## Status

LibreCanvas is fully usable as both a traditional creative image editor and an **AI-assisted image editing application**.

The AI functionality that was previously disabled during the backend migration is now operational again.

Because LibreCanvas uses an external AI backend, individual capabilities may still depend on backend availability, rate limits, supported image sizes, and model behavior.

## Background & Exhibition Story

LibreCanvas was originally developed as a project for a visual programming course and for a school's end-of-year exhibition before graduation.

However, displaying the project at the exhibition was prohibited by the instructor until the final exhibition grading process.

The project continued development afterward and eventually evolved beyond its original academic purpose into LibreCanvas.

## Backend and AI Development

LibreCanvas's AI architecture is intentionally designed to avoid being permanently tied to a single model or provider.

Previous implementations encountered limitations with services such as Gemini and Pollinations. The current implementation uses a different backend and restores the image generation and image editing functionality that had previously been unavailable.

Future integrations with other models or providers are still welcome, including:

- Stable Diffusion
- Hugging Face Diffusers
- Local image-generation models
- Free or open AI APIs
- Other image-to-image and inpainting models

This makes it possible for LibreCanvas to continue evolving even if a particular AI provider changes its API, limits, or availability.

## Usage

1. Clone the repository:

   ```bash
   git clone https://github.com/GeniusPilot2016/LibreCanvas.git
