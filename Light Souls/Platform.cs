using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    /// <summary>
    /// A static, collidable surface. Tiles its texture horizontally to fill
    /// the full width of its bounding rectangle.
    /// </summary>
    public sealed class Platform
    {
        // ── Properties ───────────────────────────────────────────────────────────

        /// <summary>Axis-aligned bounding box used for collision detection.</summary>
        public Rectangle Bounds { get; }

        // ── Private fields ───────────────────────────────────────────────────────

        private readonly Texture2D _texture;

        // ── Constructor ──────────────────────────────────────────────────────────

        public Platform(Rectangle bounds, Texture2D texture)
        {
            Bounds   = bounds;
            _texture = texture;
        }

        // ── Public methods ───────────────────────────────────────────────────────

        /// <summary>Tiles the platform texture across its full width.</summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            int tileW = _texture.Width;
            int tileH = _texture.Height;
            int y     = Bounds.Y + (Bounds.Height - tileH) / 2; // vertically centred

            for (int x = Bounds.X; x < Bounds.Right; x += tileW)
            {
                int clippedWidth = System.Math.Min(tileW, Bounds.Right - x);
                var src  = new Rectangle(0, 0, clippedWidth, tileH);
                var dest = new Rectangle(x, y, clippedWidth, tileH);
                spriteBatch.Draw(_texture, dest, src, Color.White);
            }
        }
    }
}