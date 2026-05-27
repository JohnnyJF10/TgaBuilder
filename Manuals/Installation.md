# Installation

## Download

Head over to [GitHub Releases](https://github.com/JohnnyJF10/TgaBuilder/releases) to download the latest version.

---

## Windows (WPF Version)

For the latest releases there are two tool versions per release: the **.NET 6.0** version and the **.NET 8.0** version.

- Download the **.NET 6.0** version (`TgaBuilder-dotnet6`) if you already have Tomb Editor Version 1.9 installed on your system and you do not wish to install another .NET runtime (Tomb Editor 1.9 uses .NET 6.0 as well).

- Download the **.NET 8.0** version (`TgaBuilder-dotnet8`) if you have the .NET 8.0 runtime installed or do not mind installing it. This version has slightly better performance.

Extract the files and start **TgaBuilder.exe**.

---

## Cross-Platform — Windows & Linux (Avalonia UI Version)

There is also a cross-platform version based on Avalonia UI instead of WPF. It has been tested on Ubuntu, Linux Mint, and Windows. Some features (mainly clipboard) are missing, but otherwise it is fully functional. This version requires **.NET 8.0**.

To run the Avalonia UI version on Linux, navigate to the directory where you extracted the files and run:

```bash
dotnet TgaBuilderAvaloniaUi.dll
```

---

## Requirements

### Windows (WPF version)
- Windows 10 / 11
- 64-bit architecture
- **.NET 6** or **.NET 8** runtime installed  
  _(With Tomb Editor Version 1.9, you already have the .NET 6 runtime installed.)_

### Linux (Avalonia UI version)
- **.NET 8** runtime on the target device

---

## Important Note on Resource Usage

As a .NET WPF tool, TgaBuilder has significantly higher system resource requirements — particularly RAM — compared to TBuilder, which was written in Delphi. If you are still satisfied using TBuilder, please continue using it. TgaBuilder is **not** intended as a substitute for it.
