using System.Collections.Generic;

namespace Structopedia.Catalog;

/// <summary>
/// One entry of the structure catalog: every schematic of a single folder, from a single origin.
/// </summary>
/// <param name="Key">Folder path the group was built from, empty for files sitting at the root.</param>
/// <param name="Title">Readable name derived from the folder path.</param>
/// <param name="Origin">Who shipped these files.</param>
/// <param name="IsStory">
/// True for the story line content, which is hidden by default so the catalog does not spoil it.
/// </param>
/// <param name="Variants">The files of the group, in natural order.</param>
internal sealed record StructureGroup(
    string Key,
    string Title,
    StructureOrigin Origin,
    bool IsStory,
    IReadOnlyList<StructureVariant> Variants);
