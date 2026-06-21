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
    /// The ComputeFocus method is designed to calculate a distance-based gradient for texture transitions
    /// within a 2D coordinate system (0 to 1). It is primarily used to create blend weights between
    /// different textures or to exclude tiles from drawing if they do not meet certain threshold conditions.
    /// The function determines how far a given normalized point
    /// is from the transition's center or edges, effectively shaping the "profile" of the transition.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float ComputeFocus(TransitionDirection mode, float normalizedX, float normalizedY, float widening = 0f, float shift = 0f)
    {
        float distToT1 = 0, distToT2 = 0;
        const float epsilon = 0.000001f;

        // Clamping for safety
        shift = Math.Clamp(shift, -1.0f, 1.0f);

        if (mode <= TransitionDirection.Left)
        {
            // ==========================================
            // ORTHOGONAL CASES
            // ==========================================
            float wideningInverse = widening >= 1.0f ? 1e6f : 1.0f / (1.0f - widening);

            if (mode == TransitionDirection.Top || mode == TransitionDirection.Bottom)
            {
                // Calculate side distance with explicit edge handling for shift
                float sideDistance;
                if (shift >= 1.0f - epsilon)
                    sideDistance = normalizedX * 0.5f; // Peak is at the right edge
                else if (shift <= -1.0f + epsilon)
                    sideDistance = (1.0f - normalizedX) * 0.5f; // Peak is at the left edge
                else
                    sideDistance = Math.Min(normalizedX / (1.0f + shift), (1.0f - normalizedX) / (1.0f - shift));

                float distanceX = widening >= 1.0f ? 0.5f : Math.Min(sideDistance * wideningInverse, 0.5f);

                (distToT1, distToT2) = mode == TransitionDirection.Top
                    ? (Math.Min(distanceX, 1.0f - normalizedY), normalizedY)
                    : (Math.Min(distanceX, normalizedY), 1.0f - normalizedY);
            }
            else // Left or Right
            {
                float sideDistance;
                if (shift >= 1.0f - epsilon)
                    sideDistance = normalizedY * 0.5f; // Peak is at the bottom edge
                else if (shift <= -1.0f + epsilon)
                    sideDistance = (1.0f - normalizedY) * 0.5f; // Peak is at the top edge
                else
                    sideDistance = Math.Min(normalizedY / (1.0f + shift), (1.0f - normalizedY) / (1.0f - shift));

                float distanceY = widening >= 1.0f ? 0.5f : Math.Min(sideDistance * wideningInverse, 0.5f);

                (distToT1, distToT2) = mode == TransitionDirection.Left
                    ? (Math.Min(distanceY, 1.0f - normalizedX), normalizedX)
                    : (Math.Min(distanceY, normalizedX), 1.0f - normalizedX);
            }
        }
        else
        {
            // ==========================================
            // DIAGONAL CASES
            // ==========================================
            float coordA = normalizedX;
            float coordB = mode == TransitionDirection.DiagonalTopLeft ? normalizedY : 1.0f - normalizedY;
            float averageCoord = (coordA + coordB) * 0.5f;
            float crossSectionWidth = (coordA + coordB) <= 1.0f ? (coordA + coordB) : (2.0f - (coordA + coordB));

            float coordANew, coordBNew;
            if (crossSectionWidth < 0.0001f)
            {
                coordANew = coordA;
                coordBNew = coordB;
            }
            else
            {
                float crossSectionPosition = (coordA - coordB) / crossSectionWidth;
                float shiftedCenter = shift;

                float normalizedshiftedDistance;
                bool isRightOfCenter = crossSectionPosition >= shiftedCenter;

                if (isRightOfCenter)
                {
                    float availableSpace = 1.0f - shiftedCenter;
                    // FIX: If availableSpace is 0, we are AT the center/peak. 
                    // Return 0.0 distance to ensure it's treated as the plateau.
                    normalizedshiftedDistance = availableSpace < epsilon ? 0.0f : (crossSectionPosition - shiftedCenter) / availableSpace;
                }
                else
                {
                    float availableSpace = shiftedCenter - (-1.0f);
                    // FIX: Same logic for the left side
                    normalizedshiftedDistance = availableSpace < epsilon ? 0.0f : (shiftedCenter - crossSectionPosition) / availableSpace;
                }

                normalizedshiftedDistance = Math.Clamp(normalizedshiftedDistance, 0.0f, 1.0f);

                float widenedDistance = widening >= 1.0f
                    ? 0.0f
                    : Math.Max(0.0f, normalizedshiftedDistance - widening) / (1.0f - widening);

                float newCrossSectionPosition = isRightOfCenter ? widenedDistance : -widenedDistance;
                float newDifference = newCrossSectionPosition * crossSectionWidth;
                float offsetFromAverage = newDifference * 0.5f;

                coordANew = averageCoord + offsetFromAverage;
                coordBNew = averageCoord - offsetFromAverage;
            }

            (distToT1, distToT2) = mode == TransitionDirection.DiagonalTopLeft
                ? (Math.Min(1.0f - coordANew, 1.0f - coordBNew), Math.Min(coordANew, coordBNew))
                : (Math.Min(coordANew, coordBNew), Math.Min(1.0f - coordANew, 1.0f - coordBNew));
        }

        // Final Blend Logic
        if (distToT2 <= 0.00001f) return 1.0f;
        if (distToT1 <= 0.00001f) return 0.0f;
        return distToT1 / (distToT1 + distToT2);
    }
}

