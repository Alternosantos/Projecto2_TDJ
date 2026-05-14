using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    public sealed class Enemy
    {
        private const float WalkSpeed = 100f;
        private const float Gravity = 1200f;
        private const float DampingDecay = 0.95f;
        private const float Scale = 0.5f;
        private const float EdgeCheckCooldown = 0.3f;

        public Vector2 Position;
        public Vector2 Velocity;

        private readonly Texture2D _texture;
        private readonly Vector2 _spawnPosition;
        private Animation _walkAnimation;

        private Vector2 _settledSpawnPosition;
        private bool _hasSettled = false;
        private float _edgeCheckTimer;           

        private int CurrentWidth => (int)((_walkAnimation != null ? _walkAnimation.Frames[0].Width : _texture.Width) * Scale);
        private int CurrentHeight => (int)((_walkAnimation != null ? _walkAnimation.Frames[0].Height : _texture.Height) * Scale);

        private int _direction = 1;

        public Enemy(Texture2D texture, Vector2 startPosition)
        {
            _texture = texture;
            _spawnPosition = startPosition;
            _settledSpawnPosition = startPosition;
            Position = startPosition;
            Velocity = Vector2.Zero;
            _edgeCheckTimer = EdgeCheckCooldown;
        }

        public void LoadAnimation(Animation anim) => _walkAnimation = anim;

        public void Reset()
        {
            Position = _settledSpawnPosition;
            Velocity = Vector2.Zero;
            _direction = 1;
            _edgeCheckTimer = EdgeCheckCooldown;
            _walkAnimation?.Reset();
        }

        public void Update(GameTime gameTime, IReadOnlyList<Platform> platforms)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_edgeCheckTimer > 0f) _edgeCheckTimer -= dt;

            if (_walkAnimation != null && System.Math.Abs(Velocity.X) > 1f)
                _walkAnimation.Update(dt);

            Position += Velocity * dt;

            Velocity.X = _direction * WalkSpeed;
            Position.X += Velocity.X * dt;
            ResolveHorizontalCollisions(platforms);

            Velocity.Y += Gravity * dt;
            Position.Y += Velocity.Y * dt;
            ResolveVerticalCollisions(platforms);

            if (_edgeCheckTimer <= 0f)
                CheckEdgeTurnaround(platforms);

            Velocity *= DampingDecay;

            if (Position.Y > 800f) { Position.Y = 800f; Velocity.Y = 0f; }
        }

        public void FlipDirection() { _direction = -_direction; Velocity.X = 0f; }
        public bool CollidesWith(Rectangle other) => GetBounds().Intersects(other);
        public Rectangle Bounds => GetBounds();

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D tex = _walkAnimation?.GetCurrentFrame() ?? _texture;
            Color color = _walkAnimation != null ? Color.White : Color.Red;
            SpriteEffects fx = _direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(tex, Position, null, color, 0f, Vector2.Zero, Scale, fx, 0f);
        }

        private Rectangle GetBounds()
            => new Rectangle((int)Position.X, (int)Position.Y, CurrentWidth, CurrentHeight);

        private void ResolveHorizontalCollisions(IReadOnlyList<Platform> platforms)
        {
            Rectangle bounds = GetBounds();
            foreach (var platform in platforms)
            {
                if (!bounds.Intersects(platform.Bounds)) continue;
                if (Velocity.X > 0f) Position.X = platform.Bounds.Left - CurrentWidth;
                else if (Velocity.X < 0f) Position.X = platform.Bounds.Right;
                _direction = -_direction;
                Velocity.X = 0f;
                break;
            }
        }

        private void ResolveVerticalCollisions(IReadOnlyList<Platform> platforms)
        {
            Rectangle bounds = GetBounds();
            foreach (var platform in platforms)
            {
                if (!bounds.Intersects(platform.Bounds)) continue;
                if (Velocity.Y >= 0f)
                {
                    Position.Y = platform.Bounds.Top - CurrentHeight;
                    Velocity.Y = 0f;
                    if (!_hasSettled) { _settledSpawnPosition = Position; _hasSettled = true; }
                }
                else if (Velocity.Y < 0f)
                {
                    Position.Y = platform.Bounds.Bottom;
                    Velocity.Y = 0f;
                }
                break;
            }
        }

        private void CheckEdgeTurnaround(IReadOnlyList<Platform> platforms)
        {
            int probeX = (int)(Position.X + (_direction == 1 ? CurrentWidth : 0));
            int probeY = (int)(Position.Y + CurrentHeight + 1);
            foreach (var platform in platforms)
                if (platform.Bounds.Contains(probeX, probeY)) return;
            _direction = -_direction;
        }


    }
}