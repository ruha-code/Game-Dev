using UnityEngine;
using UnityEngine.UIElements;

namespace AeroOS.UI
{
    [UxmlElement]
    public partial class AeroActiveBackground : VisualElement
    {
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

            // Horizontal gradient: dark blue (transparent) -> bright blue -> dark blue (transparent)
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

            painter.fillGradient = FillGradient.MakeLinearGradient(
                gradient,
                new Vector2(0, 0),
                new Vector2(w, 0),
                AddressMode.Clamp
            );

            painter.BeginPath();
            painter.MoveTo(new Vector2(0, 0));
            painter.LineTo(new Vector2(w, 0));
            painter.LineTo(new Vector2(w, h));
            painter.LineTo(new Vector2(0, h));
            painter.ClosePath();
            painter.Fill();

            // Bright line at the bottom
            painter.strokeColor = new Color(0.5f, 0.8f, 1f, 0.5f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, h));
            painter.LineTo(new Vector2(w, h));
            painter.Stroke();
        }
    }

    [UxmlElement]
    public partial class AeroSwoosh : VisualElement
    {
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
            
            // Draw a glowing curved line at the bottom
            painter.lineWidth = 2f;
            painter.lineCap = LineCap.Round;
            
            // Main swoosh
            painter.strokeColor = new Color(0.4f, 0.9f, 1f, 0.4f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, h * 0.8f));
            painter.BezierCurveTo(
                new Vector2(w * 0.3f, h * 0.9f),
                new Vector2(w * 0.7f, h * 0.7f),
                new Vector2(w, h * 0.85f)
            );
            painter.Stroke();

            // Subtle secondary swoosh
            painter.strokeColor = new Color(0.2f, 0.6f, 1f, 0.2f);
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, h * 0.85f));
            painter.BezierCurveTo(
                new Vector2(w * 0.4f, h * 0.95f),
                new Vector2(w * 0.6f, h * 0.8f),
                new Vector2(w, h * 0.9f)
            );
            painter.Stroke();
        }
    }
}
