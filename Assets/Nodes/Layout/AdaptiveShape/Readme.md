# C3DLS Adaptive Shape

`C3DLS_AdaptiveShape` is a component-based adaptive geometry generator for the broader Composable 3D Layout System.

It is intended to generate reusable 3D panel and frame-like geometry that is structurally aligned with the layout system’s existing semantics:

- per-axis borders
- per-axis padding
- content-region thinking
- preserved structural bands
- adaptive reshaping under layout changes

Unlike a generic polygon editor, this tool is meant to produce geometry that already fits the language and behavior of the wider layout system. Existing related systems in the project include `SmartBounds`, `BordersPadding`, `BoundsSettings`, adaptive shape files, and resize infrastructure such as `RegionPreservingScaler`, `ResizeArgs`, `ResizeOptions`, and `ResizeSettings`. :contentReference[oaicite:0]{index=0}

---

## Direction

This tool is no longer being treated as a standalone “Shape Stamper” editor window.

The current direction is:

- **component name:** `C3DLS_AdaptiveShape`
- **editing model:** custom inspector
- **editor UI:** foldout visual editors embedded in the inspector
- **major editors:**
  - Shape Editor
  - Profile Editor
  - later: Inner Profile Editor

This system is now considered part of the layout/component workflow rather than a detached window-based authoring tool.

---

## Main purpose

`C3DLS_AdaptiveShape` authors geometry from:

1. **Outer Shape**  
   A 2D closed contour defining the front-face silhouette.

2. **Profile**  
   A 2D cross-sectional profile defining depth, bevel, flange, side-wall, and other structural transitions.

3. **Inner Shape**  
   An optional inner contour that acts as a cutout / hole in the first implementation.

For the initial version, the inner shape is supported only as a hole. It does **not** yet receive its own bevel, shell, or profile treatment. Later, this can be extended with a third foldout editor for an inner profile.

---

## Core insight: regions instead of anchors

One of the key design discoveries from the earlier tool was that **anchors are the wrong abstraction**.

Instead of saying a point is anchored to left / right / center / top / bottom, the better model is:

- a point belongs to a **region**
- a point stores an **interpolation within that region**

This makes the geometry more semantically meaningful and much more compatible with border / padding / content thinking.

### Shape editor region model

The shape editor uses a **3×3 region grid**:

#### X
- Negative
- Middle
- Positive

#### Y
- Positive
- Middle
- Negative

This creates 9 total regions.

### Profile editor region model

The profile editor uses a **2×5 region structure**.

#### X
- Inner = padding edge → content edge
- Outer = content edge → border edge

#### Vertical axis
- Positive Outer = +border → +content
- Positive Inner = +content → +padding
- Center = +padding → -padding
- Negative Inner = -padding → -content
- Negative Outer = -content → -border

This keeps the profile tightly aligned with the wider layout system’s per-axis border / padding / content semantics.

---

## Editing behavior

Points should generally move freely.

When a point is dragged:

1. the user drags in editor space
2. the editor detects which region the point now occupies
3. the point is stored as:
   - region identity
   - normalized interpolation within that region

Points are **not** primarily constrained by anchors.

### Pinning

A useful optional behavior is point pinning.

If a point is marked as pinned:

- it cannot leave its current region
- it may still move within that region by changing interpolation

This allows the user to preserve structural intent without returning to the old anchor-based model.

### Direct interpolation editing

The user should also be able to input normalized interpolated vertex positions directly.

This is especially useful because mirroring is not yet robust. It gives the user a precise way to say things like:

- “put this point at the very start of this region”
- “put this point exactly in the middle of this region”
- “put this point at the end of this region on the Y axis”

So each editable point should support both:

- drag-based editing
- direct numeric interpolation editing

This is important for precision, repeatability, and working around current mirroring limitations.

---

## Geometry goals

The generated geometry should be readable, structured, and material-driven.

Planned semantic material groupings include:

- **Front**
- **Body**
- **Inner Shell**
- **Effects Channels**

The term **Effects Channels** refers to geometry regions that exist partly to serve downstream material-driven effects. The goal is that runtime systems can modulate an effects channel material without needing to know exactly which specific generated geometry strip is being addressed.

This allows cleaner downstream behavior such as:

- pulsing an accent band
- modulating an emissive bevel strip
- fading or highlighting a designated surface family
- styling front/body/effects regions independently

This is preferable to requiring downstream systems to identify specific geometry procedurally after the mesh has already been built.

---

## First-iteration geometry model

The broad geometry model is:

### 1. Outer face polygon
Generate a triangulated 2D face from the outer contour.

If an inner shape exists, subtract it as a hole so the front face contains a cutout.

### 2. Profile-derived depth structure
Interpret the profile as a set of ordered structural bands extending from the front face into 3D.

These bands define things like:

- face thickness
- bevel transitions
- side-wall steps
- body depth
- effect-channel segments

### 3. Contour rings
For each major profile stage:

- generate a contour ring derived from the outer shape
- offset / project / extrude according to that profile stage
- connect adjacent rings with strip geometry

This produces:

- front surface
- side walls
- bevel surfaces
- body segments
- effect-channel surfaces

### 4. Material grouping
Assign generated surfaces into semantic material groups:

- Front
- Body
- Effects Channels
- Inner Shell

### 5. Mesh output
Emit the mesh and assign it to the target GameObject workflow, such as mesh filter / cache / bake path.

---

## First-iteration treatment of the inner shape

The inner shape is included in the first iteration, but only as a hole.

### Supported initially
- inner contour as a cutout in the front face
- hole-aware front triangulation
- preservation of the inner contour as part of the shape definition

### Deferred until later
- separate inner profile
- bevel around the inner contour
- inner shell wall generation
- effects channels on inner contour surfaces

A likely future extension is a third foldout editor:

- **Inner Profile Editor**

That would allow the hole to have its own wall/profile/bevel treatment.

---

## Inspector-first workflow

This tool is intended to use a **custom inspector**, not an editor window.

The expected inspector structure is something like:

- general settings
- shape editor foldout
- profile editor foldout
- later: inner profile editor foldout
- material assignments
- generation / bake controls
- per-point direct interpolation editing where useful

This is preferred because the system is strongly tied to a GameObject and to the wider component-driven layout architecture.

---

## Relationship to the wider layout system

`C3DLS_AdaptiveShape` is intended to align directly with the rest of the Composable 3D Layout System.

Important neighboring systems already present in the project include:

- bounds and border/padding semantics:
  - `SmartBounds.cs`
  - `BordersPadding.cs`
  - `BoundsSettings.cs`

- adaptive and shape-related files:
  - `AdaptiveShape.cs`
  - `AdaptiveShapeBuilder.cs`
  - `AdaptiveShapeSegment.cs`
  - `AdaptiveShapeBuildResult.cs`

- shape/profile/tessellation related files:
  - `ShapeStamperProfileGenerator.cs`
  - `ShapeStamperTessellator.cs`
  - `ShapeStamperBakeService.cs`

- resize/preserved-region infrastructure:
  - `RegionPreservingScaler.cs`
  - `ResizeArgs.cs`
  - `ResizeOptions.cs`
  - `ResizeSettings.cs`
  - `Resizer.cs` :contentReference[oaicite:1]{index=1}

This new system should preserve the useful geometry-side lessons from those tools while replacing the older editor architecture.

---

## Libraries and geometry infrastructure

### GMesh

The project includes the CodeSmile GMesh library, including files such as:

- `GMesh.Euler.SplitEdge.cs`
- `GMesh.Euler.SplitFace.cs`
- `GMesh.Euler.JoinEdges.cs`
- `GMesh.Euler.JoinFaces.cs`
- `GMesh.Transform.cs`
- `GMesh.Unity.Mesh.cs`
- `GMesh.Utility.cs` :contentReference[oaicite:2]{index=2}

This is an important geometry library for the project because it provides a serious route to:

- structured topology edits
- mesh graph operations
- bevel-oriented workflows
- cleaner procedural mesh construction than only raw vertex pushing

Even if the first implementation of `C3DLS_AdaptiveShape` uses simpler mesh construction, GMesh remains a key library worth preserving and reusing.

---

## Data model direction

A minimal serialized point model should look conceptually like this.

### Shape point
- `id`
- `xRegion`
- `yRegion`
- `regionLerp`
- `pinned`

### Profile point
- `id`
- `xRegion`
- `zRegion`
- `regionLerp`
- `pinned`

The user should be able to edit both:

- spatial position by dragging
- normalized interpolation values directly

This keeps the model semantic, compact, and precise.

---

## Suggested generation pipeline

A likely generation pipeline is:

1. read serialized shape/profile definitions from the component
2. resolve editor points into concrete 2D coordinates
3. build the outer contour
4. optionally build the inner contour
5. triangulate the front face with optional hole
6. convert the profile into ordered depth/material bands
7. generate contour rings for each profile stage
8. stitch rings into side/bevel/body/effects surfaces
9. assign triangles into semantic material groups
10. emit mesh
11. assign mesh to the target GameObject workflow

---

## What to preserve from previous work

Even though the earlier editor implementation is being replaced, the following should be preserved:

- the use of GMesh as a serious geometry/topology option
- prior adaptive-shape generation experiments
- profile, tessellation, and bake logic already discovered
- the insight that border / padding / content semantics should drive geometry structure
- the use of semantic material groupings instead of anonymous generated strips
- inner contour support as a hole, even before full inner-profile support

---

## What the first rewrite should support

### First implementation
- `C3DLS_AdaptiveShape` component
- custom inspector
- shape foldout editor
- profile foldout editor
- optional inner shape as a hole
- region-based point storage
- point pinning
- direct interpolation editing
- semantic material grouping
- mesh generation from outer shape + profile

### Later extensions
- inner profile editor
- full inner shell / inner bevel generation
- better mirroring tools
- more advanced guide behavior
- more advanced topology operations through GMesh

---

## Intent statement

`C3DLS_AdaptiveShape` is a component-based adaptive geometry generator for the Composable 3D Layout System. It authors outer shape, profile, and optional inner-hole geometry using region-based editing aligned with existing border / padding / content semantics. Generated surfaces are assigned into semantic material groups such as Front, Body, Inner Shell, and Effects Channels so the result is structurally adaptive and easy to style downstream.

An adaptive shape is designed to serve as a ui element or a ui panel. As a UI panel it can be either concave or convex depending on how the profile is set up. 

---

## Applying and testing these changes locally

Use this checklist to pull the branch, open the project, and validate the new AdaptiveShape material-slot workflow.

### 1) Pull the branch / commit

```bash
git fetch --all
git branch --show-current
# pull whichever branch you are actually using
git pull
# optional: ensure commit is present
git log --oneline -n 5
```

You should see commit `8865260` (`Start AdaptiveShape material-slot model and custom inspector`) and, if pulled, `0a4dec0` (`Document local apply and test workflow for AdaptiveShape changes`).

### 2) Open in Unity

1. Open the project root in Unity Hub (`/workspace/GeomNodes` in this environment).
2. Wait for script compilation to finish.
3. Confirm there are no compiler errors in Console.

### 3) Add and configure an AdaptiveShape

1. In any test scene, create an empty GameObject.
2. Add the `AdaptiveShape` component.
3. In the inspector:
   - (Optional) assign `SmartBounds`.
   - Toggle `Prefer Smart Bounds Borders Padding` depending on your test path.
   - In **Size**, toggle `Use Borders Padding Min As Inner Size` to test bridge behavior.
   - In **Material Slots**, add 2-4 slots and optionally name them (e.g., `Front`, `Body`, `FX`).
   - In **Profile > Edges**, add edges and assign each edge to a slot index with the popup.

### 4) Behavioral checks

Validate these expected behaviors:

- With zero slots, profile rows warn that no slots are available.
- After adding slots, profile edges can select slot indices from popup labels.
- `materialSlotIndex` values stay clamped to valid slot range when slot count changes.
- `GetCurrentInnerSize()` reflects:
  - `BordersPadding.x/y.minContentsSize` when bridge mode is ON.
  - explicit inner size values when bridge mode is OFF.

### 5) Recommended quick regression script check

From project root:

```bash
git diff --check
```

### 6) Optional play mode verification

If your mesh build path is already wired in your local branch, enter Play Mode and ensure runtime systems that consume profile edge channeling now read from slot indices rather than per-edge direct material assumptions.

