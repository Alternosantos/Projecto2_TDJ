using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    public sealed class Coin
    {
        public Vector2 Position { get; }
        public bool IsCollected { get; set; }

        private Animation _animation;
        private readonly Texture2D _staticTexture;
        private const float Scale = 0.5f; // Ajusta conforme necessário

        public Coin(Texture2D texture, Vector2 position)
        {
            _staticTexture = texture;
            Position = position;
            IsCollected = false;
            _animation = null;
        }

        public void LoadAnimation(Animation anim) => _animation = anim;

        public void Update(GameTime gameTime)
        {
            if (!IsCollected && _animation != null)
                _animation.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        public void Reset()
        {
            IsCollected = false;
            _animation?.Reset();
        }

        public Rectangle GetBounds()
        {
            int width, height;
            if (_animation != null && _animation.Frames.Count > 0)
            {
                width = (int)(_animation.Frames[0].Width * Scale);
                height = (int)(_animation.Frames[0].Height * Scale);
            }
            else
            {
                width = (int)(_staticTexture.Width * Scale);
                height = (int)(_staticTexture.Height * Scale);
            }
            return new Rectangle((int)Position.X, (int)Position.Y, width, height);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsCollected) return;

            Texture2D tex;
            if (_animation != null)
                tex = _animation.GetCurrentFrame();
            else
                tex = _staticTexture;

            spriteBatch.Draw(tex, Position, null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
    }
}