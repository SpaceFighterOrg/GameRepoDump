using Assets.Scripts.Interfaces;
using Backend.Common.DTOs.Map;
using GenericEventSystem;
using GenericEventSystem.EventData;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assets.Scripts.MVVM.Views.GalaxyMap
{
    [AddComponentMenu("Galaxy/Galaxy Background Generator")]
    public class GalaxyBackgroundGenerator : ViewBase, IPlanetPicker
    {
        [SerializeField] private EventDefinition _planetSelectedChannel;

        [Header("Grid Dimensions")]
        [SerializeField] private int _columns = 30;
        [SerializeField] private int _rows = 20;
        [SerializeField] private float _gridOffsetZ = -10f;

        [Header("Triangle Geometry")]
        [Tooltip("Side length of each equilateral triangle (world units).")]
        [SerializeField] private float _sideLength = 1f;
        [Tooltip("Gap between adjacent triangle edges (world units).")]
        [SerializeField] private float _triangleSpacing = 0.04f;

        [Header("Base Extrusion (Noise) – calm")]
        [SerializeField] private float _noiseScale = 0.18f;
        [SerializeField] private float _noiseSpeed = 0.08f;
        [SerializeField] private float _baseExtrusionMin = 0.05f;
        [SerializeField] private float _baseExtrusionMax = 0.55f;

        [Header("Battle Extrusion (Turbulence)")]
        [SerializeField] private float _battleExtrusionMin = 0.40f;
        [SerializeField] private float _battleExtrusionMax = 1.40f;
        [SerializeField] private float _battleNoiseSpeed = 0.55f;
        [SerializeField] private float _battleNoiseScale = 0.45f;

        [Header("Rendering")]
        [SerializeField] private Material _instancedMaterial;
        [Tooltip("Material used for selected-province triangles. Leave null to share _instancedMaterial.")]
        [SerializeField] private Material _selectedMaterial;

        [Header("Selection Pulse")]
        [Tooltip("Oscillations per second for the faction-colour → white pulse on selected cells.")]
        [SerializeField] private float _selectedPulseSpeed = 1.5f;

        [Header("Faction")]
        [SerializeField] private FactionRegistry _factionRegistry;

        [Header("Colour")]
        [Tooltip("Alpha applied to faction colour for unselected cells.")]
        [Range(0f, 1f)]
        [SerializeField] private float _provinceAlpha = 0.35f;
        [Tooltip("Alpha applied to faction colour for selected cells.")]
        [Range(0f, 1f)]
        [SerializeField] private float _selectedAlpha = 0.85f;

        private int _totalCells;
        private Matrix4x4[] _baseMatrices;   
        private Vector3[] _cellPositions;  
        private int[] _factionIds;
        private int[] _planetIds;
        private bool[] _isBattle;
        private float[] _noiseOffsets;   
        private float[] _cellPlanetDistSq;

        private List<MapPlanetDTO> _planets = new List<MapPlanetDTO>();
        private int _selectedPlanetId = -1;
        private Mesh _triangleMesh;
        private const int k_MaxBatchSize = 1023;
        private Matrix4x4[] _batchScratch = new Matrix4x4[k_MaxBatchSize];

        private MaterialPropertyBlock _mpb;

        private static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor"); // URP/HDRP
        private static readonly int k_ColorId = Shader.PropertyToID("_Color");     // Built-in

        private Vector3 _sortCamPos;
        private Comparison<Matrix4x4> _depthComparison;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _depthComparison = CompareByDepth;

            _triangleMesh = BuildTriangleMesh(_sideLength);
            RebuildGrid();
        }

        public void OnPlanetSelected(EventData eventData)
        {
            var data = eventData as PlanetIdEventData;
            _selectedPlanetId = data.PlanetId;
        }

        public int GetPlanetIdAtWorldPos(Vector3 worldPos)
        {
            if (_planets == null || _planets.Count == 0) return -1;

            int best = -1;
            float bestDsq = float.MaxValue;

            foreach (MapPlanetDTO p in _planets)
            {
                float dx = p.X - worldPos.x;
                float dy = p.Y - worldPos.y;
                float dsq = dx * dx + dy * dy;
                if (dsq < bestDsq) { bestDsq = dsq; best = p.Id; }
            }

            return best;
        }

        public void HandleGalaxyChangedEvent(EventData eventData)
        {
            var data = eventData as GalaxyChangedEventData;
            if (data?.Galaxy == null) return;
            _planets = data.Galaxy.Planets ?? new List<MapPlanetDTO>();
            RebuildVoronoi();
        }

        private void RebuildGrid()
        {
            _totalCells = _rows * _columns;

            _baseMatrices = new Matrix4x4[_totalCells];
            _cellPositions = new Vector3[_totalCells];
            _factionIds = new int[_totalCells];
            _planetIds = new int[_totalCells];
            _isBattle = new bool[_totalCells];
            _noiseOffsets = new float[_totalCells];
            _cellPlanetDistSq = new float[_totalCells];

            float h = Mathf.Sqrt(3f) / 2f * _sideLength;               // triangle height
            float colStep = _sideLength * 0.5f + _triangleSpacing;            // horizontal step
            float rowStep = h + _triangleSpacing * (Mathf.Sqrt(3f) / 2f);    // vertical step

            float totalWidth = (_columns - 1) * colStep;
            float totalHeight = (_rows - 1) * rowStep;

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    int idx = row * _columns + col;
                    bool pointDown = (row + col) % 2 == 1;

                    float x = col * colStep - totalWidth * 0.5f;
                    float y = row * rowStep - totalHeight * 0.5f;

                    // Point-up triangles sit slightly lower so rows interlock cleanly.
                    if (!pointDown) y -= h * 0.333f;

                    Vector3 pos = new Vector3(x, y, _gridOffsetZ);
                    Quaternion rot = pointDown
                        ? Quaternion.Euler(0f, 0f, 180f)
                        : Quaternion.identity;

                    _baseMatrices[idx] = Matrix4x4.TRS(pos, rot, Vector3.one);
                    _cellPositions[idx] = pos;
                    _noiseOffsets[idx] = Random.Range(0f, 100f);
                }
            }

            RebuildVoronoi();
        }

        private void RebuildVoronoi()
        {
            bool hasPlanets = _planets != null && _planets.Count > 0;

            for (int i = 0; i < _totalCells; i++)
            {
                if (!hasPlanets)
                {
                    _planetIds[i] = -1;
                    _factionIds[i] = -1;
                    _isBattle[i] = false;
                    _cellPlanetDistSq[i] = float.MaxValue;
                    continue;
                }

                Vector3 cp = _cellPositions[i];
                int bestId = -1;
                int bestFac = -1;
                bool battle = false;
                float bestDsq = float.MaxValue;

                foreach (MapPlanetDTO p in _planets)
                {
                    float dx = p.X - cp.x;
                    float dy = p.Y - cp.y;
                    float dsq = dx * dx + dy * dy;

                    if (dsq < bestDsq)
                    {
                        bestDsq = dsq;
                        bestId = p.Id;
                        bestFac = p.FactionId;
                        battle = p.BattleStats != null && p.BattleStats.Count > 0;
                    }
                }

                _planetIds[i] = bestId;
                _factionIds[i] = bestFac;
                _isBattle[i] = battle;
                _cellPlanetDistSq[i] = bestDsq;
            }
        }

        private readonly Dictionary<Color, List<Matrix4x4>> _normalBuckets =
            new Dictionary<Color, List<Matrix4x4>>(32);
        private readonly Dictionary<Color, List<Matrix4x4>> _selectedBuckets =
            new Dictionary<Color, List<Matrix4x4>>(32);

        private void Update()
        {
            RenderFrame();
        }

        private void RenderFrame()
        {
            if (_instancedMaterial == null) return;

            float t = Time.time;

            foreach (var l in _normalBuckets.Values) l.Clear();
            foreach (var l in _selectedBuckets.Values) l.Clear();

            Material normalMat = _instancedMaterial;
            Material selectedMat = _selectedMaterial != null ? _selectedMaterial : _instancedMaterial;

            for (int i = 0; i < _totalCells; i++)
            {
                float extrusion;
                if (_isBattle[i])
                {
                    float nx = _cellPositions[i].x * _battleNoiseScale + _noiseOffsets[i];
                    float ny = _cellPositions[i].y * _battleNoiseScale + _noiseOffsets[i] + 50f;
                    float nv = Mathf.PerlinNoise(nx + t * _battleNoiseSpeed,
                                                 ny + t * _battleNoiseSpeed);
                    extrusion = Mathf.Lerp(_battleExtrusionMin, _battleExtrusionMax, nv);
                }
                else
                {
                    float nx = _cellPositions[i].x * _noiseScale + _noiseOffsets[i];
                    float ny = _cellPositions[i].y * _noiseScale + _noiseOffsets[i] + 50f;
                    float nv = Mathf.PerlinNoise(nx + t * _noiseSpeed,
                                                 ny + t * _noiseSpeed);
                    extrusion = Mathf.Lerp(_baseExtrusionMin, _baseExtrusionMax, nv);
                }

                Matrix4x4 m = _baseMatrices[i] * Matrix4x4.Scale(new Vector3(1f, 1f, extrusion));

                bool isSelected = _selectedPlanetId >= 0 && _planetIds[i] == _selectedPlanetId;
                Color c = GetCellColor(i, isSelected, t);

                Dictionary<Color, List<Matrix4x4>> dict = isSelected ? _selectedBuckets : _normalBuckets;

                if (!dict.TryGetValue(c, out List<Matrix4x4> bucket))
                {
                    bucket = new List<Matrix4x4>(256);
                    dict[c] = bucket;
                }
                bucket.Add(m);
            }

            FlushBuckets(_normalBuckets, normalMat);
            FlushBuckets(_selectedBuckets, selectedMat);
        }

        private void FlushBuckets(Dictionary<Color, List<Matrix4x4>> buckets, Material mat)
        {
            Camera cam = Camera.main;
            _sortCamPos = cam != null ? cam.transform.position : new Vector3(0f, 0f, -100f);

            foreach (KeyValuePair<Color, List<Matrix4x4>> kv in buckets)
            {
                Color colour = kv.Key;
                List<Matrix4x4> list = kv.Value;
                int total = list.Count;

                if (total == 0) continue;

                list.Sort(_depthComparison);

                _mpb.SetColor(k_ColorId, colour);
                _mpb.SetColor(k_BaseColorId, colour);

                if (_batchScratch.Length < Mathf.Min(total, k_MaxBatchSize))
                    _batchScratch = new Matrix4x4[k_MaxBatchSize];

                for (int start = 0; start < total; start += k_MaxBatchSize)
                {
                    int count = Mathf.Min(k_MaxBatchSize, total - start);

                    for (int j = 0; j < count; j++)
                        _batchScratch[j] = list[start + j];

                    Graphics.DrawMeshInstanced(_triangleMesh, 0, mat, _batchScratch, count, _mpb);
                }
            }
        }

        private int CompareByDepth(Matrix4x4 a, Matrix4x4 b)
        {
            float da = ((Vector3)a.GetColumn(3) - _sortCamPos).sqrMagnitude;
            float db = ((Vector3)b.GetColumn(3) - _sortCamPos).sqrMagnitude;
            return db.CompareTo(da);
        }

        private Color GetCellColor(int cellIndex, bool isSelected, float time)
        {
            int factionId = _factionIds[cellIndex];

            Color baseColor = Color.grey; // fallback when no faction data

            if (factionId >= 0 && _factionRegistry != null)
            {
                FactionConfig cfg = _factionRegistry.Get(factionId);
                if (cfg != null) baseColor = cfg.Color;
            }

            float alpha = isSelected ? _selectedAlpha : _provinceAlpha;
            baseColor.a = alpha;

            if (isSelected)
            {
                float t = Mathf.Sin(time * _selectedPulseSpeed) * 0.5f + 0.5f;
                Color white = new Color(1f, 1f, 1f, alpha);
                baseColor = Color.Lerp(baseColor, white, t);
            }

            return baseColor;
        }

        private static Mesh BuildTriangleMesh(float s)
        {
            float h = Mathf.Sqrt(3f) / 2f * s;
            float r = h * 2f / 3f;
            float r2 = h / 3f;

            Vector3 tipTop = new Vector3(0f, +r, -1f);
            Vector3 tipBl = new Vector3(-s * 0.5f, -r2, -1f);
            Vector3 tipBr = new Vector3(+s * 0.5f, -r2, -1f);

            Vector3 basTop = new Vector3(0f, +r, 0f);
            Vector3 basBl = new Vector3(-s * 0.5f, -r2, 0f);
            Vector3 basBr = new Vector3(+s * 0.5f, -r2, 0f);

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var tris = new List<int>();

            int fi = verts.Count;
            verts.Add(tipTop); verts.Add(tipBr); verts.Add(tipBl);
            norms.Add(-Vector3.forward); norms.Add(-Vector3.forward); norms.Add(-Vector3.forward);
            tris.Add(fi); tris.Add(fi + 1); tris.Add(fi + 2);

            int bi = verts.Count;
            verts.Add(basTop); verts.Add(basBl); verts.Add(basBr);
            norms.Add(Vector3.forward); norms.Add(Vector3.forward); norms.Add(Vector3.forward);
            tris.Add(bi); tris.Add(bi + 1); tris.Add(bi + 2);

            Vector3 centroid = Vector3.zero;

            AddSideQuad(verts, norms, tris, basTop, basBr, tipTop, tipBr, centroid); // right edge
            AddSideQuad(verts, norms, tris, basBr, basBl, tipBr, tipBl, centroid); // bottom edge
            AddSideQuad(verts, norms, tris, basBl, basTop, tipBl, tipTop, centroid); // left edge

            var mesh = new Mesh { name = "EquilateralTrianglePrism" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddSideQuad(
            List<Vector3> verts,
            List<Vector3> norms, List<int> tris,
            Vector3 baseA, Vector3 baseB,
            Vector3 tipA, Vector3 tipB,
            Vector3 centroid)
        {
            Vector3 edgeMid = (baseA + baseB) * 0.5f;
            Vector3 outward = new Vector3(
                edgeMid.x - centroid.x,
                edgeMid.y - centroid.y,
                0f)
                .normalized;

            int idx = verts.Count;
            verts.Add(baseA); norms.Add(outward);
            verts.Add(tipA); norms.Add(outward);
            verts.Add(tipB); norms.Add(outward);
            verts.Add(baseB); norms.Add(outward);

            tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 1);
            tris.Add(idx); tris.Add(idx + 3); tris.Add(idx + 2);
        }
    }
}