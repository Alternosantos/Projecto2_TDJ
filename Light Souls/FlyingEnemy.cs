using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    /// <summary>
    /// An airborne enemy that oscillates horizontally between two bounds.
    /// Can be knocked away by the player's stomp and gradually returns to
    /// its original altitude.
    /// </summary>
    public sealed class FlyingEnemy
    {
        // ── Constants ────────────────────────────────────────────────────────────

        private const float PatrolSpeed = 80f;
        private const float DampingDecay = 0.95f;

        // ── Public state ─────────────────────────────────────────────────────────

       
        public Vector2 Position;

        
        public Vector2 Velocity;

       

        private readonly Texture2D _texture;
        private readonly float _leftBound;
        private readonly float _rightBound;
        private readonly float _originalY;

        private int   _direction    = 1;
        private float _returnTimer  = 0f;
        private bool  _returningToY = false;

     

        public FlyingEnemy(Texture2D texture, Vector2 startPosition,
                           float leftBound, float rightBound)
        {
            _texture    = texture;
            Position    = startPosition;
            Velocity    = Vector2.Zero;
            _leftBound  = leftBound;
            _rightBound = rightBound;
            _originalY  = startPosition.Y;
        }

       

        public void Update(GameTime gameTime, IReadOnlyList<Platform> platforms)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Apply external impulse
            Position += Velocity * dt;

            // Smoothly return to original altitude after being knocked away
            if (_returningToY)
            {
                _returnTimer -= dt;
                if (_returnTimer <= 0f)
                {
                    Position.Y    = _originalY;
                    Velocity.Y    = 0f;
                    _returningToY = false;
                }
                else
                {
                    Position.Y = MathHelper.Lerp(Position.Y, _originalY, 0.05f);
                }
            }

            // Normal horizontal patrol (suppressed while a strong knockback is active)
            bool knockedBack = System.Math.Abs(Velocity.X) >= PatrolSpeed * 1.5f;
            if (!knockedBack && !_returningToY)
                Position.X += _direction * PatrolSpeed * dt;

            // Enforce horizontal patrol bounds
            if (Position.X <= _leftBound)
            {
                Position.X = _leftBound;
                _direction = 1;
            }
            else if (Position.X + _texture.Width >= _rightBound)
            {
                Position.X = _rightBound - _texture.Width;
                _direction = -1;
            }

            Velocity *= DampingDecay;
        }

        
        public void MarkToReturn(float delay = 2f)
        {
            _returningToY = true;
            _returnTimer  = delay;
        }

      
        public bool CollidesWith(Rectangle other)
            => GetBounds().Intersects(other);

        public void Draw(SpriteBatch spriteBatch)
            => spriteBatch.Draw(_texture, Position, Color.Purple);

       

        private Rectangle GetBounds()
            => new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
    }
}