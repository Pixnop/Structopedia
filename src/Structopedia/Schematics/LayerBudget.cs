using System;

namespace Structopedia.Schematics;

/// <summary>
/// Spends a vertex ceiling across the layers of one preview, from the ground up.
/// <para>
/// The ceiling is on the whole structure, not on a layer, because the layers of a preview are drawn
/// together and live on the graphics card together. Once it is spent the build stops: the layers
/// above simply do not exist, which is a truth the slider can tell, rather than a mesh with holes in
/// it that it cannot.
/// </para>
/// </summary>
internal sealed class LayerBudget
{
    private bool _exhausted;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayerBudget"/> class with nothing spent.
    /// </summary>
    /// <param name="maxVertices">Vertex ceiling of the whole preview, at least one.</param>
    internal LayerBudget(int maxVertices)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxVertices, 1);

        MaxVertices = maxVertices;
    }

    /// <summary>Vertex ceiling the preview may not go past.</summary>
    internal int MaxVertices { get; }

    /// <summary>Vertices handed out so far.</summary>
    internal int Used { get; private set; }

    /// <summary>True once the budget has turned something down, which ends the build.</summary>
    internal bool Exhausted => _exhausted;

    /// <summary>Layer the budget first gave up on, or null while it still has room.</summary>
    internal int? StoppedAtLayer { get; private set; }

    /// <summary>
    /// Asks for room for one more piece of geometry.
    /// </summary>
    /// <param name="layer">Layer the geometry belongs to, remembered when the answer is no.</param>
    /// <param name="vertices">Vertices it would add.</param>
    /// <returns>True when it fits and was charged, false once the budget is spent.</returns>
    internal bool TryAdd(int layer, int vertices)
    {
        if (_exhausted)
        {
            return false;
        }

        // The very first piece always goes in: a preview showing nothing reads as a broken structure
        // rather than as a heavy one, whatever the ceiling was set to.
        if (Used > 0 && Used + vertices > MaxVertices)
        {
            _exhausted = true;
            StoppedAtLayer = layer;
            return false;
        }

        Used += vertices;
        return true;
    }
}
