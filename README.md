# LibreCanvas

<img width="1227" height="433" alt="librecanvas_banner" src="https://github.com/user-attachments/assets/bda441aa-5536-4129-bce6-0c7261b658ca" />


> **LibreCanvas is an open-source C# image editor that combines traditional painting tools with AI-powered image generation and editing.**

![LibreCanvas](https://github.com/user-attachments/assets/5063a256-fabf-449e-a552-6228e3e6b95a)

[![GitHub stars](https://img.shields.io/github/stars/GeniusPilot2016/LibreCanvas?style=social)](https://github.com/GeniusPilot2016/LibreCanvas/stargazers)
[![License](https://img.shields.io/github/license/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas/blob/main/LICENSE)
[![Top language](https://img.shields.io/github/languages/top/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas)
[![AI Powered](https://img.shields.io/badge/AI-Powered-7C4DFF?style=flat&logo=artificialintelligence&logoColor=white)](https://github.com/GeniusPilot2016/LibreCanvas)
[![Last commit](https://img.shields.io/github/last-commit/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas/commits/master)
[![PRs](https://img.shields.io/github/issues-pr/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas/pulls)
[![GitHub forks](https://img.shields.io/github/forks/GeniusPilot2016/LibreCanvas?style=social)](https://github.com/GeniusPilot2016/LibreCanvas/network/members)
[![Alpha](https://img.shields.io/badge/status-alpha-orange?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas)

LibreCanvas is inspired by classic paint applications and Photoshop-like workflows. It is designed to let you create, edit, transform, and refine images without constantly switching between separate applications.

> **Development status:** LibreCanvas is currently in active development. The first public release will be published when the core editing workflow is nearly complete. Until then, features, interfaces, and AI integrations may change.

⭐ Star the repository to follow development and get notified when the first public release is available.

## Highlights

- Traditional painting and image-manipulation tools
- AI text-to-image generation
- AI image-to-image editing
- AI-assisted background removal
- Selection-based AI editing
- Reference-image support
- Undo and editing-workflow integration
- Replaceable, model-agnostic AI backend design
- Open-source development under the GNU GPL-3.0 license

## AI capabilities

LibreCanvas currently aims to support the following AI-assisted workflows:

- **Text-to-image generation** — Generate images from natural-language prompts.
- **Image-to-image editing** — Transform an existing image using instructions.
- **Selection-based editing** — Apply AI operations to selected regions instead of replacing the entire canvas.
- **Background removal** — Isolate a subject and create a transparent-background result.
- **Reference-image editing** — Use an existing image as guidance for an AI operation.
- **Integrated editing** — Continue editing generated results with LibreCanvas's normal canvas tools.

AI functionality depends on the configured backend service, its availability, rate limits, supported image sizes, and model behavior.

## Current status

LibreCanvas is intended to become a complete creative image editor with integrated AI features. The current development version is usable for experimentation, but it should not yet be considered production-ready.

The project is currently focused on completing and stabilizing the core workflow before the first public release.

### First-release goals

The first public release is planned once the following areas are sufficiently stable:

- Creating a new canvas and opening existing images
- Core painting and image-editing operations
- Reliable undo and redo behavior
- Image export and saving
- AI operations with useful error handling
- Initial user documentation
- A repeatable build and installation process

## Why "LibreCanvas"?

LibreCanvas was previously named **ArtFusion**. The name was changed because "ArtFusion" is used by multiple existing products and services, creating confusion in search results and online communities.

The name **LibreCanvas** reflects the project's open-source goals and its focus on giving creators a flexible digital canvas.

## Backend architecture

LibreCanvas is designed to avoid being permanently tied to a single AI model or provider. The backend can be replaced or expanded as new models, APIs, and local inference options become available.

Possible future integrations include:

- Stable Diffusion
- Hugging Face Diffusers
- Local image-generation models
- Open or self-hosted AI APIs
- Image-to-image and inpainting models

This approach helps the project adapt when a provider changes its API, availability, pricing, or usage limits.

## Getting started

The repository currently contains the latest development version rather than a finalized public release.

### Clone the repository

```bash
git clone https://github.com/GeniusPilot2016/LibreCanvas.git
cd LibreCanvas
```

### Build and run

The exact build and run instructions may evolve while the project is in development. Use the project files and the latest source configuration to determine the required .NET version and startup project.

Typical .NET commands are:

```bash
dotnet restore
dotnet build
dotnet run
```

> If the application requires an AI backend, configure the required service or credentials according to the project configuration before using AI features. Never commit private API keys to the repository.

## Development roadmap

Planned areas of development include:

- Stabilizing the core canvas workflow
- Improving performance and reliability
- Expanding painting and image-editing tools
- Improving AI error handling and feedback
- Adding more backend and model options
- Improving documentation and onboarding
- Preparing the first public release

Ideas, bug reports, and technical suggestions are welcome through the issue tracker.

## Contributing

Contributions, testing, feedback, and ideas are welcome as LibreCanvas approaches its first public release.

Before opening an issue, please check whether a similar issue already exists. When reporting a bug, include:

- Operating system
- .NET version
- Steps to reproduce the issue
- Expected and actual behavior
- Error messages or screenshots, when available
- Whether the issue affects normal editing or AI functionality

## License

LibreCanvas is distributed under the **GNU General Public License v3.0**. See [`LICENSE`](LICENSE) for the full license text.

## Project background

LibreCanvas began as a visual-programming course project intended for a school end-of-year exhibition. Development continued after the original academic project and gradually expanded into an open-source creative application with AI-assisted image-editing capabilities.

---

If you are interested in the project, consider starring the repository and following its development toward the first public release.
