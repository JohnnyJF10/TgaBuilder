using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TgaBuilderLib.Transitions;

public partial class TransitionHelper
{
    /// <summary>
    /// The ComputeTopology method is designed to calculate a distance-based gradient for texture transitions
    /// within a 2D coordinate system (0 to 1). It is primarily used to create blend weights between
    /// different textures or to exclude tiles from drawing if they do not meet certain threshold conditions.
    /// The function determines how far a given normalized point
    /// is from the transition's center or edges, effectively shaping the "profile" of the transition.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (float distanceToEdge1, float distanceToEdge2) ComputeTopology(TransitionMode mode, float normalizedX, float normalizedY)
    {
        // Clamping for safety (in case it wasn't handled externally)
        Shift = Math.Clamp(Shift, -1.0f, 1.0f);

        // Factors for the "left/top" and "right/bottom" slopes based on shift
        float leftSlopeFactor = 1.0f + Shift;
        float rightSlopeFactor = 1.0f - Shift;

        // Epsilon to prevent division by zero
        const float epsilon = 0.000001f;

        switch (mode)
        {
            // ==========================================
            // ORTHOGONAL CASES
            // ==========================================
            case TransitionMode.Top:
            case TransitionMode.Bottom:
            case TransitionMode.Left:
            case TransitionMode.Right:
                {
                    float wideningInverse = Widening >= 1.0f ? 1e6f : 1.0f / (1.0f - Widening);

                    if (mode == TransitionMode.Top || mode == TransitionMode.Bottom)
                    {
                        float sideDistance = Math.Min(normalizedX / Math.Max(epsilon, leftSlopeFactor), (1.0f - normalizedX) / Math.Max(epsilon, rightSlopeFactor));
                        float distanceX = Widening >= 1.0f ? 0.5f : Math.Min(sideDistance * wideningInverse, 0.5f);

                        return mode == TransitionMode.Top
                            ? (Math.Min(distanceX, 1.0f - normalizedY), normalizedY)
                            : (Math.Min(distanceX, normalizedY), 1.0f - normalizedY);
                    }
                    else // Left or Right
                    {
                        float sideDistance = Math.Min(normalizedY / Math.Max(epsilon, leftSlopeFactor), (1.0f - normalizedY) / Math.Max(epsilon, rightSlopeFactor));
                        float distanceY = Widening >= 1.0f ? 0.5f : Math.Min(sideDistance * wideningInverse, 0.5f);

                        return mode == TransitionMode.Left
                            ? (Math.Min(distanceY, 1.0f - normalizedX), normalizedX)
                            : (Math.Min(distanceY, normalizedX), 1.0f - normalizedX);
                    }
                }

            // ==========================================
            // DIAGONAL CASES
            // ==========================================
            case TransitionMode.DiagonalTopLeft:
            case TransitionMode.DiagonalTopRight:
                {
                    float coordA = normalizedX;
                    float coordB = mode == TransitionMode.DiagonalTopLeft ? normalizedY : 1.0f - normalizedY;
                    float averageCoord = (coordA + coordB) * 0.5f;

                    // crossSectionWidth is the width of the cross-section at the current depth
                    float crossSectionWidth = (coordA + coordB) <= 1.0f ? (coordA + coordB) : (2.0f - (coordA + coordB));

                    float coordANew, coordBNew;
                    if (crossSectionWidth < 0.0001f)
                    {
                        coordANew = coordA;
                        coordBNew = coordB;
                    }
                    else
                    {
                        // 1. Calculate 1D coordinate on the cross-section [-1, 1]
                        // 1  = Fully at the edge where coordA > coordB
                        // -1 = Fully at the edge where coordB > coordA
                        float crossSectionPosition = (coordA - coordB) / crossSectionWidth;

                        // 2. The center (peak/plateau) shifts exactly by the Shift value.
                        // At Shift = 1, the center lies at crossSectionPosition = 1 (edge).
                        float tiltedCenter = Shift;

                        // 3. Calculate normalized distance to the shifted center [0, 1]
                        float normalizedTiltedDistance;
                        bool isRightOfCenter = crossSectionPosition >= tiltedCenter;

                        if (isRightOfCenter)
                        {
                            // We are to the right of the center
                            float availableSpace = 1.0f - tiltedCenter;
                            normalizedTiltedDistance = availableSpace < epsilon ? 1.0f : (crossSectionPosition - tiltedCenter) / availableSpace;
                        }
                        else
                        {
                            // We are to the left of the center
                            float availableSpace = tiltedCenter - (-1.0f);
                            normalizedTiltedDistance = availableSpace < epsilon ? 1.0f : (tiltedCenter - crossSectionPosition) / availableSpace;
                        }

                        // Clamping for safety against floating point inaccuracies
                        normalizedTiltedDistance = Math.Max(0.0f, Math.Min(1.0f, normalizedTiltedDistance));

                        // 4. Apply widening (create plateau)
                        float widenedDistance = Widening >= 1.0f
                            ? 0.0f
                            : Math.Max(0.0f, normalizedTiltedDistance - Widening) / (1.0f - Widening);

                        // 5. Transform back into coordinate space
                        // The final Math.Min(coordANew, coordBNew) evaluation automatically forms a symmetrical peak.
                        // To achieve asymmetrical slopes, we simulate that the NEW center is exactly at 0.
                        float newCrossSectionPosition = isRightOfCenter ? widenedDistance : -widenedDistance;

                        // Reconstruct the new difference
                        float newDifference = newCrossSectionPosition * crossSectionWidth;
                        float offsetFromAverage = newDifference * 0.5f;

                        // The plateau (averageCoord) remains untouched; only the slope gradient (offset) changes.
                        // This causes the shape to slide parallel to its own orientation.
                        coordANew = averageCoord + offsetFromAverage;
                        coordBNew = averageCoord - offsetFromAverage;
                    }

                    if (mode == TransitionMode.DiagonalTopLeft)
                        return (Math.Min(1.0f - coordANew, 1.0f - coordBNew), Math.Min(coordANew, coordBNew));
                    else
                        return (Math.Min(coordANew, coordBNew), Math.Min(1.0f - coordANew, 1.0f - coordBNew));
                }

            default:
                return (0f, 0f);
        }
    }
}
