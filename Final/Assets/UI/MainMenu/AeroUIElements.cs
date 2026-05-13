using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace AeroOS.UI
{
    [UxmlElement]
    public partial class AeroActiveBackground : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<AeroActiveBackground, UxmlTraits> { }

        private static readonly Gradient sharedGradient = new Gradient();
        private static readonly GradientColorKey[] colorKeys = new GradientColorKey[3];
        private static readonly GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];

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
            
            colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.4f, 0.8f, 0f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.2f, 0.6f, 1f, 0.8f), 0.5f);
            colorKeys[2] = new GradientColorKey(new Color(0.1f, 0.4f, 0.8f, 0f), 1f);
            
            alphaKeys[0] = new GradientAlphaKey(0f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0.8f, 0.5f);
            alphaKeys[2] = new GradientAlphaKey(0f, 1f);
            
            sharedGradient.SetKeys(colorKeys, alphaKeys);

            painter.fillGradient = FillGradient.MakeLinearGradient(sharedGradient, new Vector2(0, 0), new Vector2(w, 0), AddressMode.Clamp);
            painter.BeginPath();
            painter.MoveTo(Vector2.zero); painter.LineTo(new Vector2(w, 0)); painter.LineTo(new Vector2(w, h)); painter.LineTo(new Vector2(0, h)); painter.ClosePath();
            painter.Fill();

            // Removed stroke color and line drawing to fix bottom horizontal line artifact
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
        private bool isRunning = true;

        // Optimized shared objects to eliminate GC spikes
        private static readonly Gradient waterGrad = new Gradient();
        private static readonly Gradient rayGrad = new Gradient();
        private static readonly Gradient pGrad = new Gradient();
        private static readonly GradientColorKey[] colorKeys = new GradientColorKey[3];
        private static readonly GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
        private static readonly GradientColorKey[] pcKeys = new GradientColorKey[2];
        private static readonly GradientAlphaKey[] paKeys = new GradientAlphaKey[2];

        public AeroAtmosphere()
        {
            generateVisualContent += OnGenerateVisualContent;
            pickingMode = PickingMode.Ignore;
            
            for (int i = 0; i < 40; i++) bgParticles.Add(new Particle { pos = new Vector2(Random.value, Random.value), size = Random.Range(0.5f, 1.5f), speed = Random.Range(0.002f, 0.005f), color = new Color(1, 1, 1, 0.15f) });
            for (int i = 0; i < 30; i++) midParticles.Add(new Particle { pos = new Vector2(Random.value, Random.value), size = Random.Range(1.5f, 2.5f), speed = Random.Range(0.005f, 0.012f), color = new Color(0.8f, 0.95f, 1, 0.3f) });
            for (int i = 0; i < 15; i++) fgParticles.Add(new Particle { pos = new Vector2(Random.value, Random.value), size = Random.Range(4f, 8f), speed = Random.Range(0.015f, 0.03f), color = new Color(1, 1, 1, 0.1f) });

            schedule.Execute(() => {
                if (!isRunning) return;
                time += 0.016f;
                MarkDirtyRepaint();
            }).Every(16);
        }

        public void StopAtmosphere() 
        {
            isRunning = false;
            style.display = DisplayStyle.None;
        }

        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            if (!isRunning) return;
            float w = contentRect.width;
            float h = contentRect.height;
            if (w < 1f || h < 1f) return;

            var painter = ctx.painter2D;

            // Water Shimmer
            float waterAlpha = 0.02f + Mathf.Sin(time * 0.5f) * 0.01f;
            colorKeys[0] = new GradientColorKey(new Color(0.4f, 0.8f, 1f, 0f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(0.4f, 0.8f, 1f, 0f), 0.65f);
            colorKeys[2] = new GradientColorKey(new Color(0.4f, 0.8f, 1f, waterAlpha), 0.85f);
            alphaKeys[0] = new GradientAlphaKey(0f, 0f);
            alphaKeys[1] = new GradientAlphaKey(0f, 0.65f);
            alphaKeys[2] = new GradientAlphaKey(waterAlpha, 0.85f);
            waterGrad.SetKeys(colorKeys, alphaKeys);
            painter.fillGradient = FillGradient.MakeLinearGradient(waterGrad, Vector2.zero, new Vector2(0, h), AddressMode.Clamp);
            
            painter.BeginPath();
            painter.MoveTo(Vector2.zero); painter.LineTo(new Vector2(w, 0)); painter.LineTo(new Vector2(w, h)); painter.LineTo(new Vector2(0, h)); painter.ClosePath();
            painter.Fill();

            // Light Rays
            float rayInt = (0.06f + Mathf.PingPong(time * 0.2f, 0.04f)) + brightnessBoost;
            rayInt = Mathf.Clamp01(rayInt);
            colorKeys[0] = new GradientColorKey(new Color(0.9f, 0.98f, 1f, rayInt), 0f); 
            colorKeys[1] = new GradientColorKey(new Color(0.9f, 0.98f, 1f, rayInt * 0.4f), 0.3f);
            colorKeys[2] = new GradientColorKey(new Color(0.9f, 0.98f, 1f, 0f), 1f);
            alphaKeys[0] = new GradientAlphaKey(rayInt, 0f); 
            alphaKeys[1] = new GradientAlphaKey(rayInt * 0.4f, 0.3f);
            alphaKeys[2] = new GradientAlphaKey(0f, 1f);
            rayGrad.SetKeys(colorKeys, alphaKeys);
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
                
                // Minimal gradient setup for particles using cached objects
                pcKeys[0] = new GradientColorKey(p.color, 0f);
                pcKeys[1] = new GradientColorKey(new Color(p.color.r, p.color.g, p.color.b, 0f), 1f);
                paKeys[0] = new GradientAlphaKey(p.color.a, 0f);
                paKeys[1] = new GradientAlphaKey(0f, 1f);
                pGrad.SetKeys(pcKeys, paKeys);

                Vector2 center = new Vector2(px + (p.anomaly ? anomalyShift : 0), py);
                painter.fillGradient = FillGradient.MakeRadialGradient(pGrad, center, p.size, center, AddressMode.Clamp);
                
                painter.BeginPath();
                painter.Arc(center, p.size, Angle.Degrees(0), Angle.Degrees(360), ArcDirection.Clockwise);
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

        private static readonly Gradient sweepGrad = new Gradient();
        private static readonly GradientColorKey[] cKeys = new GradientColorKey[3];
        private static readonly GradientAlphaKey[] aKeys = new GradientAlphaKey[3];

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
                cKeys[0] = new GradientColorKey(new Color(1, 1, 1, 0), 0f);
                cKeys[1] = new GradientColorKey(new Color(1, 1, 1, 0.4f), 0.5f);
                cKeys[2] = new GradientColorKey(new Color(1, 1, 1, 0), 1f);
                aKeys[0] = new GradientAlphaKey(0, 0f);
                aKeys[1] = new GradientAlphaKey(0.4f, 0.5f);
                aKeys[2] = new GradientAlphaKey(0, 1f);
                sweepGrad.SetKeys(cKeys, aKeys);

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

        private static readonly Gradient highlightGrad = new Gradient();
        private static readonly GradientColorKey[] cKeys = new GradientColorKey[3];
        private static readonly GradientAlphaKey[] aKeys = new GradientAlphaKey[3];

        public AeroHighlightSweep() { generateVisualContent += OnGenerateVisualContent; pickingMode = PickingMode.Ignore; }
        public void Animate() { offset = -1.0f; schedule.Execute(() => { offset += 0.05f; MarkDirtyRepaint(); if (offset > 2.0f) return; }).Every(16).Until(() => offset > 2.0f); }
        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            float w = contentRect.width; float h = contentRect.height;
            if (w < 1f || h < 1f || offset < -0.5f || offset > 1.5f) return;
            var painter = ctx.painter2D;
            
            cKeys[0] = new GradientColorKey(new Color(1, 1, 1, 0), 0f);
            cKeys[1] = new GradientColorKey(new Color(1, 1, 1, 0.3f), 0.5f);
            cKeys[2] = new GradientColorKey(new Color(1, 1, 1, 0), 1f);
            aKeys[0] = new GradientAlphaKey(0, 0f);
            aKeys[1] = new GradientAlphaKey(0.3f, 0.5f);
            aKeys[2] = new GradientAlphaKey(0, 1f);
            highlightGrad.SetKeys(cKeys, aKeys);

            painter.fillGradient = FillGradient.MakeLinearGradient(highlightGrad, new Vector2(w * offset, 0), new Vector2(w * (offset + 0.2f), h), AddressMode.Clamp);
            painter.BeginPath();
            painter.MoveTo(Vector2.zero); painter.LineTo(new Vector2(w, 0)); painter.LineTo(new Vector2(w, h)); painter.LineTo(new Vector2(0, h)); painter.ClosePath();
            painter.Fill();
        }
    }
}