Version 2.2.5 with major transition performance improvements, new controls to manage transition topology, and advanced brick-edge sizing and tinting options.

- Significant performance improvements for transition window calculations. Slider updates now trigger much faster redraws, with little to no visible latency on performant systems.
- New controls to fine-tune transition topology and the brick edge breakup line:
    - **Widening** (`0` to `1`, default `0`): Broadens the transition front from a point into a plateau. This replaces the old offset setting, because the previous offset use case is now covered by widening. At `1`, the side slopes collapse into the texture edges, resulting in a single-line transition. With **Hardness** at `1`, this reproduces the former offset case.
    ![WideningToEdge](Screenshots/WideningToEdge_gif.gif)
    - **Shift** (`-1` to `1`, default `0`): Moves the transition pivot/plateau left or right to create asymmetric transitions. At extreme values, one side slope can collapse into the texture edge.
    ![WideningShift](Screenshots/WideningShift_gif.gif)
- **Edge protection toggle** improvement: Edge protection can now be explicitly turned off (`false`), allowing workflows where strict border preservation is not desired.
![EdgeProtection](Screenshots/EdgeProtection_gif.gif)
- New **brick edge tinting and sizing** controls:
    - **Edge Width** (`0` to `12`): Controls how wide the edge blending band is, from no edge smoothing to a broader softened edge.
    - **Edge Tint Color**: Lets you choose the tint color applied to edge regions.
    - **Blend/application modes** for edge tinting:
        - **Multiply**: Darkens by multiplying tile and tint colors.
        - **Screen**: Lightens by inverse multiplication.
        - **Additive**: Adds color values for stronger brightening/glow.
        - **Overlay**: Increases contrast by combining multiply and screen behavior.
        - **HardLight**: Strong contrast effect driven by tint values.
        - **SoftLight**: Smoother, more subtle contrast shaping.
        - **ColorDodge**: Strong highlight/brightening effect.
        - **ColorBurn**: Strong shadow/darkening effect.
    ![EdgeTinting](Screenshots/EdgeTinting_gif.gif)
- Fixed an issue where info texts could disappear unexpectedly.
- Various code refactorings for transitions module

---

2. Version 2.2.6 with a new unified Transition window, a fully featured Modifications window, four new brick segmentation methods, new brick transition sections (Shadow, Underfilling, Manual), and quality-of-life additions to the main window.

### Modifications Window

The Modifications window has been redesigned as a dedicated side-panel dialog with a resizable split layout (image previews on the left, scrollable controls on the right). Controls are organized into three collapsible expanders:

- **Basic** expander:
    - **Exposure** (`-5` to `+5`): adjusts overall brightness in photographic stops.
    - **Brightness** (`-1` to `+1`): shifts overall luminosity linearly.
    - **Contrast** (`-1` to `+1`): increases or reduces tonal range.
    - **Highlights** (`-1` to `+1`): recovers or boosts bright tonal regions.
    - **Shadows** (`-1` to `+1`): lifts or crushes dark tonal regions.
    - **Whites** (`-1` to `+1`): clips or expands the brightest point of the image.
    - **Blacks** (`-1` to `+1`): clips or expands the darkest point of the image.

- **Color** expander:
    - **Saturation** (`-1` to `+1`): uniform increase or decrease of color intensity.
    - **Vibrance** (`-1` to `+1`): smart saturation that boosts muted colors while protecting already-saturated ones.
    - **Hue** (`-180` to `+180`): rotates all colors around the color wheel.
    - **Temperature** (`-1` to `+1`): shifts the image toward cooler (blue) or warmer (orange) tones.
    - **Tint** (`-1` to `+1`): shifts the image toward green (negative) or magenta (positive).

- **Color Overlay** expander:
    - **Eyedropper toggle**: samples the overlay color directly from the input image.
    - **Color picker**: selects a single color to blend with the texture.
    - **Overlay Amount** (`0` to `100`): blends the selected color with the texture; `0` is invisible, `100` replaces the texture entirely.
    - **Mix Mode** selector: controls how the overlay color is combined with the texture:
        - *Linear*: straightforward linear interpolation.
        - *Soft Light*: filmic soft-light response for subtle tonal shaping.
        - *OKLab Chroma*: transfers overlay chroma in perceptual color space while preserving luminance.
    - **Luma Preservation** (`0` to `1`) *(OKLab Chroma mode only)*: keeps original luminance while transferring overlay chroma.
    - **Chroma Boost** (`0` to `2`) *(OKLab Chroma mode only)*: increases or reduces overlay chroma intensity in perceptual color space.

- **Apply / Cancel / OK** action bar at the bottom of the window. The new **Apply** button previews changes without closing the dialog.

### Transition Window — Unified Smooth & Brick Window

The former separate Smooth Transition and Brick Transition windows have been **merged into a single unified Transition Helper window**. A pair of radio buttons at the top left of the window selects the active mode:

- **Smooth** mode: exposes the familiar Hardness, Widening, and Shift sliders.
- **Brick** mode: exposes a scrollable set of expanders for all brick-specific controls (Analysis, Pivot, Edge, Shadow, Underfilling, Manual — see below).

A shared **Apply / Cancel / OK** action bar is present at the bottom of the window. The **Apply** button lets you preview the transition result without closing the dialog.

#### Brick Transition — Four New Segmentation Methods

The Analysis expander now offers six segmentation methods via a drop-down selector. The four new color-based methods are:

1. **Felzenszwalb** — graph-based color segmentation.
    - **Min Size** (`1` – `500`): minimum component size before region merging.
    - **Scale** (`1` – `500`): merge tolerance scale factor (higher = larger, fewer segments).

2. **SLIC** — superpixel segmentation.
    - **Segment Count** (`1` – `2000`): target number of superpixels.
    - **Compactness** (`0.1` – `50`): trade-off between color similarity and spatial regularity.

3. **Quickshift** — mode-seeking color segmentation.
    - **Max Distance** (`1` – `50`): maximum local growth distance from seed pixel.
    - **Ratio** (`0.1` – `5`): color similarity threshold scaling.

4. **Grid Fit** — grid-aligned segmentation.
    - **Marker Radius** (`1` – `20`): size of the seed markers (1 = small, 20 = large).
    - **Angle** (`-90` – `+90`): rotation angle for the grid fitting.

#### Brick Transition — New Controls on Existing Methods

- **Watershed**: new **Marker Count** (`#`, `1` – `256`) slider — sets the number of seed markers used for watershed segmentation.
- **Brick Fit**: new **Angle** (`-90` – `+90`) slider — rotates the fitting grid.

#### Brick Transition — Gaussian Filter

A new **Gaussian** option has been added to the pre-processing filter drop-down (alongside None, Box Blur, Median, and Bilateral). When selected, a dedicated **Gaussian σ** slider (`0.1` – `20`) controls the blur radius.

#### Brick Transition — Shadow Section

A new **Shadow** expander in brick mode adds shadow rendering behind tile borders:

- **Eyedropper toggle**: samples the shadow color directly from the result image.
- **Shadow Color** picker: selects the color drawn over the background behind tile borders.
- **Shadow Size** (`0` – `32`): maximum shadow extent in pixels from the tile border.
- **Shadow Hardness** (`0` – `100`): controls how quickly the shadow fades away from tile borders.

#### Brick Transition — Underfilling Section

A new **Underfilling** expander lets you fill in under-exposed areas between tiles:

- **Reverse toggle**: turns underfilling into overfilling by substituting bright pixels instead (useful for sandy textures and bright backgrounds).
- **Underfilling Threshold** (`0` – `255`): `0` = no underfilling, `255` = maximum underfilling.
- **Underfilling Pivot** (`0` – `1`): sets the positional bias of the underfilling area (`0` = left, `1` = right).

#### Brick Transition — Manual Operations

A new **Manual** expander in brick mode enables direct pixel-level control over tile visibility on the result image:

- **Tile Visibility Pen** toggle: activates drawing mode — left-click on the result image to mark tiles as visible.
- **Tile Visibility Eraser** toggle: activates erase mode — left-click on the result image to mark tiles as hidden.
- **Reset** button: clears all manual visibility overrides and reverts to the computed result.

### Main Window

- **Gridless mode on the target panel**: hold **Space** while clicking or dragging on the target panel to make a free (grid-independent) selection. Releasing Space returns to normal grid-snapped behavior.
- **Copy / Paste** (`Ctrl+C` / `Ctrl+V`) is now supported in the **Avalonia UI** version, allowing you to copy the current selection and paste it directly from the system clipboard.