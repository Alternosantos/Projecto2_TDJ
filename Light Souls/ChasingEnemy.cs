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
        private const float EdgeCheckCooldown = 0.3f; // segundos sem edge-check após reset
        private const float Scale = 0.5f;

        // ── Public state ─────────────────────────────────────────────────────────

        public Vector2 Position;
        public Vector2 Velocity;

        // ── Private fields ───────────────────────────────────────────────────────

        private readonly Texture2D _texture;
        private readonly Vector2 _spawnPosition;
        private Animation _runAnimation;

        private int CurrentWidth => (int)((_runAnimation != null ? _runAnimation.Frames[0].Width : _texture.Width) * Scale);
        private int CurrentHeight => (int)((_runAnimation != null ? _runAnimation.Frames[0].Height : _texture.Height) * Scale);

        // Posição gravada depois de aterrar pela primeira vez
        private Vector2 _settledSpawnPosition;
        private bool _hasSettled = false;

        // Cooldown que desativa o edge-check nos primeiros frames após reset
        private float _edgeCheckTimer = 0f;

        private int _direction = 1;
        private float _stunTimer = 0f;
        private bool _isStunned = false;

        // ── World bounds ─────────────────────────────────────────────────────────

        private int _worldWidth = int.MaxValue;
        private int _worldHeight = int.MaxValue;

        // ── Constructor ──────────────────────────────────────────────────────────

        public ChasingEnemy(Texture2D texture, Vector2 startPosition)
        {
            _texture = texture;
            _spawnPosition = startPosition;
            _settledSpawnPosition = startPosition;
            Position = startPosition;
            Velocity = Vector2.Zero;
        }

        // ── Public methods ───────────────────────────────────────────────────────

        public void LoadAnimation(Animation anim)
        {
            _runAnimation = anim;
        }

        public void SetWorldBounds(int width, int height)
        {
            _worldWidth = width;
            _worldHeight = height;
        }

        public void Reset()
        {
            Position = _settledSpawnPosition;
            Velocity = Vector2.Zero;
            _direction = 1;
            _isStunned = false;
            _stunTimer = 0f;
            _edgeCheckTimer = EdgeCheckCooldown; // desativa edge-check brevemente
            _runAnimation?.Reset();
        }

        public void Update(GameTime gameTime, IReadOnlyList<Platform> platforms, Player player)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_edgeCheckTimer > 0f)
                _edgeCheckTimer -= dt;

            if (_runAnimation != null && Math.Abs(Velocity.X) > 1f && !_isStunned)
                _runAnimation.Update(dt);

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

        public void FlipDirection()
        {
            _direction = -_direction;
            Velocity.X = 0f;
        }

        public bool CollidesWith(Rectangle other)
            => GetBounds().Intersects(other);

        public Rectangle Bounds => GetBounds();

        public void Draw(SpriteBatch spriteBatch)
        {
            Texture2D tex = _runAnimation?.GetCurrentFrame() ?? _texture;
            Color color = _runAnimation != null ? Color.White : Color.Orange;
            SpriteEffects effect = _direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            
            spriteBatch.Draw(tex, Position, null, color, 0f, Vector2.Zero, Scale, effect, 0f);
        }

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

            // Edge check só corre depois do cooldown ter expirado
            if (!isAggro && _edgeCheckTimer <= 0f)
                CheckEdgeTurnaround(platforms);

            Velocity.X *= DampingDecay;

            if (Position.Y > _worldHeight + 300f)
            {
                Position.Y = _worldHeight + 300f;
                Velocity.Y = 0f;
            }
        }

        private Rectangle GetBounds()
            => new Rectangle((int)Position.X, (int)Position.Y, CurrentWidth, CurrentHeight);

        private void ResolveHorizontalCollisions(IReadOnlyList<Platform> platforms, bool isChasing)
        {
            Rectangle bounds = GetBounds();
            foreach (var platform in platforms)
            {
                if (!bounds.Intersects(platform.Bounds)) continue;

                if (Velocity.X > 0f)
                    Position.X = platform.Bounds.Left - CurrentWidth;
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
                    Position.Y = platform.Bounds.Top - CurrentHeight;
                    Velocity.Y = 0f;

                    // Grava a posição estabilizada na primeira aterragem
                    if (!_hasSettled)
                    {
                        _settledSpawnPosition = Position;
                        _hasSettled = true;
                    }
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