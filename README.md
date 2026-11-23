<img width="1227" height="433" alt="librecanvas_banner" src="https://github.com/user-attachments/assets/86d4cbb2-4d83-4769-925b-45a6aa96195b" />

# 🎨🖌️🖼️🤖 LibreCanvas: Traditional Image Editing Meets Modern AI

LibreCanvas is an open-source C# image editor that combines traditional painting and image-manipulation tools with modern AI-powered image generation and editing. Inspired by classic paint applications and Photoshop-like workflows, LibreCanvas aims to offer a familiar, flexible creative environment where users can draw, edit, transform, and refine images without constantly switching between separate applications. 🎨🖌️🤖

[![GitHub stars](https://img.shields.io/github/stars/GeniusPilot2016/LibreCanvas?style=social)](https://github.com/GeniusPilot2016/LibreCanvas/stargazers)
[![Downloads](https://img.shields.io/github/downloads/GeniusPilot2016/LibreCanvas/total?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas/releases)
[![License](https://img.shields.io/github/license/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas/blob/main/LICENSE)
[![Top language](https://img.shields.io/github/languages/top/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas)
[![AI Powered](https://img.shields.io/badge/AI-Powered-7C4DFF?style=flat&logo=artificialintelligence&logoColor=white)](https://github.com/GeniusPilot2016/LibreCanvas)
[![Last commit](https://img.shields.io/github/last-commit/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas/commits/master)
[![Issues](https://img.shields.io/github/issues-raw/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas/issues)
[![PRs](https://img.shields.io/github/issues-pr/GeniusPilot2016/LibreCanvas?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas/pulls)
[![GitHub forks](https://img.shields.io/github/forks/GeniusPilot2016/LibreCanvas?style=social)](https://github.com/GeniusPilot2016/LibreCanvas/network/members)
[![Alpha](https://img.shields.io/badge/status-alpha-orange?style=flat-square)](https://github.com/GeniusPilot2016/LibreCanvas)

![LibreCanvas](https://github.com/user-attachments/assets/c9193b40-db85-45fa-aaf1-e97538798c2b)

## 🚧 Status

LibreCanvas is **still in active development** and has **not yet reached its stable release**. The current alpha version is available for experimentation, but it should not be considered production-ready. Features, interfaces, and AI integrations may change as development progresses.

The first stable release is planned for when the core editing workflow is sufficiently complete and stable. Feedback, bug reports, and suggestions are welcome in the meantime.

## ✨ Features

- ### 🖥️ Familiar and flexible image-editing environment
  LibreCanvas is inspired by traditional paint applications and Photoshop-like editing workflows. Its goal is to bring familiar creative tools and AI-assisted operations together in one application, making it easier to move between drawing, editing, and image refinement.

  <img width="1920" height="1140" alt="image" src="https://github.com/user-attachments/assets/8716121e-e425-4e9b-bbc4-fb72d4c365ce" />

- ### 🖌️ Traditional painting and editing tools
  Create and modify images using conventional painting and image-manipulation operations. LibreCanvas is being developed to support the essential tools needed for everyday image creation and editing.

  <img width="298" height="702" alt="image" src="https://github.com/user-attachments/assets/8ea0d991-45a7-49f9-9d9b-384c6e54a024" />

- ### 🖼️ Canvas and image management
  The core workflow is being built around creating new canvases, opening existing images, and saving or exporting completed work. Reliable undo and redo behavior is also an important first-release goal.
  <img width="1920" height="1140" alt="image" src="https://github.com/user-attachments/assets/651f7501-7cec-4083-9824-3a4429ded5e1" />

- ### 🚀 More Than a Traditional Image Editor
  LibreCanvas is designed to go beyond conventional image editing by integrating AI-powered creative workflows directly into the editor.

  - #### 🤖 Turn text into images
    LibreCanvas aims to support **text-to-image generation**, allowing users to create images from natural-language prompts and continue working with the results in the editor.
    <img width="1920" height="1140" alt="image" src="https://github.com/user-attachments/assets/c16aae46-cf66-4674-b0eb-e2fa144e3688" />

  - #### 🎨 Edit existing images with AI
    The planned **image-to-image editing** workflow lets users provide an existing image and instructions for transforming or refining it.
    <img width="1920" height="1140" alt="image" src="https://github.com/user-attachments/assets/2cb3cf82-b87c-4df5-9807-15975c613a55" />
    
  - #### ✂️ Edit selected regions
    **Selection-based AI editing** is intended to apply changes to particular areas of an image rather than replacing the entire canvas.
    <img width="1920" height="1140" alt="image" src="https://github.com/user-attachments/assets/c5990e24-7b48-4f0a-b660-eb69741ec339" />

  - #### 🪄 Remove backgrounds with AI
    **AI-assisted background removal** is intended to isolate subjects and produce results with transparent backgrounds.
    <img width="1920" height="1140" alt="image" src="https://github.com/user-attachments/assets/9c3bc1b2-c338-4e44-899c-029592d1fac9" />

  - #### 🖼️ Use reference images
    **Reference-image support** is intended to let users guide AI operations with an existing image.
    <img width="1920" height="1140" alt="image" src="https://github.com/user-attachments/assets/40e15f8b-f9e3-4f1b-9fc2-5a6e011dd8bc" />

  - #### 🔙 Continue editing AI-generated results
    LibreCanvas aims to integrate AI results into its ordinary editing workflow, including normal canvas operations and undo behavior, rather than treating AI as a separate tool.
    <img width="1920" height="1140" alt="image" src="https://github.com/user-attachments/assets/dfffacb8-e595-4e33-a9f0-5780d9796131" />

**Note:** AI functionality and output depend on its availability, rate limits, supported image sizes, and model behavior. The capabilities above describe development targets and may not all be complete in the current version.

## 🧠 AI Backend Architecture

LibreCanvas is intended to support different AI services and models as the ecosystem evolves. Its backend design aims to make integrations replaceable or expandable without requiring the editor to depend permanently on a single provider.

Possible future integrations include:

- Stable Diffusion
- Hugging Face Diffusers
- Local image-generation models
- Open or self-hosted AI APIs
- Image-to-image and inpainting models

This approach is intended to make LibreCanvas more adaptable when providers change their APIs, availability, pricing, or usage limits. **These are possible future integrations, not a list of confirmed supported backends.**

## 🖥️📃 System Requirements

**Windows Version:** Windows 10 (1809 and above) 64-bit/ARM64 (minimum)/Windows 11 (recommended)

**CPU:** Intel® Core i3 or equivalent (64-bit), or ARM64 equivalent (e.g., Qualcomm Snapdragon) (minimum)

**RAM:** 2 GB (minimum), 4 GB (recommended)

**Storage:** 250 MB (minimum)

## ▶️ Running the Released Executable

You can run LibreCanvas easily without compiling by using the pre-built executable available in the [Releases](https://github.com/GeniusPilot2016/LibreCanvas/releases) section.

### ⚙️ Execution Instructions

1. **Go to the Releases Page**
   - Visit the [Releases](https://github.com/GeniusPilot2016/LibreCanvas/releases) page on GitHub.

2. **Download the Zip File**
   - Find the latest release, then download the zip file (for example: `librecanvas-0.23.0-alpha-x64.zip` or similar, depending on your system).

3. **Extract the Zip File**
   - Right-click the downloaded zip file and choose "Extract All..." (on Windows), or use your preferred zip extractor.

4. **Run the Executable**
   - Open the extracted folder.
   - Go to the `LibreCanvas` folder
   - Find and double-click `LibreCanvas.exe` to launch the program.

5. **(Optional) Allow Security Prompts**
   - If Windows identifies the program as coming from the Internet, you may need to accept a security prompt or click "More info" and then "Run anyway".

**No installation is required.**  
If you encounter issues running LibreCanvas, make sure your system meets the requirements listed above.

## 🌱 Cloning

1. Open Visual Studio or another Git-compatible development environment.

2. Select **Clone a repository**, or open a terminal.

3. Enter the following repository URL or run:

   ```bash
   git clone https://github.com/GeniusPilot2016/LibreCanvas.git
   cd LibreCanvas
   ```

4. Open the project or solution file and follow the source configuration to select the correct startup project and .NET SDK.

5. Build and run the development version. If you plan to contribute, test your changes before opening a pull request.

## 🗺️ Development Roadmap

The main priorities before the first public release are:

- Creating new canvases and opening existing images reliably
- Completing essential painting and image-editing operations
- Stabilizing undo and redo behavior
- Improving image export and saving
- Adding useful AI error handling and feedback
- Improving performance and reliability
- Expanding AI backend and model options
- Preparing initial documentation and a repeatable build process
- Publishing the first stable public release

These are development goals, not guarantees that every feature is already implemented.

## 🤝 Contributing

Contributions, testing, bug reports, and technical suggestions are welcome as LibreCanvas progresses toward its first public release.

Before opening an issue, please check whether a similar issue has already been reported. When reporting a bug, include:

- Operating system
- .NET version
- Steps to reproduce the problem
- Expected and actual behavior
- Error messages or screenshots, if available
- Whether the problem affects ordinary editing or AI functionality

You can report issues through the [GitHub issue tracker](https://github.com/GeniusPilot2016/LibreCanvas/issues).

## 🏷️ Why "LibreCanvas"?

LibreCanvas was previously known as **ArtFusion**. The project was renamed because “ArtFusion” was already used by multiple existing products and services, making it difficult to distinguish the project in searches and online communities.

The name **LibreCanvas** reflects the project's open-source philosophy and its goal of providing creators with a flexible digital canvas.

## 📜 License

LibreCanvas is open-source software distributed under the **GNU General Public License v3.0**. See [`LICENSE`](LICENSE) for the complete license text.

## ⭐️ Support LibreCanvas!

If you're interested in the project, please consider [leaving a star ⭐️ on GitHub](https://github.com/GeniusPilot2016/LibreCanvas) to help others discover it and follow its progress toward the first public release!

## 🌟 From a School Project to an Open-Source Editor

LibreCanvas began as a visual-programming course project created for a school end-of-year exhibition. Development continued beyond the original academic project, gradually expanding its scope into an open-source image editor with integrated AI-assisted creative capabilities.

The project is continuing to evolve, with its focus on making traditional image editing and modern AI workflows part of the same creative experience.
