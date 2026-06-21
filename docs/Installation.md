# Installation

> If you have Windows 10/11 and either Tomb Editor version 1.9 or later or the .NET 6 runtime installed, use the standard version. Otherwise, use one of the Avalonia preview versions. 

## Download

Head over to [GitHub Releases](https://github.com/JohnnyJF10/TgaBuilder/releases) to download the latest version.

---

## Standard (Windows, .NET6, WPF, framework dependent)

The standard version is based on **.NET 6.0**. It requires Windows 10 or 11, x64 and .NET 6. If you have Tomb Editor Version 1.9 installed, you have .NET 6 insatlled already. 

Extract the files and start **TgaBuilder.exe**.

---

## Cross-Platform — Windows & Linux (Preview Avalonia UI Version)

There is also a preview cross-platform version based on Avalonia UI instead of WPF. It has been tested on Ubuntu, Linux Mint, and Windows. Some few features (mainly jpeg support) are missing, but otherwise it is fully functional. These versions, based on .NET 8, are compiled as standalone and do not require additionally installations.
For Linux, it might be required to make the binary executable. In order to do this, run 
```
chmod +x TgaBuilderAvaloniaUi
```

---

## Requirements

### Windows (WPF version)
- Windows 10 / 11 (only standard version)
- 64-bit architecture
- **.NET 6** runtime installed (only standard version; _with Tomb Editor Version 1.9, you already have the .NET 6 runtime installed.)_

---

## Important Note on Resource Usage

As a .NET WPF tool, TgaBuilder has significantly higher system resource requirements — particularly RAM — compared to TBuilder, which was written in Delphi. If you are still satisfied using TBuilder, please continue using it. TgaBuilder is **not** intended as a substitute for it. MacOs is not supported currently.
