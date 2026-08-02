using System;

namespace Structopedia.Schematics;

/// <summary>
/// Maps between the layer a preview is cut at and the pixel the slider handle sits on.
/// <para>
/// The range runs over every layer from the lowest to the highest, empty ones included. A cut is a
/// height, not a position in a list of layers that happen to hold something, so asking for layer 7
/// has to mean layer 7 even when the schematic put no block there.
/// </para>
/// </summary>
internal static class LayerCutoff
{
    /// <summary>Bounds a layer to the range a structure spans.</summary>
    /// <param name="layer">Layer to bound.</param>
    /// <param name="minLayer">Lowest layer of the structure.</param>
    /// <param name="maxLayer">Highest layer of the structure.</param>
    /// <returns>The layer, pulled inside the range.</returns>
    internal static int Clamp(int layer, int minLayer, int maxLayer)
        => layer < minLayer ? minLayer : layer > maxLayer ? maxLayer : layer;

    /// <summary>
    /// Reads the layer a point on the slider track stands for. Points off the track clamp to the end
    /// they are past, so a drag that runs off the widget still tracks the pointer.
    /// </summary>
    /// <param name="x">Pointer position along the track.</param>
    /// <param name="trackX">Left end of the track.</param>
    /// <param name="trackWidth">Width of the track. A track of no width reads as the whole structure.</param>
    /// <param name="minLayer">Lowest layer of the structure.</param>
    /// <param name="maxLayer">Highest layer of the structure.</param>
    /// <returns>The layer to cut at.</returns>
    internal static int FromPixel(double x, double trackX, double trackWidth, int minLayer, int maxLayer)
    {
        int span = maxLayer - minLayer;
        if (span <= 0 || trackWidth <= 0.0)
        {
            return maxLayer;
        }

        double along = (x - trackX) / trackWidth;
        along = along < 0.0 ? 0.0 : along > 1.0 ? 1.0 : along;

        return minLayer + (int)Math.Round(along * span, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Places the slider handle for a layer. A structure of a single layer parks the handle at the
    /// far end, since its view is always whole.
    /// </summary>
    /// <param name="layer">Layer the preview is cut at.</param>
    /// <param name="trackX">Left end of the track.</param>
    /// <param name="trackWidth">Width of the track.</param>
    /// <param name="minLayer">Lowest layer of the structure.</param>
    /// <param name="maxLayer">Highest layer of the structure.</param>
    /// <returns>The position of the handle along the track.</returns>
    internal static double ToPixel(int layer, double trackX, double trackWidth, int minLayer, int maxLayer)
    {
        int span = maxLayer - minLayer;
        if (span <= 0)
        {
            return trackX + trackWidth;
        }

        return trackX + ((double)(Clamp(layer, minLayer, maxLayer) - minLayer) / span * trackWidth);
    }

    /// <summary>
    /// Moves a cutoff by whole layers, bounded by the range. A cutoff left over from another
    /// structure is pulled back in on the way.
    /// </summary>
    /// <param name="layer">Layer the preview is cut at.</param>
    /// <param name="delta">Layers to move by, signed.</param>
    /// <param name="minLayer">Lowest layer of the structure.</param>
    /// <param name="maxLayer">Highest layer of the structure.</param>
    /// <returns>The new cutoff.</returns>
    internal static int Step(int layer, int delta, int minLayer, int maxLayer)
        => Clamp(Clamp(layer, minLayer, maxLayer) + delta, minLayer, maxLayer);
}
