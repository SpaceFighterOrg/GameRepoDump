using Backend.Common.DTOs.Map;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MVVM.Views.GalaxyMap
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class PlanetDonutFactionView : ViewBase
    {
        [Header("Ring Shape")]
        [SerializeField] private float outerRadius = 0.6f;
        [SerializeField] private float innerRadius = 0.45f;
        [SerializeField] private float gapDegrees = 3f;
        [SerializeField] private int arcResolution = 40;

        [Header("Outline")]
        [SerializeField] private bool showOutline = true;
        [SerializeField] private float outlineWidth = 0.03f;
        [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, 1f);

        [Header("Sorting")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 10;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;

        private MeshFilter _outlineMeshFilter;
        private MeshRenderer _outlineMeshRenderer;
        private Mesh _outlineMesh;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _mesh = new Mesh { name = "PlanetDonut" };
            _meshFilter.mesh = _mesh;

            _meshRenderer.sortingLayerName = sortingLayerName;
            _meshRenderer.sortingOrder = sortingOrder;

            if (_meshRenderer.sharedMaterial == null)
                _meshRenderer.material = MakeMaterial();

            if (showOutline)
            {
                var outlineGO = new GameObject("DonutOutline");
                outlineGO.transform.SetParent(transform, false);
                outlineGO.transform.localPosition = Vector3.zero;
                outlineGO.transform.localRotation = Quaternion.identity;
                outlineGO.transform.localScale = Vector3.one;

                _outlineMeshFilter = outlineGO.AddComponent<MeshFilter>();
                _outlineMeshRenderer = outlineGO.AddComponent<MeshRenderer>();

                _outlineMesh = new Mesh { name = "PlanetDonutOutline" };
                _outlineMeshFilter.mesh = _outlineMesh;

                _outlineMeshRenderer.sortingLayerName = sortingLayerName;
                _outlineMeshRenderer.sortingOrder = sortingOrder - 1;

                _outlineMeshRenderer.material = MakeMaterial();
            }
        }

        public void Refresh(List<PlanetBattleStatsDTO> battleStats, FactionRegistry factionRegistry)
        {
            var segments = BuildSegments(battleStats, factionRegistry);
            BuildMesh(segments);

            if (showOutline && _outlineMesh != null)
                BuildOutlineMesh(segments);
        }

        private struct Segment
        {
            public Color Color;
            public float Fraction;
        }

        private List<Segment> BuildSegments(List<PlanetBattleStatsDTO> stats, FactionRegistry factionRegistry)
        {
            var result = new List<Segment>();
            if (stats == null || stats.Count == 0) return result;

            float total = 0f;
            foreach (var s in stats) total += Mathf.Max(0f, s.ControlPoints);
            if (total <= 0f) return result;

            foreach (var stat in stats)
            {
                float pts = Mathf.Max(0f, stat.ControlPoints);
                if (pts <= 0f) continue;

                var cfg = factionRegistry.Get(stat.FactionId);
                result.Add(new Segment
                {
                    Color = cfg != null ? cfg.Color : Color.gray,
                    Fraction = pts / total,
                });
            }

            result.Sort((a, b) => b.Fraction.CompareTo(a.Fraction));
            return result;
        }

        private void BuildMesh(List<Segment> segments)
        {
            _mesh.Clear();
            if (segments.Count == 0) return;

            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            float gapRad = gapDegrees * Mathf.Deg2Rad;
            float startAngle = Mathf.PI * 0.5f;

            foreach (var seg in segments)
            {
                float sweepRad = seg.Fraction * Mathf.PI * 2f - gapRad;
                if (sweepRad <= 0f) { startAngle += seg.Fraction * Mathf.PI * 2f; continue; }

                AppendAnnularArc(
                    vertices, colors, triangles,
                    startAngle, sweepRad, outerRadius, 
                    innerRadius, seg.Color);

                startAngle += seg.Fraction * Mathf.PI * 2f;
            }

            ApplyMesh(_mesh, vertices, colors, triangles);
        }

        private void BuildOutlineMesh(List<Segment> segments)
        {
            _outlineMesh.Clear();
            if (segments.Count == 0) return;

            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            float gapRad = gapDegrees * Mathf.Deg2Rad;
            float startAngle = Mathf.PI * 0.5f;

            float outerO = outerRadius + outlineWidth;
            float innerO = Mathf.Max(0f, innerRadius - outlineWidth);

            foreach (var seg in segments)
            {
                float sweepRad = seg.Fraction * Mathf.PI * 2f - gapRad;
                if (sweepRad <= 0f) { startAngle += seg.Fraction * Mathf.PI * 2f; continue; }

                AppendAnnularArc(
                    vertices, colors, triangles,
                    startAngle, sweepRad, outerO, 
                    innerO, outlineColor);

                startAngle += seg.Fraction * Mathf.PI * 2f;
            }

            ApplyMesh(_outlineMesh, vertices, colors, triangles);
        }

        private void AppendAnnularArc(
            List<Vector3> vertices, List<Color> colors, List<int> triangles,
            float startAngle, float sweepRad,
            float outerR, float innerR,
            Color color)
        {
            int steps = Mathf.Max(2, Mathf.RoundToInt(arcResolution * (sweepRad / (Mathf.PI * 2f))));
            int baseIndex = vertices.Count;

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float ang = startAngle + t * sweepRad;
                float cos = Mathf.Cos(ang);
                float sin = Mathf.Sin(ang);

                vertices.Add(new Vector3(cos * outerR, sin * outerR, 0f));
                vertices.Add(new Vector3(cos * innerR, sin * innerR, 0f));
                colors.Add(color);
                colors.Add(color);
            }

            for (int i = 0; i < steps; i++)
            {
                int a = baseIndex + i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;

                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }
        }

        private static void ApplyMesh(Mesh mesh, List<Vector3> verts, List<Color> cols, List<int> tris)
        {
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
        }

        private static Material MakeMaterial()
        {
            Shader shader = Shader.Find("Custom/VertexColorUnlit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Unlit/Color");
            return new Material(shader);
        }
    }
}