namespace TgaBuilderLib.Transitions;

partial class TransitionHelper
{
    // Draws segmented tile pixels over a background using a pixel selection derived from
    // tile topology and optional corner slicing. Pipeline: Input → Label Map → Selection → Result.
    public byte[] MixBricks(
      byte[] tilePixels,
      byte[] bgPixels)
    {
        if (bgPixels.Length != tilePixels.Length)
            throw new ArgumentException("Pixel arrays must have same length.");


        // Requirements correction in case this is first time use
        if (Labels.Length == 0|| TileSegmentList.Count == 0)
            CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresAnalysis;

        // Pipeline step 1: Analyze tile segments to build Label Map (Input → Label Map)
        var currentLabels = new int[Labels.Length];
        var currentTileSegments = new List<TileSegment>();
        (currentLabels, currentTileSegments) = CurrentBricksPipelineRequirements == BricksPipelineRequirements.RequiresAnalysis
            ? BricksAnalyze(tilePixels)
            : (Labels, TileSegmentList);


        // Requirements correction in case this is first time use
        if (Selection.Length == 0
            && CurrentBricksPipelineRequirements > BricksPipelineRequirements.RequiresSelectionBuilding)
            CurrentBricksPipelineRequirements = BricksPipelineRequirements.RequiresSelectionBuilding;

        // Pipeline step 2: Determine which pixels are drawn (Input → Label Map → Selection)
        var currentSelection = new bool[Width * Height];
        currentSelection = CurrentBricksPipelineRequirements <= BricksPipelineRequirements.RequiresSelectionBuilding
            ? BuildSelection(currentTileSegments, currentLabels, Mode, ReversePivot)
            : Selection;


        // Pipeline step 3: Blend selected tile pixels with background based on edge proximity and EdgeColor (Selection → Result)
        // If statement here, todo
        byte[] result = BricksDraw(tilePixels, bgPixels, currentSelection);


        // Write back pipeline results to properties for reuse
        switch (CurrentBricksPipelineRequirements)
        {
            case BricksPipelineRequirements.RequiresAnalysis:
                Labels = currentLabels;
                TileSegmentList = currentTileSegments;
                goto case BricksPipelineRequirements.RequiresSelectionBuilding;

            case BricksPipelineRequirements.RequiresSelectionBuilding:
                Selection = currentSelection;
                goto case BricksPipelineRequirements.RequiresEdgeColoring;

            case BricksPipelineRequirements.RequiresEdgeColoring:
                break;
            default:
                break;
        }

        return result;
    }
}
