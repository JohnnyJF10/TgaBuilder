# Modifications & Transition Helpers

This guide covers the **Modifications Window** and both **Transition Helper** windows (Smooth and Brick). These tools are accessed from the Selection Area action buttons in the main window.

---

## Table of Contents

- [Modifications Window](#modifications-window)
  - [Basic Adjustments](#basic-adjustments)
  - [Color Adjustments](#color-adjustments)
  - [Color Overlay](#color-overlay)
- [Transition Helpers — Overview](#transition-helpers--overview)
- [Smooth Transition Helper](#smooth-transition-helper)
  - [Smooth Controls](#smooth-controls)
  - [Pivot (Smooth)](#pivot-smooth)
- [Brick Transition Helper](#brick-transition-helper)
  - [Analysis](#analysis)
    - [Segmentation Algorithms](#segmentation-algorithms)
      - [Felzenszwalb](#felzenszwalb)
      - [SLIC (Simple Linear Iterative Clustering)](#slic-simple-linear-iterative-clustering)
      - [Quickshift](#quickshift)
      - [Watershed](#watershed)
      - [Brick Fit](#brick-fit)
      - [Grid Fit](#grid-fit)
    - [Filters](#filters)
      - [None](#none)
      - [Box Blur](#box-blur)
      - [Median](#median)
      - [Bilateral](#bilateral)
      - [Gaussian](#gaussian)
  - [Pivot (Brick)](#pivot-brick)
  - [Edge](#edge)
  - [Shadow](#shadow)
  - [Underfilling](#underfilling)
  - [Manual](#manual)

---

---

# Modifications Window

The Modifications Window lets you apply non-destructive image adjustments to the current selection before placing it. All changes are previewed in real time. The window contains three expandable sections.

<!-- 📸 SCREENSHOT NEEDED: Full Modifications window showing all three expanders open -->

---

## Basic Adjustments

<!-- 📸 SCREENSHOT NEEDED: Basic expander section with all sliders visible -->

The **Basic** expander contains fundamental luminance and tonal adjustments:

| Parameter     | Range       | Description                                                                 |
|---------------|-------------|-----------------------------------------------------------------------------|
| **Exposure**  | −5 to +5    | Photographic stops — doubles/halves brightness per stop                     |
| **Brightness**| −1 to +1    | Linear brightness offset applied uniformly                                  |
| **Contrast**  | −1 to +1    | Expands or compresses the overall tonal range                               |
| **Highlights**| −1 to +1    | Adjusts only bright regions of the image                                    |
| **Shadows**   | −1 to +1    | Adjusts only dark regions of the image                                      |
| **Whites**    | −1 to +1    | Shifts the brightest point (white clipping)                                 |
| **Blacks**    | −1 to +1    | Shifts the darkest point (black clipping)                                   |

---

## Color Adjustments

<!-- 📸 SCREENSHOT NEEDED: Color expander section with all sliders visible -->

The **Color** expander provides hue, saturation, and white-balance controls:

| Parameter       | Range         | Description                                                             |
|-----------------|---------------|-------------------------------------------------------------------------|
| **Saturation**  | −1 to +1      | Uniform color intensity increase/decrease                               |
| **Vibrance**    | −1 to +1      | Smart saturation — boosts muted colors more than already-saturated ones |
| **Hue**         | −180° to +180°| Rotates the entire color wheel                                          |
| **Temperature** | −1 to +1      | Cool (blue) ↔ Warm (orange) shift                                       |
| **Tint**        | −1 to +1      | Green ↔ Magenta shift                                                   |

---

## Color Overlay

<!-- 📸 SCREENSHOT NEEDED: Color Overlay expander showing eyedropper, color picker, amount slider, and mix mode dropdown -->

The **Color Overlay** expander lets you tint the entire selection with a chosen color:

| Control              | Description                                                                     |
|----------------------|---------------------------------------------------------------------------------|
| **Eyedropper**       | Toggle eyedropper mode to sample a color directly from the input image          |
| **Color Picker**     | Choose an overlay color manually                                                |
| **Overlay Amount**   | 0 = invisible, 100 = only the overlay color                                     |
| **Mixing Mode**      | Select blending algorithm: **Linear**, **Soft Light**, or **OKLab Chroma**      |

When **OKLab Chroma** mode is selected, two additional sliders appear:

| Parameter            | Description                                                                     |
|----------------------|---------------------------------------------------------------------------------|
| **Luma Preservation**| How much of the original luminance to keep (0 = none, 1 = full preservation)    |
| **Chroma Boost**     | Intensifies or reduces the chroma component of the overlay blend                |

---

---

# Transition Helpers — Overview

The transition helper tools open in separate windows and allow you to build transition textures from two input selections (picked from Source or Destination panels). There are two modes:

- **Smooth** — soft directional gradient transitions
- **Brick** — marker-based tile segmentation transitions

Both modes share the **Transition Direction** control (Top, Right, Bottom, Left, Diagonal Top-Left, Diagonal Top-Right) and a **Pivot** section.

<!-- 📸 SCREENSHOT NEEDED: Transition window showing mode selection (Smooth/Brick radio buttons) at top -->

---

---

# Smooth Transition Helper

The Smooth mode creates soft directional transitions between two textures. It uses a gradient mask defined by the direction, pivot, hardness, and widening parameters.

<!-- 📸 SCREENSHOT NEEDED: Smooth Transition window with result preview and controls visible -->

---

## Smooth Controls

<!-- 📸 SCREENSHOT NEEDED: Smooth controls section showing Hardness, Widening, and Shift sliders -->

| Parameter    | Range     | Description                                                                                 |
|--------------|-----------|-------------------------------------------------------------------------------------------  |
| **Hardness** | 0 to 1    | 0 = soft gradient blend; 1 = hard sharp edge between the two textures                       |
| **Widening** | 0 to 1    | 0 = transition confined to center; 1 = transition plateau touches the edges                 |
| **Shift**    | −1 to +1  | Moves the transition front left or right relative to the pivot line                         |

---

## Pivot (Smooth)

<!-- 📸 SCREENSHOT NEEDED: Pivot slider in smooth mode -->

The **Pivot** slider (0 to 1) controls where the border line between the two textures is positioned. At 0.5 it is centered; lower values shift it toward the first texture, higher values toward the second.

---

---

# Brick Transition Helper

The Brick mode uses image segmentation algorithms to detect individual "tiles" or "bricks" in the input texture and selectively draws them based on whether their centroid falls within the pivot shape. This allows you to create structured brick/tile-based transitions.

The Brick mode contains **six expandable sections**, each described in detail below.

<!-- 📸 SCREENSHOT NEEDED: Brick Transition window full view showing all expanders (collapsed) in the right panel -->

---

## Analysis

<!-- 📸 SCREENSHOT NEEDED: Analysis expander fully expanded, showing segmentation method dropdown, filter dropdown, and all conditional sliders -->

The **Analysis** expander is the core of the Brick transition. It controls how the input texture is segmented into individual regions (tiles/bricks).

There are two main controls at the top:

1. **Segmentation Method** — the algorithm used to detect tile boundaries
2. **Filter** — an optional pre-processing filter applied before segmentation

A **Map** toggle button in the header shows/hides the label map overlay for debugging detected regions.

---

### Segmentation Algorithms

TgaBuilder provides **six** segmentation algorithms. Each algorithm has its own set of parameters that appear when the algorithm is selected.

---

#### Felzenszwalb

<!-- 📸 SCREENSHOT NEEDED: Analysis expander with Felzenszwalb selected, showing Min Size and Scale sliders -->

**Felzenszwalb** is a graph-based segmentation algorithm that efficiently merges pixels into regions based on boundary evidence. It works well for textures with varying region sizes.

| Parameter      | Description                                                                                          |
|----------------|------------------------------------------------------------------------------------------------------|
| **Min Size**   | Minimum number of pixels per segment. Higher values force the algorithm to merge small segments into larger neighbors, reducing over-segmentation. |
| **Scale**      | Controls the preference for larger vs. smaller segments. Higher values produce larger, coarser segments; lower values produce more fine-grained segments. |

**When to use:** Good general-purpose choice for most textures. Works well for natural stone, irregular tiles, and textures with varying segment sizes. Default algorithm.

**Tips:**
- Start with a medium Scale and increase Min Size to reduce fragmentation.
- For large bricks with clear boundaries, increase Scale.
- For fine mosaics, decrease Scale and Min Size.

---

#### SLIC (Simple Linear Iterative Clustering)

<!-- 📸 SCREENSHOT NEEDED: Analysis expander with SLIC selected, showing Segment Count and Compactness sliders -->

**SLIC** produces compact, approximately uniformly sized superpixels by clustering in a combined color + spatial space. It divides the image into a regular grid of regions.

| Parameter          | Description                                                                                   |
|--------------------|-----------------------------------------------------------------------------------------------|
| **Segment Count**  | Target number of superpixels to generate. More segments = smaller individual regions.          |
| **Compactness**    | Balances color similarity vs. spatial proximity. Higher values produce more compact, regular-shaped segments; lower values produce segments that follow color boundaries more closely. |

**When to use:** Best for textures where you want approximately uniformly sized tiles. Ideal for regular tile patterns, mosaic floors, and grid-like textures.

**Tips:**
- Set Segment Count roughly to the number of visible tiles in the texture.
- For perfectly square tiles, use high Compactness.
- For tiles that follow natural color boundaries, lower the Compactness.

---

#### Quickshift

<!-- 📸 SCREENSHOT NEEDED: Analysis expander with Quickshift selected, showing Max Distance and Ratio sliders -->

**Quickshift** is a mode-seeking segmentation algorithm that groups pixels by shifting them toward denser regions in color-spatial space. It produces irregularly shaped segments that closely follow color boundaries.

| Parameter         | Description                                                                                    |
|-------------------|-----------------------------------------------------------------------------------------------|
| **Max Distance**  | Maximum distance in the color-spatial feature space for pixels to be grouped together. Larger values produce larger segments. |
| **Ratio**         | Balance between color and spatial proximity (0 to 1). 0 = purely spatial; 1 = purely color-based. |

**When to use:** Good for textures with organic, irregular boundaries. Works well for natural stone, weathered bricks, and textures where tile boundaries are not perfectly straight.

**Tips:**
- Start with a Ratio of 0.5 for balanced results.
- For textures where color is the primary separator, increase Ratio toward 1.
- Increase Max Distance for coarser segmentation.

---

#### Watershed

<!-- 📸 SCREENSHOT NEEDED: Analysis expander with Watershed selected, showing Invert Grayscale toggle, Marker Count slider -->

**Watershed** treats the grayscale image as a topographic surface and "floods" from marker seeds, finding boundaries where different flood regions meet. The markers are automatically placed based on local minima detection.

| Parameter          | Description                                                                                    |
|--------------------|-----------------------------------------------------------------------------------------------|
| **Invert Grayscale** | Toggle to invert the grayscale input before processing. Useful when joints are lighter than bricks. |
| **Marker Count (#)** | Controls the number of initial seed markers. A larger value places more seeds, producing more/smaller segments. A smaller value produces fewer/larger segments. |

**When to use:** Excellent for bricks and tiles with visible mortar joints or grooves. The algorithm naturally finds boundaries at the "ridges" (joint lines) between regions.

**Tips:**
- For natural or temple-style old bricks, use Watershed with a **Box Blur** filter and a small Marker Count.
- Toggle **Invert Grayscale** if joints appear as light lines rather than dark grooves.
- Combine with Bilateral filter to preserve edges while smoothing noise.

---

#### Brick Fit

<!-- 📸 SCREENSHOT NEEDED: Analysis expander with Brick Fit selected, showing Marker Radius and Angle sliders -->

**Brick Fit** is a custom algorithm that fits a brick-pattern grid to the image. It uses the grayscale structure of the image to determine the best fit for a regular brick layout (staggered rows).

| Parameter        | Description                                                                                      |
|------------------|--------------------------------------------------------------------------------------------------|
| **Marker Radius**| Controls the size of the pattern-fitting kernel. Larger values produce larger brick regions.      |
| **Angle**        | Rotation angle for the brick pattern grid (in degrees). Use to align with angled brick textures. |
| **Invert Grayscale** | Toggle to invert grayscale input before pattern fitting.                                     |

**When to use:** Ideal for regular wall bricks with a consistent staggered (running bond) pattern. Works best when the mortar lines are clearly visible.

**Tips:**
- For standard horizontal brickwork, keep Angle at 0.
- Adjust Marker Radius until the detected regions match individual bricks.
- Works best with clean, regular masonry textures.

---

#### Grid Fit

<!-- 📸 SCREENSHOT NEEDED: Analysis expander with Grid Fit selected, showing Marker Radius and Angle sliders -->

**Grid Fit** is similar to Brick Fit but uses a regular rectangular grid (no stagger offset). It attempts to align a regular grid to the image structure.

| Parameter        | Description                                                                                      |
|------------------|--------------------------------------------------------------------------------------------------|
| **Marker Radius**| Controls the size of the grid cells. Larger values produce larger rectangular regions.            |
| **Angle**        | Rotation angle for the grid (in degrees). Use to align with rotated tile patterns.               |
| **Invert Grayscale** | Toggle to invert grayscale input before grid fitting.                                        |

**When to use:** Best for perfectly rectangular tile patterns (bathroom tiles, floor tiles, checkerboard patterns) where there is no horizontal offset between rows.

**Tips:**
- For wall tiles with clear horizontal and vertical joints, use Grid Fit with no filter.
- For wall bricks with clear joints in either the horizontal or vertical direction, choose Grid Fit with no input filter and a large Marker Radius.
- Adjust Angle for diagonal tile patterns.

---

### Filters

Filters are applied to the image **before** the segmentation algorithm runs. They can dramatically improve segmentation quality by smoothing noise or enhancing edges.

---

#### None

No pre-processing. The raw input image is passed directly to the segmentation algorithm. Use this when the texture has very clear, well-defined boundaries that don't need any help.

---

#### Box Blur

<!-- 📸 SCREENSHOT NEEDED: Result comparison showing effect of Box Blur filter on segmentation -->

A simple averaging blur that replaces each pixel with the mean of its neighborhood. This smooths out fine detail and noise uniformly.

**When to use:** Good general-purpose smoothing. Helps Watershed find cleaner boundaries by removing small surface details while keeping the overall structure intact. Recommended for natural or temple-style old bricks with a small Marker Count.

---

#### Median

<!-- 📸 SCREENSHOT NEEDED: Result comparison showing effect of Median filter on segmentation -->

Replaces each pixel with the median value of its neighborhood. Unlike Box Blur, Median preserves edges while removing salt-and-pepper noise.

**When to use:** When the texture has random noise or very small speckles that confuse the segmentation, but you want to keep sharp edges intact. Good for photographed brick textures with sensor noise.

---

#### Bilateral

<!-- 📸 SCREENSHOT NEEDED: Analysis expander showing Bilateral selected with Sigma slider visible -->

An edge-preserving smoothing filter that averages pixels based on both spatial distance and color similarity. Pixels across strong edges are not averaged together.

| Parameter           | Description                                                                        |
|---------------------|------------------------------------------------------------------------------------|
| **Bilateral Sigma (σ)** | Controls the strength of the smoothing. Higher values produce more aggressive smoothing while still preserving edges. |

**When to use:** When you want to smooth out texture surface detail (grain, roughness) without blurring the mortar joints. Excellent for photographic textures where you want to preserve the joint lines.

---

#### Gaussian

<!-- 📸 SCREENSHOT NEEDED: Analysis expander showing Gaussian selected with Sigma slider visible -->

A Gaussian-weighted blur that smooths the image with a bell-curve kernel. Produces a softer blur than Box Blur with less ringing.

| Parameter          | Description                                                                         |
|--------------------|-------------------------------------------------------------------------------------|
| **Gaussian Sigma (σ)** | Controls the blur radius. Higher values produce more blurring. The kernel size adapts to the sigma value. |

**When to use:** When you want smooth, artifact-free blurring. Good for textures with fine detail that needs to be suppressed before segmentation. Slightly better quality than Box Blur for most use cases.

---

---

## Pivot (Brick)

<!-- 📸 SCREENSHOT NEEDED: Pivot expander in brick mode showing Reverse Pivot, Protect Edges, Slice Corners, Widening and Shift -->

The **Pivot** expander controls which detected tiles are drawn (foreground) and which become background, based on a directional shape.

| Control             | Description                                                                                          |
|---------------------|------------------------------------------------------------------------------------------------------|
| **Reverse Pivot**   | Inverts the drawing logic — tiles that would be foreground become background and vice versa           |
| **Protect Edges**   | Ensures tiles touching the image edges are always kept, preventing edge artifacts                     |
| **Slice Corners**   | (Enabled when Protect Edges is active) Cuts corner tiles for more accurate adjacent texture placement |
| **Widening**        | 0 = pivot shape confined to center; 1 = pivot shape touches the edges                                |
| **Shift**           | −1 to +1 — moves the pivot shape left or right relative to center                                    |

**How it works:** After segmentation detects all tiles, each tile's centroid is tested against the pivot shape (defined by the transition direction and pivot value). Tiles whose centroid falls inside the shape are drawn; others become the background texture.

---

## Edge

<!-- 📸 SCREENSHOT NEEDED: Edge expander showing eyedropper, color picker, blend mode dropdown, and width slider -->

The **Edge** expander adds a colored border/outline around detected tile boundaries in the transition result.

| Control          | Description                                                                          |
|------------------|--------------------------------------------------------------------------------------|
| **Eyedropper**   | Sample edge color directly from the image                                            |
| **Color Picker** | Choose edge color manually                                                           |
| **Blend Mode**   | How the edge color combines with the underlying tile pixels                          |
| **Edge Width**   | 1 = narrow edge (1 pixel); 12 = wide edge (12 pixels)                               |

**When to use:** To simulate mortar lines, grout, or demolition edges between bricks in the transition result. Helps make the transition look more realistic by adding visible separation between tiles.

---

## Shadow

<!-- 📸 SCREENSHOT NEEDED: Shadow expander showing eyedropper, color picker, size and hardness sliders -->

The **Shadow** expander adds a shadow effect along tile borders, giving depth to the transition result.

| Control             | Description                                                                       |
|---------------------|-----------------------------------------------------------------------------------|
| **Eyedropper**      | Sample shadow color directly from the image                                       |
| **Color Picker**    | Choose shadow color manually                                                      |
| **Shadow Size**     | 0 to 32 pixels — how far the shadow extends from tile borders                    |
| **Shadow Hardness** | 0% to 100% — how quickly the shadow fades (0% = very soft, 100% = hard shadow)   |

**When to use:** To add visual depth and a sense of relief to brick transitions. Makes detected tiles appear to "pop out" from the background.

---

## Underfilling

<!-- 📸 SCREENSHOT NEEDED: Underfilling expander showing Reverse toggle, Threshold slider, and Pivot slider -->

The **Underfilling** expander provides a luminance-based fallback for the pivot shape. Instead of using only the geometric pivot, tiles can also be selected based on their brightness.

| Control                   | Description                                                                                |
|---------------------------|--------------------------------------------------------------------------------------------|
| **Reverse Underfilling**  | Toggle to use bright pixels instead of dark pixels for underfilling                        |
| **Underfilling Threshold**| 0 to 255 — luminance cutoff. Pixels above/below this value trigger underfilling            |
| **Underfilling Pivot**    | 0 to 1 — spatial position of the underfilling boundary (0 = left, 1 = right)              |

**How it works:** Underfilling overrides the standard pivot-based tile drawing for tiles that contain predominantly dark (or bright, if reversed) pixels below the threshold. This helps in situations where purely geometric pivot logic doesn't capture the desired visual result.

---

## Manual

<!-- 📸 SCREENSHOT NEEDED: Manual expander showing Draw/Erase toggle buttons and Reset button, with manual painting visible on result image -->

The **Manual** expander allows you to override the algorithmic tile visibility by painting directly on the result image.

| Control                          | Description                                                            |
|----------------------------------|------------------------------------------------------------------------|
| **Draw Mode** (Pen icon)         | Click to enable drawing — paint tiles as visible (foreground)          |
| **Erase Mode** (Eraser icon)     | Click to enable erasing — paint tiles as hidden (background)           |
| **Reset Explicit Visibility**    | Clears all manual overrides and reverts to algorithm-only results      |

**How it works:** After the segmentation and pivot logic determine which tiles are visible, you can manually correct the result by painting on the result image. Drawing on a tile forces it to be visible; erasing forces it to be hidden. This provides a final manual correction pass without changing any algorithm parameters.

**Tips:**
- Use the indicator map overlay to see exactly which tiles have been manually overridden.
- Reset when you want to go back to a purely algorithmic result.
