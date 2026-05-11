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