using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    public class Coin
    {
        public Vector2 Position;
        public bool IsCollected;
        private Texture2D _texture;

        public Coin(Texture2D texture, Vector2 position)
        {
            _texture = texture;
            Position = position;
            IsCollected = false;
        }

        public Rectangle GetBounds()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsCollected)
                spriteBatch.Draw(_texture, Position, Color.Yellow);
        }
    }
}