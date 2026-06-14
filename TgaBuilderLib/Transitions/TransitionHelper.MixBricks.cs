namespace TgaBuilderLib.Transitions;

partial class TransitionHelper
{
    // Draws segmented tile pixels over a background using a pixel selection derived from
    // tile topology and optional corner slicing. Pipeline: Input → Label Map → _selection → Result.
    // Each pipeline stage writes directly into the reusable cached buffers (_labels, _selection)
    // and the caller-provided result buffer, so no per-recalc allocation occurs here.
    public void MixBricks(
      byte[] tilePixels,
      byte[] bgPixels,
      byte[] result)
    {
        if (bgPixels.Length != tilePixels.Length)
            throw new ArgumentException("Pixel arrays must have same length.");


        // Requirements correction in case this is first time use
        if (!_labelsBuilt || _tileSegmentList.Count == 0)
            CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;

        // Pipeline step 1: Analyze tile segments to build Label Map (Input → Label Map).
        // Writes _labels in place and refreshes _tileSegmentList.
        if (CurrentBricksPipelineRequirements == BricksPipelineRequirements.RequiresAnalysis)
        {
            _tileSegmentList = BricksAnalyze(tilePixels);
            _labelsBuilt = true;
        }


        // Requirements correction in case this is first time use
        if (!_selectionBuilt
            && CurrentBricksPipelineRequirements > BricksPipelineRequirements.RequiresSelectionBuilding)
            CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresSelectionBuilding;

        // Pipeline step 2: Determine which pixels are drawn (Input → Label Map → _selection).
        // Writes _selection in place.
        if (CurrentBricksPipelineRequirements <= BricksPipelineRequirements.RequiresSelectionBuilding)
        {
            BuildSelection(_tileSegmentList, _labels, tilePixels);
            _selectionBuilt = true;
            _edgeDistValid = false; // selection changed → edge-distance map must be recomputed
        }


        // Pipeline step 3: Blend selected tile pixels with background based on edge proximity and
        // EdgeColor (_selection → Result). Writes the caller-provided result buffer in place.
        BricksDraw(tilePixels, bgPixels, _selection, result);
    }
}
