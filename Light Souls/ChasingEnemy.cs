using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    /// <summary>
    /// A ground-based enemy that patrols normally but accelerates toward the
    /// player when they come within aggro range. Can be temporarily stunned
    /// by the player's stomp attack.
    /// </summary>
    public sealed class ChasingEnemy
    {
        // ── Constants ────────────────────────────────────────────────────────────

        private const float PatrolSpeed  = 70f;
        private const float ChaseSpeed   = 150f;
        private const float Gravity      = 1200f;
        private const float AggroRange   = 200f;
        private const float DampingDecay = 0.95f;

        // ── Public state ─────────────────────────────────────────────────────────

        /// <summary>World-space top-left position.</summary>
        public Vector2 Position;

        /// <summary>Current velocity (includes stomp impulses).</summary>
        public Vector2 Velocity;

        // ── Private fields ───────────────────────────────────────────────────────

        private readonly Texture2D _texture;
        private int   _direction  = 1;
        private float _stunTimer  = 0f;
        private bool  _isStunned  = false;

        // ── Constructor ──────────────────────────────────────────────────────────

        public ChasingEnemy(Texture2D texture, Vector2 startPosition)
        {
            _texture = texture;
            Position = startPosition;
            Velocity = Vector2.Zero;
        }

        // ── Public methods ───────────────────────────────────────────────────────

        public void Update(GameTime gameTime, IReadOnlyList<Platform> platforms, Player player)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Apply external impulse first
            Position += Velocity * dt;

            if (_isStunned)
            {
                UpdateStunned(dt, platforms);
                return;
            }

            UpdateMovement(dt, player, platforms);
        }

        /// <summary>Stuns the enemy for <paramref name="duration"/> seconds.</summary>
        public void Stun(float duration = 1.5f)
        {
            _isStunned = true;
            _stunTimer = duration;
            Velocity.X = 0f;
        }

        /// <returns>True if <paramref name="other"/> overlaps this enemy's bounds.</returns>
        public bool CollidesWith(Rectangle other)
            => GetBounds().Intersects(other);

        public void Draw(SpriteBatch spriteBatch)
            => spriteBatch.Draw(_texture, Position, Color.Orange);

        // ── Private helpers ──────────────────────────────────────────────────────

        private void UpdateStunned(float dt, IReadOnlyList<Platform> platforms)
        {
            _stunTimer -= dt;
            if (_stunTimer <= 0f)
                _isStunned = false;

            // Still affected by gravity while stunned
            Velocity.Y += Gravity * dt;
            Position.Y += Velocity.Y * dt;
            ResolveVerticalCollisions(platforms);
            Velocity.X = 0f;
        }

        private void UpdateMovement(float dt, Player player, IReadOnlyList<Platform> platforms)
        {
            float distX      = player.Position.X - Position.X;
            bool  isAggro    = Math.Abs(distX) < AggroRange;

            if (isAggro)
            {
                _direction = distX > 0f ? 1 : -1;
                Velocity.X = _direction * ChaseSpeed;
            }
            else
            {
                Velocity.X = _direction * PatrolSpeed;
            }

            Position.X += Velocity.X * dt;
            ResolveHorizontalCollisions(platforms, isAggro);

            Velocity.Y += Gravity * dt;
            Position.Y += Velocity.Y * dt;
            ResolveVerticalCollisions(platforms);

            if (!isAggro)
                CheckEdgeTurnaround(platforms);

            Velocity.X *= DampingDecay;

            // Safety floor
            if (Position.Y > 800f)
            {
                Position.Y = 800f;
                Velocity.Y = 0f;
            }
        }

        private Rectangle GetBounds()
            => new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);

        private void ResolveHorizontalCollisions(IReadOnlyList<Platform> platforms, bool isChasing)
        {
            Rectangle bounds = GetBounds();
            foreach (var platform in platforms)
            {
                if (!bounds.Intersects(platform.Bounds)) continue;

                if (Velocity.X > 0f)
                    Position.X = platform.Bounds.Left - _texture.Width;
                else if (Velocity.X < 0f)
                    Position.X = platform.Bounds.Right;

                // Only flip direction when patrolling, not while chasing
                if (!isChasing)
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

                if (Velocity.Y > 0f)
                {
                    Position.Y = platform.Bounds.Top - _texture.Height;
                    Velocity.Y = 0f;
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
            int probeX = (int)(Position.X + (_direction == 1 ? _texture.Width : 0));
            int probeY = (int)(Position.Y + _texture.Height + 1);

            foreach (var platform in platforms)
            {
                if (platform.Bounds.Contains(probeX, probeY))
                    return;
            }

            _direction = -_direction;
        }
    }
}