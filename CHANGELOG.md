# Changelog

All notable changes to Structopedia will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Project scaffolding: solution, shared build properties (net10.0, nullable, warnings as errors), StyleCop and .NET analyzers, and a `PackMod` target that writes the Mod DB zip to `release/`.
- Client-only mod system skeleton (`StructopediaModSystem`), which loads on the client side only and logs the mod name and version at startup.
- CI on GitHub Actions: build and unit tests against a cached Vintage Story 1.22.6 server install, on every push and pull request.
- Language assets (`en`, `fr`), shipped in the mod zip. They also carry the `game:handbook-category-structures` key that labels the handbook tab.
- Render spike: a hardcoded handbook page under a new "Structures" tab that draws a worldgen schematic as a 3D mesh inside the page text flow, with an orbital camera (left-drag to rotate, right-drag or wheel to zoom).
- Schematic decoding (`Structopedia.Schematics`): unpacks a `BlockSchematic` into positioned cells, recognising the second half of a waterlogged pair; sorts block codes into visible geometry, worldgen markers and multiblock placeholders; counts the blocks of a structure; buckets cells per layer for the slider view; and finds the blocks that opaque neighbours seal in on all six sides.
- Structure catalog (`Structopedia.Catalog`): groups scanned schematic paths by folder and origin, humanizes the names (`vug-medium1` reads as `Vug medium 1`) and sorts variants naturally, so `ruin-2` stays ahead of `ruin-10`. Story line content sorts last.
- A bounded preview cache (`Structopedia.Caching`) and the mod config POCO (`Structopedia.Config`).
- Integration tests that parse the 701 worldgen schematics the game ships and check the decoding rules against all of them. They skip themselves when `VINTAGE_STORY` points nowhere.
- Structure catalog in the handbook: the client reads the worldgen schematics of the game and of every installed mod straight from the asset origins, and lists them under the "Structures" tab, one page per folder of schematics.
- Structure pages: where the folder comes from, arrows to step through the variants it holds, the size of the one on screen, an orbital 3D preview of it, and the blocks it is built from as clickable icons leading to their own handbook page. A structure too heavy to draw whole is cut off at the vertex budget and says so.
- Settings file (`ModConfig/structopedia.json`), written with its defaults the first time the mod runs: whether the story line structures are listed, and how many vertices a preview may reach.
- Layer slicing: a slider under the preview cuts the structure at a height, so the inside of a ruin can be looked at floor by floor. Drag the handle, click anywhere on the track, or hold Ctrl and scroll over the preview to step one layer at a time. It starts on the whole structure and goes back to it on every new page or variant.
- Real meshes for chiselled blocks, rebuilt from the block entity data the schematic carries and drawn through the same builder the world renderer uses. 540 of the 701 schematics the game ships hold one.
- Multi-structure preview cache: the previews already built stay on the graphics card until they fall out of the cache, so stepping through the variants of a page and back is instant. How many are held is the existing `PreviewCacheSize` setting.
- Build guides: schematics written for the multiblock machines the game makes a player assemble by hand, listed at the top of the "Structures" tab under a "Construction guide" line, with the rules the game checks written out under it. The first three are the cementation furnace, the charcoal pit and the bloomery. Every block and every offset is taken from the 1.22.6 game code, and the tests read the game's own multiblock definition rather than a copy of it.

### Fixed
- Worldgen randomizer blocks are no longer drawn or counted. They swap themselves for one of the blocks they hold while a structure generates, so the pink cubes they show up as, 712 of them across 167 of the 701 schematics the game ships, were never part of any structure.
- Chiselled blocks no longer draw as the placeholder cube of their block type. A chiselled block whose data cannot be used is left out of the preview instead, and so are the clutter blocks, whose shape also only exists in a block entity. Both are still listed among the blocks the structure is built from.
- A structure past the vertex budget now loses its top rather than a scattering of blocks throughout: the budget is spent layer by layer from the ground up, and the page says which layer it reached.
- Large zoomed previews are no longer sliced by the far plane of the handbook. The projection the game sets up for its interface only keeps 152 units of depth behind the page, which a big structure at the zoom ceiling goes straight through, and the cut turned with the camera because the plane faces the viewer rather than the structure. The preview is now drawn far enough forward that its back corner clears the plane whichever way it has been turned.

### Removed
- The render spike page, replaced by the catalog it was written to prove.
