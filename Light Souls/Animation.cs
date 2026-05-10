using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    public class Animation
    {
        public Texture2D SpriteSheet;
        public int FrameWidth, FrameHeight;
        public int FramesPerRow;
        public float FrameTime;
        private float _timer;
        private int _currentFrame;

        public Animation(Texture2D spriteSheet, int frameWidth, int frameHeight, float frameTime)
        {
            SpriteSheet = spriteSheet;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            FrameTime = frameTime;
            FramesPerRow = spriteSheet.Width / frameWidth;
        }

        public void Update(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer >= FrameTime)
            {
                _timer -= FrameTime;
                _currentFrame = (_currentFrame + 1) % (FramesPerRow * (SpriteSheet.Height / FrameHeight));
            }
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position)
        {
            int row = _currentFrame / FramesPerRow;
            int col = _currentFrame % FramesPerRow;
            Rectangle source = new Rectangle(col * FrameWidth, row * FrameHeight, FrameWidth, FrameHeight);
            spriteBatch.Draw(SpriteSheet, position, source, Color.White);
        }
    }
}