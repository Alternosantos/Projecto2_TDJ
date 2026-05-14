using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    public sealed class FlyingEnemy
    {
        private const float PatrolSpeed = 80f;
        private const float DampingDecay = 0.95f;
        private const float Scale = 0.3f;

        public Vector2 Position;
        public Vector2 Velocity;

        private readonly Texture2D _texture;
        private readonly float _leftBound;
        private readonly float _rightBound;
        private readonly float _originalY;
        private readonly Vector2 _spawnPosition;
        private Animation _flyAnimation;

        private int CurrentWidth => (int)((_flyAnimation != null ? _flyAnimation.Frames[0].Width : _texture.Width) * Scale);
        private int CurrentHeight => (int)((_flyAnimation != null ? _flyAnimation.Frames[0].Height : _texture.Height) * Scale);

        private int _direction = 1;
        private float _returnTimer = 0f;
        private bool _returningToY = false;

        public FlyingEnemy(Texture2D texture, Vector2 startPosition,
                           float leftBound, float rightBound)
        {
            _texture = texture;
            _spawnPosition = startPosition;
            Position = startPosition;
            Velocity = Vector2.Zero;
            _leftBound = leftBound;
            _rightBound = rightBound;
            _originalY = startPosition.Y;
        }

        public void LoadAnimation(Animation anim) => _flyAnimation = anim;

        public void Reset()
        {
            Position = _spawnPosition;
            Velocity = Vector2.Zero;
            _direction = 1;
            _returningToY = false;
            _returnTimer = 0f;
            _flyAnimation?.Reset();
        }

        public void Update(GameTime gameTime, IReadOnlyList<Platform> platforms)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _flyAnimation?.Update(dt);
            Position += Velocity * dt;

            if (_returningToY)
            {
                _returnTimer -= dt;
                if (_returnTimer <= 0f) { Position.Y = _originalY; Velocity.Y = 0f; _returningToY = false; }
                else Position.Y = MathHelper.Lerp(Position.Y, _originalY, 0.05f);
            }

            bool knockedBack = System.Math.Abs(Velocity.X) >= PatrolSpeed * 1.5f;
            if (!knockedBack && !_returningToY)
                Position.X += _direction * PatrolSpeed * dt;

            if (Position.X <= _leftBound) { Position.X = _leftBound; _direction = 1; }
            else if (Position.X + CurrentWidth >= _rightBound) { Position.X = _rightBound - CurrentWidth; _direction = -1; }

            Velocity *= DampingDecay;
        }

        public void MarkToReturn(float delay = 2f) { _returningToY = true; _returnTimer = delay; }
        public void FlipDirection() => _direction = -_direction;
        public bool CollidesWith(Rectangle other) => GetBounds().Intersects(other);
        public Rectangle Bounds => GetBounds();

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D tex = _flyAnimation?.GetCurrentFrame() ?? _texture;
            Color color = _flyAnimation != null ? Color.White : Color.Purple;
            SpriteEffects fx = _direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(tex, Position, null, color, 0f, Vector2.Zero, Scale, fx, 0f);
        }

        private Rectangle GetBounds()
            => new Rectangle((int)Position.X, (int)Position.Y, CurrentWidth, CurrentHeight);
    }
}