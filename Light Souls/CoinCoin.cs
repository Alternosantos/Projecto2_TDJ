using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    public sealed class Coin
    {
        public Vector2 Position { get; }
        public bool IsCollected { get; set; }

        private readonly Texture2D _texture;

        public Coin(Texture2D texture, Vector2 position)
        {
            _texture = texture;
            Position = position;
            IsCollected = false;
        }

        // ── Reset ────────────────────────────────────────────────────────────────

        /// <summary>Torna a moeda visível e colecionável novamente.</summary>
        public void Reset() => IsCollected = false;

        // ── Public methods ───────────────────────────────────────────────────────

        public Rectangle GetBounds()
            => new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsCollected)
                spriteBatch.Draw(_texture, Position, Color.White);
        }
    }
}