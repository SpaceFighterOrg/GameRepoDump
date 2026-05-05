using Assets.Scripts.Utils.I18N;
using Backend.Common.DTOs.Map;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.MVVM.Views.UI.SelectedPlanet
{
    public class FactionControlView
    {
        private const float OuterRadiusFraction = 0.46f; // fraction of half canvas size
        private const float InnerRadiusFraction = 0.28f; // creates the hole
        private const float GapAngleDeg = 2.0f;   // gap between segments (degrees)
        private const int ArcSegments = 64;     // mesh smoothness per arc

        private readonly FactionRegistry _registry;

        private VisualElement _root;
        private VisualElement _canvas;
        private Label _totalLabel;
        private VisualElement _legend;

        private List<SegmentData> _segments = new();

        private struct SegmentData
        {
            public Color Color;
            public string Name;
            public float Points;
            public float Fraction; // 0..1
        }

        public FactionControlView(FactionRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void Mount(VisualElement parent, VisualTreeAsset uxmlAsset)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (uxmlAsset == null) throw new ArgumentNullException(nameof(uxmlAsset));

            var tree = uxmlAsset.Instantiate();
            parent.Add(tree);
            BindElements(tree);
        }

        public void BindElements(VisualElement root)
        {
            _root = root.Q<VisualElement>("donut-root");
            _canvas = root.Q<VisualElement>("donut-canvas");
            _totalLabel = root.Q<Label>("donut-total-label");
            _legend = root.Q<VisualElement>("donut-legend");

            var captionLabel = root.Q<Label>("donut-total-caption");
            if (captionLabel != null) captionLabel.text = LocalisationKeys.ControlPoints.Localize();

            _canvas.generateVisualContent += OnGenerateVisualContent;
        }

        public void Refresh(List<PlanetBattleStatsDTO> battleStats)
        {
            BuildSegments(battleStats);
            UpdateTotalLabel();
            RebuildLegend();
            _canvas?.MarkDirtyRepaint();
        }

        private void BuildSegments(List<PlanetBattleStatsDTO> stats)
        {
            _segments.Clear();

            if (stats == null || stats.Count == 0) return;

            float total = 0f;
            foreach (var s in stats) total += Mathf.Max(0f, s.ControlPoints);

            if (total <= 0f) return;

            foreach (var stat in stats)
            {
                float pts = Mathf.Max(0f, stat.ControlPoints);
                if (pts <= 0f) continue;

                var cfg = _registry.Get(stat.FactionId);
                _segments.Add(new SegmentData
                {
                    Color = cfg != null ? cfg.Color : Color.gray,
                    Name = cfg != null ? cfg.FactionName : $"Faction {stat.FactionId}",
                    Points = pts,
                    Fraction = pts / total,
                });
            }
        }

        private void UpdateTotalLabel()
        {
            if (_totalLabel == null) return;

            float total = 0f;
            foreach (var seg in _segments) total += seg.Points;
            _totalLabel.text = Mathf.RoundToInt(total).ToString("N0");
        }

        private void RebuildLegend()
        {
            if (_legend == null) return;
            _legend.Clear();

            foreach (var seg in _segments)
            {
                var row = new VisualElement();
                row.AddToClassList("legend-row");

                var swatch = new VisualElement();
                swatch.AddToClassList("legend-swatch");
                swatch.style.backgroundColor = seg.Color;

                var name = new Label(seg.Name.Localize());
                name.AddToClassList("legend-name");

                var value = new Label(Mathf.RoundToInt(seg.Points).ToString("N0"));
                value.AddToClassList("legend-value");

                var pct = new Label($"{seg.Fraction * 100f:F1}%");
                pct.AddToClassList("legend-pct");

                row.Add(swatch);
                row.Add(name);
                row.Add(value);
                row.Add(pct);
                _legend.Add(row);
            }
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            if (_segments.Count == 0) return;

            var painter = ctx.painter2D;
            var rect = _canvas.contentRect;

            float cx = rect.width * 0.5f;
            float cy = rect.height * 0.5f;
            float halfMin = Mathf.Min(cx, cy);
            float outerRadius = halfMin * OuterRadiusFraction * 2f;
            float innerRadius = halfMin * InnerRadiusFraction * 2f;

            float startAngle = -90f; // 12 o'clock

            for (int i = 0; i < _segments.Count; i++)
            {
                var seg = _segments[i];

                float sweep = seg.Fraction * 360f - GapAngleDeg;
                float endAngle = startAngle + sweep;

                DrawAnnularSegment(
                    painter,
                    cx, cy,
                    outerRadius, innerRadius,
                    startAngle, endAngle,
                    seg.Color
                );

                startAngle = endAngle + GapAngleDeg;
            }

            var dominant = _segments[0];
            for (int i = 1; i < _segments.Count; i++)
                if (_segments[i].Points > dominant.Points)
                    dominant = _segments[i];

            float centerRadius = innerRadius * 0.65f;
            DrawFilledCircle(painter, cx, cy, centerRadius, dominant.Color);
        }

        private static void DrawFilledCircle(Painter2D painter, float cx, float cy, float radius, Color color)
        {
            painter.fillColor = color;
            painter.BeginPath();
            painter.Arc(new Vector2(cx, cy), radius, 0f, 360f);
            painter.Fill();
        }

        private static void DrawAnnularSegment(
            Painter2D painter,
            float cx, float cy,
            float outerR, float innerR,
            float startDeg, float endDeg,
            Color color)
        {
            if (Mathf.Approximately(startDeg, endDeg)) return;

            float startRad = startDeg * Mathf.Deg2Rad;
            float endRad = endDeg * Mathf.Deg2Rad;
            int steps = Mathf.Max(2, Mathf.RoundToInt(ArcSegments * Mathf.Abs(endDeg - startDeg) / 360f));

            painter.fillColor = color;
            painter.strokeColor = color;

            painter.BeginPath();

            // Outer arc: start -> end
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float ang = Mathf.Lerp(startRad, endRad, t);
                float x = cx + Mathf.Cos(ang) * outerR;
                float y = cy + Mathf.Sin(ang) * outerR;

                if (i == 0) painter.MoveTo(new Vector2(x, y));
                else painter.LineTo(new Vector2(x, y));
            }

            // Inner arc: end -> start (reverse, to close the shape)
            for (int i = steps; i >= 0; i--)
            {
                float t = (float)i / steps;
                float ang = Mathf.Lerp(startRad, endRad, t);
                float x = cx + Mathf.Cos(ang) * innerR;
                float y = cy + Mathf.Sin(ang) * innerR;
                painter.LineTo(new Vector2(x, y));
            }

            painter.ClosePath();
            painter.Fill();
        }
    }
}