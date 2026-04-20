using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light_Soulls.Graphics
{
    public class Camera
    {
        public Matrix Transform { get; private set; }
        public Vector2 Position { get; set; }
        public float Zoom { get; set; } = 1f;
        public Viewport Viewport { get; set; }

        public void Update(Vector2 followTarget, Rectangle worldBounds)
        {
            float x = followTarget.X - Viewport.Width / 2f;
            float y = followTarget.Y - Viewport.Height / 2f;
            x = MathHelper.Clamp(x, worldBounds.Left, worldBounds.Right - Viewport.Width);
            y = MathHelper.Clamp(y, worldBounds.Top, worldBounds.Bottom - Viewport.Height);
            Position = new Vector2(x, y);
            UpdateTransform(); // se tiveres este método
        }

        public void UpdateTransform()
        {
            Transform = Matrix.CreateTranslation(-Position.X, -Position.Y, 0) * Matrix.CreateScale(Zoom);
        }
    }   
}
