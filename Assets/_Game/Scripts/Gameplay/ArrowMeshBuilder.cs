// ============================================================
// ArrowMeshBuilder.cs  –  Arrow Slide Puzzle
// Builds a smooth rounded-stroke mesh for a snake arrow.
//
// Algorithm:
//   1. Convert cell centres to world positions.
//   2. Walk each segment and round the corners (arc inserts).
//      Corner radius = ArrowVisuals.ComputeCornerRadius(halfWidth, cellSize),
//      clamped to 0.45 × cellSize so bends never exceed their cell.
//   3. Extrude a quad-strip of given half-width with rounded end-caps.
//   4. Build an OPEN CHEVRON arrowhead (two thick rounded stroke arms
//      forming a ">" / "V" shape) — same thickness and colour as the body.
//      Cap vertices ≥ 8, corner vertices ≥ 8.
//
// All geometry is in LOCAL space (ArrowView's transform at origin).
// Visual unit U = Clamp(cellSize, UNIT_MIN, UNIT_MAX) decouples thickness
// from raw cell size so arrows look consistent on sparse and dense levels.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace ArrowPuzzle.Gameplay
{
    public static class ArrowMeshBuilder
    {
        // ── Public entry points ────────────────────────────────

        /// <summary>
        /// Builds (or rebuilds) the body mesh of the arrow.
        /// worldPoints: cell-centre world positions, tail→head.
        /// halfWidth  : half the stroke width in world units (from ArrowVisuals.ComputeHalfWidth).
        /// cornerSteps: how many arc segments per bend (higher = smoother, min 8).
        /// cellSize   : raw grid cell size, used to clamp the corner radius.
        ///              Pass 0 or omit to skip clamping (uses halfWidth-only radius).
        /// </summary>
        public static void BuildBodyMesh(
            Mesh mesh,
            List<Vector3> worldPoints,
            float halfWidth,
            int cornerSteps = 10,
            float cellSize  = 0f)
        {
            if (mesh == null || worldPoints == null || worldPoints.Count < 2)
            {
                mesh?.Clear();
                return;
            }

            // Enforce minimum 8 segments for smooth curves
            int steps = Mathf.Max(cornerSteps, 8);

            // 1. Build the smooth centreline (with rounded corners)
            var centreline = BuildCentreline(worldPoints, halfWidth, steps, cellSize);

            // 2. Extrude the centreline into a quad-strip
            var verts    = new List<Vector3>();
            var uvs      = new List<Vector2>();
            var tris     = new List<int>();
            var colors   = new List<Color32>();

            // We'll set UV.x as arc-distance for potential gradient mapping
            float totalLen = 0f;
            for (int i = 1; i < centreline.Count; i++)
                totalLen += Vector3.Distance(centreline[i - 1], centreline[i]);
            if (totalLen < 0.0001f) totalLen = 1f;

            float dist = 0f;
            for (int i = 0; i < centreline.Count; i++)
            {
                // Tangent at this point
                Vector3 tangent;
                if (i == 0)
                    tangent = (centreline[1] - centreline[0]).normalized;
                else if (i == centreline.Count - 1)
                    tangent = (centreline[i] - centreline[i - 1]).normalized;
                else
                    tangent = (centreline[i + 1] - centreline[i - 1]).normalized;

                Vector3 normal = new Vector3(-tangent.y, tangent.x, 0f);

                verts.Add(centreline[i] + normal * halfWidth);
                verts.Add(centreline[i] - normal * halfWidth);

                float u = dist / totalLen;
                uvs.Add(new Vector2(u, 1f));
                uvs.Add(new Vector2(u, 0f));

                colors.Add(Color.white);
                colors.Add(Color.white);

                if (i > 0)
                    dist += Vector3.Distance(centreline[i], centreline[i - 1]);
            }

            // Triangles for the quad strip (double-sided for reliable 2D rendering)
            for (int i = 0; i < centreline.Count - 1; i++)
            {
                int b = i * 2;
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                tris.Add(b + 1); tris.Add(b + 2); tris.Add(b + 3);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                tris.Add(b + 1); tris.Add(b + 3); tris.Add(b + 2);
            }

            // 3. Add rounded end-cap at TAIL only (8+ cap segments = smooth semicircle).
            //    HEAD end is left flat — the chevron head covers it.
            AddSemiCircleCap(verts, uvs, tris, colors,
                centreline[0],
                GetTangent(centreline, 0).normalized,
                halfWidth, steps, capAtHead: false);

            // 4. Commit to mesh
            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Builds (or rebuilds) the arrowhead mesh.
        /// tipWorld  : world position of the arrow tip.
        /// direction : unit direction the head points.
        /// halfWidth : same halfWidth as the body.
        /// </summary>
        public static void BuildHeadMesh(
            Mesh mesh,
            Vector3 tipWorld,
            Vector3 direction,
            float halfWidth)
        {
            if (mesh == null) return;
            BuildFilledTriangleHeadMesh(mesh, tipWorld, direction, halfWidth);
        }

        // ── Private: Chevron (open V) head ────────────────────

        /// <summary>
        /// Open chevron: two thick rounded-stroke arms of the same width as the line
        /// forming an open ">" / "V" shape with rounded tips and clear direction.
        /// </summary>
        private static void BuildChevronHeadMesh(
            Mesh mesh,
            Vector3 tipWorld,
            Vector3 direction,
            float halfWidth,
            float cellSize = 0f)
        {
            float strokeWidth = halfWidth * 2f;
            float cell = cellSize > 0.001f ? cellSize : strokeWidth / ArrowPuzzle.Core.ArrowVisuals.STROKE_RATIO;

            // Head size about 1.2 cell
            float headSize = cell * 1.15f;
            float spread   = headSize * 0.55f; // half-spread (total width ~1.1-1.2 cell)
            float depth    = headSize * 0.65f; // depth along direction
            float armHalfW = halfWidth;        // EXACTLY the same width as the body line!

            Vector3 right = new Vector3(-direction.y, direction.x, 0f); // perpendicular

            // Arm endpoints: tip is shared; arms fan backward by depth and outward by spread
            Vector3 tip   = tipWorld;
            Vector3 tailR = tipWorld - direction * depth + right * spread;
            Vector3 tailL = tipWorld - direction * depth - right * spread;

            // Build both arms as thick quad strips with rounded end-caps
            var verts  = new List<Vector3>();
            var uvs    = new List<Vector2>();
            var tris   = new List<int>();
            var colors = new List<Color32>();

            int capSteps = Mathf.Max(8, ArrowPuzzle.Core.ArrowVisuals.CornerSteps);

            BuildArmMesh(verts, uvs, tris, colors, tip, tailR, armHalfW, capSteps);
            BuildArmMesh(verts, uvs, tris, colors, tip, tailL, armHalfW, capSteps);

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Builds a single chevron arm as a thick rounded-cap stroke
        /// from startPt to endPt with given half-width.
        /// Appends into the shared lists.
        /// </summary>
        private static void BuildArmMesh(
            List<Vector3> verts, List<Vector2> uvs, List<int> tris, List<Color32> colors,
            Vector3 startPt, Vector3 endPt, float armHalfW, int capSteps)
        {
            Vector3 dir     = (endPt - startPt);
            float   length  = dir.magnitude;
            if (length < 0.0001f) return;
            Vector3 unitDir = dir / length;
            Vector3 norm    = new Vector3(-unitDir.y, unitDir.x, 0f);

            int baseIdx = verts.Count;

            // Two-point quad strip (start and end)
            verts.Add(startPt + norm * armHalfW);
            verts.Add(startPt - norm * armHalfW);
            verts.Add(endPt   + norm * armHalfW);
            verts.Add(endPt   - norm * armHalfW);

            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0f));

            for (int i = 0; i < 4; i++) colors.Add(Color.white);

            // Quad triangles (double-sided)
            tris.Add(baseIdx);     tris.Add(baseIdx + 2); tris.Add(baseIdx + 1);
            tris.Add(baseIdx + 1); tris.Add(baseIdx + 2); tris.Add(baseIdx + 3);
            tris.Add(baseIdx);     tris.Add(baseIdx + 1); tris.Add(baseIdx + 2);
            tris.Add(baseIdx + 1); tris.Add(baseIdx + 3); tris.Add(baseIdx + 2);

            // Rounded end cap at the tail (endPt) so arm tips look rounded
            AddSemiCircleCap(verts, uvs, tris, colors,
                endPt, unitDir, armHalfW, capSteps, capAtHead: false);

            // Small rounded cap at tip (startPt) to avoid a sharp junction
            AddSemiCircleCap(verts, uvs, tris, colors,
                startPt, -unitDir, armHalfW, capSteps / 2, capAtHead: false);
        }

        // ── Private: Legacy filled-triangle head (kept as fallback) ───

        private static void BuildFilledTriangleHeadMesh(
            Mesh mesh,
            Vector3 tipWorld,
            Vector3 direction,
            float halfWidth)
        {
            float strokeWidth = halfWidth * 2f;
            float headLen     = strokeWidth * ArrowPuzzle.Core.ArrowVisuals.HEAD_LENGTH_RATIO;
            float headHalf    = strokeWidth * ArrowPuzzle.Core.ArrowVisuals.HEAD_WIDTH_RATIO * 0.5f;
            float headWidth   = headHalf * 2f;

            float overlap = halfWidth * 0.45f;
            Vector3 right = new Vector3(-direction.y, direction.x, 0f);

            Vector3 tip        = tipWorld;
            Vector3 baseCenter = tipWorld - direction * (headLen + overlap);
            Vector3 baseR      = baseCenter - right * headHalf;
            Vector3 baseL      = baseCenter + right * headHalf;

            Vector3[] corners = { tip, baseR, baseL };
            float roundR = headWidth * ArrowPuzzle.Core.ArrowVisuals.HEAD_CORNER_ROUNDING;

            var polygon = new List<Vector3>();

            if (roundR > 0.001f)
            {
                int cSteps = 3;
                for (int i = 0; i < 3; i++)
                {
                    Vector3 prev = corners[(i + 2) % 3];
                    Vector3 curr = corners[i];
                    Vector3 next = corners[(i + 1) % 3];

                    Vector3 inVec  = curr - prev;
                    Vector3 outVec = next - curr;

                    float inDist  = inVec.magnitude;
                    float outDist = outVec.magnitude;

                    Vector3 inDir  = inVec / inDist;
                    Vector3 outDir = outVec / outDist;

                    float dIn  = Mathf.Min(roundR, inDist * 0.22f);
                    float dOut = Mathf.Min(roundR, outDist * 0.22f);

                    Vector3 arcStart = curr - inDir * dIn;
                    Vector3 arcEnd   = curr + outDir * dOut;

                    for (int s = 0; s <= cSteps; s++)
                    {
                        float t = (float)s / cSteps;
                        polygon.Add(Bezier(arcStart, curr, arcEnd, t));
                    }
                }
            }
            else
            {
                polygon.Add(tip);
                polygon.Add(baseR);
                polygon.Add(baseL);
            }

            Vector3 centroid = (tip + baseR + baseL) / 3f;
            var verts  = new List<Vector3> { centroid };
            var uvs    = new List<Vector2> { new Vector2(0.5f, 0.5f) };
            var colors = new List<Color32> { Color.white };
            var tris   = new List<int>();

            float totalHeadH = headLen + overlap;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector3 p = polygon[i];
                verts.Add(p);

                float u = Vector3.Dot(p - baseCenter, right) / headWidth + 0.5f;
                float v = Vector3.Dot(p - baseCenter, direction) / totalHeadH;
                uvs.Add(new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v)));
                colors.Add(Color.white);
            }

            int n = polygon.Count;
            for (int i = 1; i <= n; i++)
            {
                int next = (i == n) ? 1 : i + 1;
                tris.Add(0); tris.Add(i); tris.Add(next);
                tris.Add(0); tris.Add(next); tris.Add(i);
            }

            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        // ── Private helpers ────────────────────────────────────

        /// <summary>Builds a smooth centreline with rounded corners (bend radius 0.5–0.8 cell).</summary>
        private static List<Vector3> BuildCentreline(
            List<Vector3> pts, float halfWidth, int cornerSteps, float cellSize = 0f)
        {
            var result = new List<Vector3>();

            // Corner radius: CORNER_RADIUS_RATIO × strokeWidth, clamped to 0.45 × cellSize.
            // Target: 0.5–0.8 cell radius — CORNER_RADIUS_RATIO = 0.72, so at full cellSize
            // that gives 0.72 × (0.57 × cellSize) ≈ 0.41 cell, capped at 0.45 × cellSize.
            float r;
            if (cellSize > 0.0001f)
                r = ArrowPuzzle.Core.ArrowVisuals.ComputeCornerRadius(halfWidth, cellSize);
            else
                r = halfWidth * ArrowPuzzle.Core.ArrowVisuals.CornerRadiusFraction;

            for (int i = 0; i < pts.Count; i++)
            {
                bool hasPrev = i > 0;
                bool hasNext = i < pts.Count - 1;

                if (!hasPrev || !hasNext)
                {
                    result.Add(pts[i]);
                    continue;
                }

                Vector3 prev = pts[i - 1];
                Vector3 curr = pts[i];
                Vector3 next = pts[i + 1];

                Vector3 inDir  = (curr - prev).normalized;
                Vector3 outDir = (next - curr).normalized;

                // If segments are parallel, no corner needed
                if (Vector3.Dot(inDir, outDir) > 0.9999f)
                {
                    result.Add(curr);
                    continue;
                }

                // Pull-back distance so the arc fits within both adjacent segments (large smooth bend)
                float pullback = Mathf.Min(r,
                    Vector3.Distance(prev, curr) * 0.48f,
                    Vector3.Distance(curr, next) * 0.48f);

                Vector3 arcStart = curr - inDir  * pullback;
                Vector3 arcEnd   = curr + outDir * pullback;

                // Quadratic Bézier arc: arcStart → corner point → arcEnd
                for (int s = 0; s <= cornerSteps; s++)
                {
                    float t = s / (float)cornerSteps;
                    Vector3 p = Bezier(arcStart, curr, arcEnd, t);
                    result.Add(p);
                }
            }

            return result;
        }

        private static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private static Vector3 GetTangent(List<Vector3> pts, int i)
        {
            if (pts.Count < 2) return Vector3.right;
            if (i == 0)                   return (pts[1] - pts[0]).normalized;
            if (i == pts.Count - 1)       return (pts[i] - pts[i - 1]).normalized;
            return (pts[i + 1] - pts[i - 1]).normalized;
        }

        private static void AddSemiCircleCap(
            List<Vector3> verts, List<Vector2> uvs,
            List<int> tris, List<Color32> colors,
            Vector3 centre, Vector3 tangent,
            float halfWidth, int steps, bool capAtHead)
        {
            // Enforce min 8 segments for smooth rounded caps
            int capSteps = Mathf.Max(steps, 8);

            // Outward normal from the cap endpoint
            Vector3 capDir = capAtHead ? tangent : -tangent;
            Vector3 right  = new Vector3(-capDir.y, capDir.x, 0f);

            int centreIdx = verts.Count;
            verts.Add(centre);
            uvs.Add(new Vector2(0.5f, 0.5f));
            colors.Add(Color.white);

            int firstRing = verts.Count;
            for (int s = 0; s <= capSteps; s++)
            {
                float angle = Mathf.PI * s / capSteps;    // 0..π
                float cs = Mathf.Cos(angle);
                float sn = Mathf.Sin(angle);
                Vector3 p = centre + right * (cs * halfWidth) + capDir * (sn * halfWidth);
                verts.Add(p);
                uvs.Add(new Vector2(cs * 0.5f + 0.5f, sn * 0.5f));
                colors.Add(Color.white);
            }

            for (int s = 0; s < capSteps; s++)
            {
                // Double-sided fan triangles
                tris.Add(centreIdx);
                tris.Add(firstRing + s + 1);
                tris.Add(firstRing + s);

                tris.Add(centreIdx);
                tris.Add(firstRing + s);
                tris.Add(firstRing + s + 1);
            }
        }
    }
}

