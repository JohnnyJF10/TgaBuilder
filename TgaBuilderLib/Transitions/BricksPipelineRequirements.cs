using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions;


public enum BricksPipelineRequirements
{
    /// <summary>
    /// Complete pipeline; all steps are required: tile analysis, selection building, and edge coloring.
    /// This is the default for all transition modes except Smooth.
    /// </summary>
    RequiresAnalysis = 0,
    /// <summary>
    /// Indicates that selection building and edge coloring are required before performing the associated operation.
    /// </summary>
    /// <remarks>
    /// Prerequirement is that the 
    /// </remarks>
    RequiresSelectionBuilding = 1,
    /// <summary>
    /// Indicates that only edge coloring is required for the associated operation or algorithm.
    /// </summary>
    RequiresEdgeColoring = 2,
}
