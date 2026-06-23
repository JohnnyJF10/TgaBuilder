# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2.2.7] - Latest

### Added

* **Project Export Formats:** Added write support for **KRITA (`.kra`)** and **PSD (`.psd`)** output files.
* **Transition Export:** Transition results can now be exported as layered **KRITA** and **PSD** project files for further editing outside the app.
* **Save As Format Picker:** Added a new **Save As** dropdown flow with improved output format selection per frontend.
* **Color Override Helper:** New controls can remove colour from the current texture and transfer colour/tone from a second texture.
* **Texture Retrofier:** Added retro colour-space limiting controls with optional palette reduction and dithering modes such as **Checkerboard**, **Bayer 4×4**, and **Bayer 8×8**.
* **Manual Brick Editing:** The manual transition mode can now move and freely rotate a single tile.

### Changed

* **Project Rename:** Application and repository renamed from **TgaBuilder** to **TrLynx**.
* **Transition Workflow:** Layered transition export makes it easier to continue work in tools such as **KRITA** and **PSD** when TrLynx reaches its built-in editing limits.

### Performance

* Significant performance improvements for transition calculations on large textures due to algorithm and buffer-management optimizations.

### Fixed

* Fixed oversized **PNG** and **BMP** save output on Linux.
* Fixed a Linux bug affecting file-type directory reading.

## [2.2.6] - 2026-05-27

### Added

* **New Modifications Window:** Includes basic and color adjustments (Exposure, Contrast, Saturation, etc.) and a Color Overlay feature with advanced mix modes (incl. OKLab Chroma).
* **Transition Helper Improvements:** Unified workspace for Smooth and Brick transitions, and an "Apply" button for instant previews.
* **Brick Analysis & Segmentation:** Added new methods (Felzenszwalb, SLIC, Quickshift, Grid Fit) with dedicated tuning sliders. Added a Gaussian Filter for pre-processing blur.
* **New Brick Workflow Tools:** Added Shadow Expander, Underfilling Expander, and a Manual Editing Pen (Pen, Eraser, Reset).
* **Main Window Enhancements:** Hold `Space` for free selection (grid-independent).
* **Avalonia UI:** Added Copy/Paste (`Ctrl+C` / `Ctrl+V`) support.
* New, redesigned homepage and comprehensive documentation set.

### Changed

* **Release Build Lineup:** Combined release now ships three packages: Standard WPF (.NET 6), Preview Avalonia Windows (self-contained), and Preview Avalonia Linux (self-contained).
* Updated existing segmentation methods: Added Marker Count slider to Watershed and Angle adjustment to Brick Fit.

### Removed

* Dropped separate WPF .NET 8 and non-self-contained Avalonia builds.

## [2.2.5] - 2026-05-12

### Added

* New controls to fine-tune transition topology: *Widening* (broadens transition front) and *Shift* (moves transition pivot left/right).
* New brick edge tinting and sizing controls: Edge Width, Edge Tint Color, and various blend/application modes (Multiply, Screen, Additive, Overlay, etc.).

### Changed

* Edge protection can now be explicitly turned off for workflows where strict border preservation isn't needed.
* Various code refactoring for the transitions module.

### Performance

* Significant performance improvements for transition window calculations (slider updates trigger faster redraws with minimal latency).

### Fixed

* Resolved an issue where info texts disappeared unexpectedly.

## [2.2.4] - 2026-05-05

### Added

* Color pickers added for various tasks (demolition border color, transparency replacement, selection fill).
* Added a third slider to the smooth transition window to adjust the offset property.

### Changed

* Fully integrated `bzPSD` into the `TgaBuilderLib` assembly instead of using NuGet to avoid legacy framework dependency issues.
* Integrated WPF ColorPicker as a separate assembly for customisations.
* Moved Avalonia UI ColorPicker installation to a NuGet package.

## [2.2.3] - 2026-04-26

### Added

* Selectable pre-algorithm input filters for the brick transition helper (Box Blur, Bilateral, Median, or None).
* Selectable segmentation algorithms for the brick transition helper (Watershed or XY Projection).

### Changed

* Multiple improvements to the Avalonia UI version.

### Performance

* Performance enhancements and refactoring on the brick transition pipeline (faster on weaker devices, especially with "Slice Corners" toggled).

### Fixed

* Minor bug fixes and corrections.

## [2.2.2] - 2026-04-21

### Fixed

* Fixed a crash issue in the brick transition helper window.

## [2.2.1] - 2026-04-20

### Changed

* Massive enhancements to the Avalonia UI version.

### Fixed

* Fixed issues on transition windows across all versions.

## [2.2.0] - 2026-04-17

### Added

* **New Transition Helper Windows:** Generate blend-ready transition tiles directly from selections. Includes a Smooth Transition Helper and a Brick Transition Helper.

## [2.1.4] - 2025-10-18

### Added

* **AvaloniaUi Pre-release:** Introduced an experimental cross-platform version based on Avalonia UI (tested on Linux Mint and Windows).

### Changed

* Improvements to `TgaBuilderLib` to enhance performance and UI layer independence.

### Fixed

* Corrected an incorrect library version number.
* Minor bug fixes.

## [2.1.3] - 2025-09-06

### Changed

* Various code refactorings.

### Fixed

* Fixed a bug occurring when clicking on a color picked by the eyedropper in the Format tab.

## [2.1.2]

### Added

* New shortcuts: `Middle Mouse Click` (move to Placing Mode), `Shift + LMB` (continuous placing), `Alt + LMB` (Placing and Swapping).
* Added a new button on the Placing Tab to enable Continuously Placing mode.

### Changed

* **Architectural Changes:** Removed all WPF parts from the `TgaBuilderLib` assembly to ensure cross-platform capabilities.
* Renamed "Offset" tab to "Grid" tab.

## [2.1.1] - 2025-08-11

### Added

* Checker box background for transparent areas in the Batch Loader Window.
* Added a theme switch button to the bottom right corner of the main window.

## [2.1.0] - 2025-08-08

### Added

* New shortcuts for Picker size modifications (`Shift + Mouse wheel`).
* Permanent visibility for picker size boxes and zoom sliders below panels.
* New Opacity slider on the Placing tab.
* Introduced version splits (.NET 6 and .NET 8 available for compatibility).

### Changed

* Format tab is now available on the source panel and merged with the alpha tab.
* Alpha settings available on the target panel and merged with format tab controls.
* Offset and Grid tabs have been merged.
* Various code refactorings.

### Fixed

* Fixed a crash issue during panel resizing.
* Fixed issues related to panel resizing and redo operations.

## [2.0.2] - 2025-08-03

### Added

* Checkerboard background representing transparent areas in 32-bpp panels.
* Zoom slider added to the View tabs.

### Fixed

* UI Titlebar fixes.
* Fixed an issue when importing DXTRE3D build levels.
* Improved tooltips and applied other minor fixes.

## [2.0.1] - 2025-07-29

### Added

* Reload button on the 'Source Open' panel to easily reload current files.
* Previous/Next buttons on the 'Source Open' panel for folder file walking.
* New releases are now based on verified commits.

## [2.0.0] - 2025-07-28

### Added

* **32-bit Support:** Loading, modification, and writing of 32-bit texture panels.
* New 'Format' tab added to destination tabs to switch between 24-bit and 32-bit.
* **Asynchronous Processing:** Asynchronous file reading/writing to prevent UI freezes. Includes a cancel operation.
* Workflow introduction for GitHub verification for new releases.

### Changed

* Various UI improvements.

## [1.0.6] - 2025-07-19

### Added

* Import support for Ten v1.6 and v1.5 (previously only v1.7+).
* Added controls to adjust the width of the Batch File Loader Panel (512, 1024, 2048, or 4096 px).
* Added Scroll viewers to Source, Destination, and Batch File Loader panels.
* Undo/Redo memory usage is now freely adjustable.

### Changed

* Memory management improvements for level imports via array pooling.

### Fixed

* Hotfix 1.0.601: Fixed missing object disposal during Ten Level reading.

## [1.0.5] - 2025-06-29

### Fixed

* Animation Preview fixes to avoid disruptions in edge cases.

## [1.0.3] - 2025-06-27

### Fixed

* Alpha channel fixes for imported classic TR levels.

## [1.0.2] - 2025-06-26

### Added

* Added File Drop Support for loading.

### Changed

* **Project Renamed:** Tool and repository renamed to *TgaBuilder*.
* Improvements to Undo/Redo logic.
* General code refactoring.

## [1.0.1]

### Changed

* Code clean-up.
* Corrections to the Batch File Loader (removed redundant 'Last Texture Index').
* Updated messages and documentation.

### Fixed

* Bugfixes for the Undo/Redo Manager.

## [1.0.0] - 2025-06-24

### Added

* Initial Release (as *THelper*).