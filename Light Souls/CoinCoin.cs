using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    /// <summary>
    /// A collectable coin. Once collected it is hidden and no longer participates
    /// in collision checks.
    /// </summary>
    public sealed class Coin
    {
        // ── Properties ───────────────────────────────────────────────────────────

        /// <summary>World-space position of this coin.</summary>
        public Vector2 Position { get; }

        /// <summary>True once the player has collected this coin.</summary>
        public bool IsCollected { get; set; }

        // ── Private fields ───────────────────────────────────────────────────────

        private readonly Texture2D _texture;

        // ── Constructor ──────────────────────────────────────────────────────────

        public Coin(Texture2D texture, Vector2 position)
        {
            _texture    = texture;
            Position    = position;
            IsCollected = false;
        }

        // ── Public methods ───────────────────────────────────────────────────────

        /// <summary>Returns the world-space axis-aligned bounding box of this coin.</summary>
        public Rectangle GetBounds()
            => new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);

        /// <summary>Draws the coin if it has not yet been collected.</summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsCollected)
                spriteBatch.Draw(_texture, Position, Color.White);
        }
    }
}