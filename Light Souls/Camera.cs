using Microsoft.Xna.Framework;

namespace Light_Souls
{
    /// <summary>
    /// 2-D scrolling camera that follows a target and clamps to world bounds.
    /// The camera always operates in the game's virtual coordinate space.
    /// </summary>
    public sealed class Camera
    {
        // ── Properties ───────────────────────────────────────────────────────────

        /// <summary>Top-left corner of the visible area in world space.</summary>
        public Vector2 Position { get; private set; }

        /// <summary>Width of the viewport in virtual pixels.</summary>
        public int ViewportWidth { get; }

        /// <summary>Height of the viewport in virtual pixels.</summary>
        public int ViewportHeight { get; }

        /// <summary>Total width of the loaded level in pixels.</summary>
        public int WorldWidth { get; }

        /// <summary>Total height of the loaded level in pixels.</summary>
        public int WorldHeight { get; }

        // ── Constructor ──────────────────────────────────────────────────────────

        public Camera(int viewportWidth, int viewportHeight, int worldWidth, int worldHeight)
        {
            ViewportWidth  = viewportWidth;
            ViewportHeight = viewportHeight;
            WorldWidth     = worldWidth;
            WorldHeight    = worldHeight;
            Position       = Vector2.Zero;
        }

        // ── Public methods ───────────────────────────────────────────────────────

        /// <summary>
        /// Centers the camera on <paramref name="target"/> while keeping the
        /// view fully inside the world bounds.
        /// </summary>
        public void Follow(Vector2 target)
        {
            float x = target.X - ViewportWidth  * 0.5f;
            float y = target.Y - ViewportHeight * 0.5f;

            x = MathHelper.Clamp(x, 0f, WorldWidth  - ViewportWidth);
            float maxY = WorldHeight - ViewportHeight;
            if (y > maxY)
                y = maxY;
            Position = new Vector2(x, y);
        }

        /// <summary>
        /// Returns a translation matrix that moves world-space coordinates into
        /// screen space. Pass this to <c>SpriteBatch.Begin</c> as the
        /// <c>transformMatrix</c> argument.
        /// </summary>
        public Matrix GetTransformMatrix()
            => Matrix.CreateTranslation(-Position.X, -Position.Y, 0f);
    }
}
