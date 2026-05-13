using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace AeroOS.UI
{
    [UxmlElement]
    public partial class AeroActiveBackground : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<AeroActiveBackground, UxmlTraits> { }

        public AeroActiveBackground()
        {
            generateVisualContent += OnGenerateVisualContent;
        }

        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            float w = contentRect.width;
            float h = contentRect.height;
            if (w < 1f || h < 1f) return;

            var painter = ctx.painter2D;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { 
                    new GradientColorKey(new Color(0.1f, 0.4f, 0.8f, 0f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.6f, 1f, 0.8f), 0.5f),
                    new GradientColorKey(new Color(0.1f, 0.4f, 0.8f, 0f), 1f)
                },
                new[] { 
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );

            painter.fillGradient = FillGradient.MakeLinearGradient(gradient, new Vector2(0, 0), new Vector2(w, 0), AddressMode.Clamp);
            painter.BeginPath();
            painter.MoveTo(Vector2.zero); painter.LineTo(new Vector2(w, 0)); painter.LineTo(new Vector2(w, h)); painter.LineTo(new Vector2(0, h)); painter.ClosePath();
            painter.Fill();

            painter.strokeColor = new Color(0.5f, 0.8f, 1f, 0.6f);
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, h)); painter.LineTo(new Vector2(w, h));
            painter.Stroke();
        }
    }

    [UxmlElement]
    public partial class AeroSwoosh : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<AeroSwoosh, UxmlTraits> { }

        public AeroSwoosh()
        {
            generateVisualContent += OnGenerateVisualContent;
            pickingMode = PickingMode.Ignore;
        }

        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            float w = contentRect.width;
            float h = contentRect.height;
            if (w < 1f || h < 1f) return;

            var painter = ctx.painter2D;
            painter.lineCap = LineCap.Round;
            
            painter.strokeColor = new Color(0.4f, 0.9f, 1f, 0.3f);
            painter.lineWidth = 2.5f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, h * 0.8f));
            painter.BezierCurveTo(new Vector2(w * 0.3f, h * 0.95f), new Vector2(w * 0.7f, h * 0.65f), new Vector2(w, h * 0.85f));
            painter.Stroke();

            painter.strokeColor = new Color(0.2f, 0.6f, 1f, 0.2f);
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, h * 0.85f));
            painter.BezierCurveTo(new Vector2(w * 0.4f, h * 1.0f), new Vector2(w * 0.6f, h * 0.75f), new Vector2(w, h * 0.9f));
            painter.Stroke();
        }
    }

    [UxmlElement]
    public partial class AeroAtmosphere : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<AeroAtmosphere, UxmlTraits> { }

        private class Particle { public Vector2 pos; public float size; public float speed; public Color color; public bool anomaly; }
        private List<Particle> bgParticles = new List<Particle>();
        private List<Particle> midParticles = new List<Particle>();
        private List<Particle> fgParticles = new List<Particle>();
        private float time;
        public float brightnessBoost = 0f;
        public float anomalyShift = 0f;

        public AeroAtmosphere()
        {
            generateVisualContent += OnGenerateVisualContent;
            pickingMode = PickingMode.Ignore;
            
            for (int i = 0; i < 40; i++) bgParticles.Add(new Particle { pos = new Vector2(Random.value, Random.value), size = Random.Range(0.5f, 1.5f), speed = Random.Range(0.002f, 0.005f), color = new Color(1, 1, 1, 0.15f) });
            for (int i = 0; i < 30; i++) midParticles.Add(new Particle { pos = new Vector2(Random.value, Random.value), size = Random.Range(1.5f, 2.5f), speed = Random.Range(0.005f, 0.012f), color = new Color(0.8f, 0.95f, 1, 0.3f) });
            for (int i = 0; i < 15; i++) fgParticles.Add(new Particle { pos = new Vector2(Random.value, Random.value), size = Random.Range(4f, 8f), speed = Random.Range(0.015f, 0.03f), color = new Color(1, 1, 1, 0.1f) });

            schedule.Execute(() => {
                time += 0.016f;
                MarkDirtyRepaint();
            }).Every(16);
        }

        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            float w = contentRect.width;
            float h = contentRect.height;
            if (w < 1f || h < 1f) return;

            var painter = ctx.painter2D;

            // Water Shimmer
            painter.fillColor = new Color(0.4f, 0.8f, 1f, 0.02f + Mathf.Sin(time * 0.5f) * 0.01f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, h * 0.75f));
            painter.LineTo(new Vector2(w, h * 0.75f));
            painter.LineTo(new Vector2(w, h));
            painter.LineTo(new Vector2(0, h));
            painter.ClosePath();
            painter.Fill();

            // Light Rays (Softer)
            float rayInt = (0.06f + Mathf.PingPong(time * 0.2f, 0.04f)) + brightnessBoost;
            var rayGrad = new Gradient();
            rayInt = Mathf.Clamp01(rayInt);
            rayGrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.9f, 0.98f, 1f, rayInt), 0f), new GradientColorKey(new Color(0.9f, 0.98f, 1f, 0f), 1f) },
                new[] { new GradientAlphaKey(rayInt, 0f), new GradientAlphaKey(0f, 1f) }
            );
            painter.fillGradient = FillGradient.MakeRadialGradient(rayGrad, new Vector2(w * 0.95f, -h * 0.05f), w * 1.5f, new Vector2(w * 0.95f, -h * 0.05f), AddressMode.Clamp);
            painter.BeginPath();
            painter.MoveTo(Vector2.zero); painter.LineTo(new Vector2(w, 0)); painter.LineTo(new Vector2(w, h)); painter.LineTo(new Vector2(0, h)); painter.ClosePath();
            painter.Fill();

            DrawLayer(painter, bgParticles, w, h, 1.0f);
            DrawLayer(painter, midParticles, w, h, 1.2f);
            DrawLayer(painter, fgParticles, w, h, 1.5f);
        }

        void DrawLayer(Painter2D painter, List<Particle> layer, float w, float h, float speedMult)
        {
            foreach (var p in layer)
            {
                float dir = p.anomaly ? -1 : 1;
                float px = ((p.pos.x + time * p.speed * speedMult * dir) % 1.0f) * w;
                float py = ((p.pos.y + Mathf.Sin(time * 0.5f + p.pos.x * 5) * 0.01f) % 1.0f) * h;
                painter.fillColor = p.color;
                painter.BeginPath();
                painter.Arc(new Vector2(px + (p.anomaly ? anomalyShift : 0), py), p.size, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise);
                painter.Fill();
            }
        }

        public void TriggerParticleAnomaly()
        {
            if (fgParticles.Count > 0) fgParticles[Random.Range(0, fgParticles.Count)].anomaly = true;
            schedule.Execute(() => { foreach(var p in fgParticles) p.anomaly = false; }).StartingIn(2000);
        }
    }

    [UxmlElement]
    public partial class AeroLogo : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<AeroLogo, UxmlTraits> { }
        private float time;
        private float sweepX = -0.5f;

        public AeroLogo()
        {
            generateVisualContent += OnGenerateVisualContent;
            schedule.Execute(() => {
                time += 0.016f;
                sweepX += 0.002f;
                MarkDirtyRepaint();
            }).Every(16);
        }

        public void ResetSweep() => sweepX = -0.2f;

        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            float w = contentRect.width;
            float h = contentRect.height;
            if (w < 1f || h < 1f) return;
            var painter = ctx.painter2D;
            if (sweepX > -0.2f && sweepX < 1.2f)
            {
                var sweepGrad = new Gradient();
                sweepGrad.SetKeys(new[] { new GradientColorKey(new Color(1, 1, 1, 0), 0f), new GradientColorKey(new Color(1, 1, 1, 0.4f), 0.5f), new GradientColorKey(new Color(1, 1, 1, 0), 1f) }, new[] { new GradientAlphaKey(0, 0f), new GradientAlphaKey(0.4f, 0.5f), new GradientAlphaKey(0, 1f) });
                painter.fillGradient = FillGradient.MakeLinearGradient(sweepGrad, new Vector2(w * sweepX, 0), new Vector2(w * (sweepX + 0.1f), h), AddressMode.Clamp);
                painter.BeginPath();
                painter.MoveTo(Vector2.zero); painter.LineTo(new Vector2(w, 0)); painter.LineTo(new Vector2(w, h)); painter.LineTo(new Vector2(0, h)); painter.ClosePath();
                painter.Fill();
            }
        }
    }

    [UxmlElement]
    public partial class AeroHighlightSweep : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<AeroHighlightSweep, UxmlTraits> { }
        private float offset = -1.0f;
        public AeroHighlightSweep() { generateVisualContent += OnGenerateVisualContent; pickingMode = PickingMode.Ignore; }
        public void Animate() { offset = -1.0f; schedule.Execute(() => { offset += 0.05f; MarkDirtyRepaint(); if (offset > 2.0f) return; }).Every(16).Until(() => offset > 2.0f); }
        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            float w = contentRect.width; float h = contentRect.height;
            if (w < 1f || h < 1f || offset < -0.5f || offset > 1.5f) return;
            var painter = ctx.painter2D;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(new Color(1, 1, 1, 0), 0f), new GradientColorKey(new Color(1, 1, 1, 0.3f), 0.5f), new GradientColorKey(new Color(1, 1, 1, 0), 1f) }, new[] { new GradientAlphaKey(0, 0f), new GradientAlphaKey(0.3f, 0.5f), new GradientAlphaKey(0, 1f) });
            painter.fillGradient = FillGradient.MakeLinearGradient(gradient, new Vector2(w * offset, 0), new Vector2(w * (offset + 0.2f), h), AddressMode.Clamp);
            painter.BeginPath();
            painter.MoveTo(Vector2.zero); painter.LineTo(new Vector2(w, 0)); painter.LineTo(new Vector2(w, h)); painter.LineTo(new Vector2(0, h)); painter.ClosePath();
            painter.Fill();
        }
    }
}