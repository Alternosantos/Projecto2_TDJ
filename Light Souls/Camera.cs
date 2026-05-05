using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light_Souls
{
    public class Camera
    {
        public Vector2 Position { get; set; }
        public int ViewportWidth { get; set; }
        public int ViewportHeight { get; set; }
        public int WorldWidth { get; set; }   // Total level width (in pixels)
        public int WorldHeight { get; set; }  // Total level height (in pixels)

        public Camera(int viewportWidth, int viewportHeight, int worldWidth, int worldHeight)
        {
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
            WorldWidth = worldWidth;
            WorldHeight = worldHeight;
            Position = Vector2.Zero;
        }

        // Follow the target (player), keeping camera within world bounds
        public void Follow(Vector2 targetPosition)
        {
            // Center camera on target
            float targetX = targetPosition.X + 16 - (ViewportWidth / 2);   // 16 = half player width (assuming 32px)
            float targetY = targetPosition.Y + 16 - (ViewportHeight / 2);  // center vertically

            // Clamp to world bounds
            targetX = MathHelper.Clamp(targetX, 0, WorldWidth - ViewportWidth);
            targetY = MathHelper.Clamp(targetY, 0, WorldHeight - ViewportHeight);

            Position = new Vector2(targetX, targetY);
        }

        // Transform world coordinates to screen coordinates
        public Vector2 Transform(Vector2 worldPosition)
        {
            return worldPosition - Position;
        }

        // Transform a rectangle (useful for drawing tiles)
        public Rectangle Transform(Rectangle worldRectangle)
        {
            return new Rectangle(
                worldRectangle.X - (int)Position.X,
                worldRectangle.Y - (int)Position.Y,
                worldRectangle.Width,
                worldRectangle.Height);
        }
    }
}
