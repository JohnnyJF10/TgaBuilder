namespace TrLynxLib.Transitions;

public partial class TransitionHelper
{
    // Builds the layer stack for a multi-layer export of the current transition.
    // Layers are ordered bottom-to-top (index 0 = bottom). Pixels are BGRA32.
    // Reads the current pipeline state (Pixels1 = foreground/bricks, Pixels2 = background,
    // _selection for bricks), which is kept up to date by the recalc pipeline while the
    // transition window is open.
    public IReadOnlyList<TransitionExportLayer> GetExportLayers()
    {
        if (!IsActive)
            return Array.Empty<TransitionExportLayer>();

        return TypeOfTransition == TransitionType.Smooth
            ? BuildSmoothExportLayers()
            : BuildBricksExportLayers();
    }

    // Bricks: clean raw split.
    //   0) Background  = Pixels2 (full)                         visible
    //   1) Bricks      = Pixels1 at selected pixels             visible
    //   2) Bricks hidden = Pixels1 at unselected pixels         hidden
    // Unmasked pixels are fully transparent; masked pixels keep the source alpha.
    private IReadOnlyList<TransitionExportLayer> BuildBricksExportLayers()
    {
        int count = Width * Height;

        byte[] background = (byte[])Pixels2.Clone();
        byte[] selected = new byte[count * TRANSITIONS_BPP];
        byte[] unselected = new byte[count * TRANSITIONS_BPP];

        bool[] selection = _selection;

        for (int i = 0; i < count; i++)
        {
            int o = i * TRANSITIONS_BPP;
            byte[] target = selection[i] ? selected : unselected;

            target[o + 0] = Pixels1[o + 0];
            target[o + 1] = Pixels1[o + 1];
            target[o + 2] = Pixels1[o + 2];
            target[o + 3] = Pixels1[o + 3];
            // The other buffer stays fully transparent (all zero) at this pixel.
        }

        // Manually moved/rotated tiles are selected at their new positions, where the loop above
        // copied the wrong (static) source pixels. Repaint their true content from the original
        // source pixels so the exported "Bricks" layer matches the on-screen result.
        foreach (var tile in _manipulatedTiles)
        {
            int[] dst = tile.DstOffsets;
            int[] src = tile.SrcOffsets;
            for (int i = 0; i < dst.Length; i++)
            {
                int d = dst[i] * TRANSITIONS_BPP;
                int s = src[i] * TRANSITIONS_BPP;
                selected[d + 0] = Pixels1[s + 0];
                selected[d + 1] = Pixels1[s + 1];
                selected[d + 2] = Pixels1[s + 2];
                selected[d + 3] = Pixels1[s + 3];
            }
        }

        return new[]
        {
            new TransitionExportLayer(background, Width, Height, "Background", true),
            new TransitionExportLayer(selected, Width, Height, "Bricks", true),
            new TransitionExportLayer(unselected, Width, Height, "Bricks (hidden)", false),
        };
    }

    // Smooth: an opaque background plus a weighted foreground reproduces the in-app blend
    // (result = Pixels2*(1-weight) + Pixels1*weight) under normal source-over compositing.
    //   0) Background (Texture 2) = Pixels2 (full)              visible
    //   1) Blend (Texture 1)      = Pixels1 with A = weight*255 visible
    //   2) Texture 2 (source)     = Pixels2 (full)              hidden  (reference)
    //   3) Texture 1 (source)     = Pixels1 (full)              hidden  (reference)
    private IReadOnlyList<TransitionExportLayer> BuildSmoothExportLayers()
    {
        int count = Width * Height;

        byte[] background = (byte[])Pixels2.Clone();
        byte[] blend = new byte[count * TRANSITIONS_BPP];
        byte[] texture2Full = (byte[])Pixels2.Clone();
        byte[] texture1Full = (byte[])Pixels1.Clone();

        // Same setup as MixSmooth so the exported weight matches the preview exactly.
        Hardness = Math.Clamp(Hardness, 0.0f, 1.0f);
        Pivot = Math.Clamp(Pivot, 0.0f, 1.0f);
        Widening = Math.Clamp(Widening, 0.0f, 1.0f);

        float lower = Pivot * Hardness;
        float upper = 1.0f - (1.0f - Pivot) * Hardness;
        bool isHardCut = (upper <= lower + 0.00001f);

        for (int y = 0; y < Height; y++)
        {
            float ny = Height > 1 ? (float)y / (Height - 1) : 0f;

            for (int x = 0; x < Width; x++)
            {
                float nx = Width > 1 ? (float)x / (Width - 1) : 0f;

                float weight = ComputeWeight(Direction, Pivot, Widening, Shift, lower, upper, isHardCut, nx, ny);

                int o = (y * Width + x) * TRANSITIONS_BPP;
                blend[o + 0] = Pixels1[o + 0];
                blend[o + 1] = Pixels1[o + 1];
                blend[o + 2] = Pixels1[o + 2];
                blend[o + 3] = (byte)Math.Clamp(weight * 255f + 0.5f, 0f, 255f);
            }
        }

        return new[]
        {
            new TransitionExportLayer(background, Width, Height, "Background (Texture 2)", true),
            new TransitionExportLayer(blend, Width, Height, "Blend (Texture 1)", true),
            new TransitionExportLayer(texture2Full, Width, Height, "Texture 2 (source)", false),
            new TransitionExportLayer(texture1Full, Width, Height, "Texture 1 (source)", false),
        };
    }
}
