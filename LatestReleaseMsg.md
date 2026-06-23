Version 2.2.7 continues the TrLynx push with new export formats, better save-format handling, stronger modifications tools, faster transitions, and the final rename away from TgaBuilder.

---

## 1. TrLynx Rename
* **New identity:** The application and repository are now fully renamed from **TgaBuilder** to **TrLynx**.
* **Documentation and release messaging:** The project now consistently presents itself under the TrLynx name.

## 2. New Output Formats and Save As Improvements
* **KRITA write support:** TrLynx can now write layered **`.kra`** files.
* **PSD write support:** TrLynx can now write **`.psd`** files as output.
* **Better Save As selection:** A new **Save As** dropdown flow improves output format selection and keeps the available choices aligned with what each frontend can actually export.

## 3. Transition Helper Upgrades
* **Manual single-tile editing:** In Brick / Manual mode you can now move a single tile freely and rotate it directly.
* **Layered project export:** Current transition results can be exported as layered **KRITA** and **PSD** project files, making it easier to continue editing in external tools when needed.
* **Large-texture speedups:** Transition calculations perform significantly better on larger textures thanks to algorithm and buffer optimizations.

## 4. Modifications Helper Additions
* **Color Override:** New controls let you erase colour from the current texture and transfer colour information from a second texture.
* **Texture Retrofier:** New retro-style controls can artificially limit the colour space and apply dithering for classic vibes.
* **Dithering modes:** Includes **Checkerboard**, **Bayer 4×4**, and **Bayer 8×8** options.

## 5. Linux Fixes
* **PNG/BMP saving:** Fixed Linux issues that could produce overly large saved **PNG** and **BMP** files.
* **Directory file-type handling:** Fixed a Linux bug affecting file-type directory reading.

> If you have Windows 10/11 and either Tomb Editor version 1.9 or later or the .NET 6 runtime installed, use the standard version. Otherwise, use one of the Avalonia preview versions.