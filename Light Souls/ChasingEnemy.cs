using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    public sealed class ChasingEnemy
    {
        // ── Constants ────────────────────────────────────────────────────────────

        private const float PatrolSpeed = 70f;
        private const float ChaseSpeed = 80f;
        private const float Gravity = 1200f;
        private const float AggroRange = 100f;
        private const float DampingDecay = 0.95f;

        // ── Public state ─────────────────────────────────────────────────────────

        public Vector2 Position;
        public Vector2 Velocity;

        // ── Private fields ───────────────────────────────────────────────────────

        private readonly Texture2D _texture;
        private readonly Vector2 _spawnPosition; // ← posição de spawn guardada
        private int _direction = 1;
        private float _stunTimer = 0f;
        private bool _isStunned = false;
        // ── World bounds ─────────────────────────────────────────────────────────

        private int _worldWidth = int.MaxValue;
        private int _worldHeight = int.MaxValue; private int _worldRotation = 0;

        // ── Constructor ──────────────────────────────────────────────────────────

        public ChasingEnemy(Texture2D texture, Vector2 startPosition)
        {
            _texture = texture;
            _spawnPosition = startPosition; // ← guardado uma vez, nunca muda
            Position = startPosition;
            Velocity = Vector2.Zero;
        }

        // ── Public methods ───────────────────────────────────────────────────────

        /// <summary>
        /// Volta à posição de spawn e limpa todo o estado.
        /// Chama isto quando o player morre.
        /// </summary>
        public void Reset()
        {
            Position = _spawnPosition;
            Velocity = Vector2.Zero;
            _direction = 1;
            _isStunned = false;
            _stunTimer = 0f;
        }

        /// <summary>
        /// Informs the player of the level boundaries so it can clamp its
        /// position and detect falls.
        /// </summary>
        public void SetWorldBounds(int width, int height)
        {
            _worldWidth = width;
            _worldHeight = height;
        }

        public void Update(GameTime gameTime, IReadOnlyList<Platform> platforms, Player player)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;


            Position += Velocity * dt;

            if (_isStunned)
            {
                UpdateStunned(dt, platforms);
                return;
            }

            UpdateMovement(dt, player, platforms);
        }

        public void Stun(float duration = 1.5f)
        {
            _isStunned = true;
            _stunTimer = duration;
            Velocity.X = 0f;
        }

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

            Velocity.Y += Gravity * dt;
            Position.Y += Velocity.Y * dt;
            ResolveVerticalCollisions(platforms);
            Velocity.X = 0f;
        }

        private void UpdateMovement(float dt, Player player, IReadOnlyList<Platform> platforms)
        {
            float distX = player.Position.X - Position.X;
            bool isAggro = Math.Abs(distX) < AggroRange;

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