# Query plan operator icons from SVG

Every operator in the query plan viewer is drawn with a glyph: path data in
`PlanOperatorGlyphs.cs`, drawn in the symbol colour on a chip in the operator's category colour. An
SVG file in this folder replaces the glyph for one operator. Every other operator keeps its glyph.

## Naming

The file name is the operator's `PlanOperatorKind` (case doesn't matter). The rest of the name picks
how the file is drawn:

| File | Drawn |
|---|---|
| `ComputeScalar.svg` | Recoloured onto the category chip like a glyph, in two tones: black marks in the symbol colour and pure red (`#F00`) marks in the accent colour. So it follows the light and dark themes and fits the rest of the set. |
| `ComputeScalar.color.svg` or `ComputeScalar.colour.svg` | In its own colours, filling the icon's square, with no chip, like an SSMS icon. It does not change with the theme, so check it on a dark background too. |

The `PlanOperatorKind` for each operator is shown on its card in the operator reference. Build
`DBADash.QueryPlan.Reference` and open `DBADashBuild\Reference\QueryPlanOperators.html`.

## Two tones

A recoloured icon is drawn in the chip's two colours, the same two a glyph has. The file says which
mark gets which:

- **Black** (`#000`) is drawn in the symbol colour: white on the chip in the light theme, near black
  in the dark one. White and greys come out in the symbol colour too.
- **Red** (`#F00`) is drawn in the accent colour, yellow in both themes. Keep it for the one part
  that says what the operator did, like the path of an index seek, so a second colour on an icon
  keeps its meaning.

It's a straight mapping of how red a mark is, so other colours come out somewhere between the two.
Stick to black and pure red. The index scan and seek icons here are examples.

Draw on a 24 by 24 `viewBox` to match the glyphs. The icon is drawn in the same inset square a
glyph uses, and thin lines (about 1.5 to 2 units) keep detail readable at node size.

## The files

- Give the SVG a `viewBox`, or a `width` and `height`. It is scaled to fit, keeping its shape.
- Square works best. A tinted icon is drawn in the chip less a small inset; a full-colour one fills
  the chip's square.
- Files are embedded in the assembly when the project builds, so there is nothing to ship alongside.
- Only one file per operator. A file that doesn't load, doesn't name an operator, or clashes with
  another is ignored and the glyph is drawn instead. The reason is listed at the top of the
  operator reference, and the tests fail on it.
