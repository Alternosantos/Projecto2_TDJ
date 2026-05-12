using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    /// <summary>
    /// A ground-based enemy that patrols a platform and turns around at edges
    /// or walls. Can be temporarily knocked back by the player's stomp.
    /// </summary>
    public sealed class Enemy
    {
        // ── Constants ────────────────────────────────────────────────────────────

        private const float WalkSpeed    = 100f;
        private const float Gravity      = 1200f;
        private const float DampingDecay = 0.95f;   // velocity damping per frame

        // ── Public state ─────────────────────────────────────────────────────────

        /// <summary>World-space top-left position of the enemy sprite.</summary>
        public Vector2 Position;

        /// <summary>Current velocity (may include external impulses from stomp).</summary>
        public Vector2 Velocity;

        // ── Private fields ───────────────────────────────────────────────────────

        private readonly Texture2D _texture;
        private int _direction = 1;   // +1 = right, -1 = left

        // ── Constructor ──────────────────────────────────────────────────────────

        public Enemy(Texture2D texture, Vector2 startPosition)
        {
            _texture = texture;
            Position = startPosition;
            Velocity = Vector2.Zero;
        }

        // ── Public methods ───────────────────────────────────────────────────────

        public void Update(GameTime gameTime, IReadOnlyList<Platform> platforms)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Apply any external impulse (e.g. stomp knockback)
            Position += Velocity * dt;

            // Regular walking movement
            Velocity.X  = _direction * WalkSpeed;
            Position.X += Velocity.X * dt;
            ResolveHorizontalCollisions(platforms);

            // Gravity
            Velocity.Y += Gravity * dt;
            Position.Y += Velocity.Y * dt;
            ResolveVerticalCollisions(platforms);

            // Turn around at platform edges
            CheckEdgeTurnaround(platforms);

            // Gradually damp the external impulse component
            Velocity *= DampingDecay;

            // Safety: stop falling through the floor far below the level
            if (Position.Y > 800f)
            {
                Position.Y = 800f;
                Velocity.Y = 0f;
            }
        }

        /// <summary>Reverses the patrol direction (called by stomp or wall collision).</summary>
        public void FlipDirection()
        {
            _direction = -_direction;
            Velocity.X = 0f;
        }

        /// <returns>True if <paramref name="other"/> overlaps this enemy's bounds.</returns>
        public bool CollidesWith(Rectangle other)
            => GetBounds().Intersects(other);

        public void Draw(SpriteBatch spriteBatch)
            => spriteBatch.Draw(_texture, Position, Color.Red);

        // ── Private helpers ──────────────────────────────────────────────────────

        private Rectangle GetBounds()
            => new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);

        private void ResolveHorizontalCollisions(IReadOnlyList<Platform> platforms)
        {
            Rectangle bounds = GetBounds();
            foreach (var platform in platforms)
            {
                if (!bounds.Intersects(platform.Bounds)) continue;

                if (Velocity.X > 0f)
                    Position.X = platform.Bounds.Left - _texture.Width;
                else if (Velocity.X < 0f)
                    Position.X = platform.Bounds.Right;

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
            // Probe one pixel below the leading foot
            int probeX = (int)(Position.X + (_direction == 1 ? _texture.Width : 0));
            int probeY = (int)(Position.Y + _texture.Height + 1);

            foreach (var platform in platforms)
            {
                if (platform.Bounds.Contains(probeX, probeY))
                    return; // there is ground ahead — no need to turn
            }

            _direction = -_direction;
        }
    }
}