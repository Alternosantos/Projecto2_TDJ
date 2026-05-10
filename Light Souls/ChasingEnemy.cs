using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Light_Souls
{
    public class ChasingEnemy
    {
        public Vector2 Position;
        public Vector2 Velocity;
        private Texture2D _texture;
        private float _walkSpeed = 70f;
        private float _chaseSpeed = 100f;
        private float _gravity = 1200f;
        private int _direction = 1;
        private float _aggroRange = 100f;
        private float _stunTimer = 0f;
        private bool _isStunned = false;

        public ChasingEnemy(Texture2D texture, Vector2 startPosition)
        {
            _texture = texture;
            Position = startPosition;
            Velocity = Vector2.Zero;
        }

        public void Update(GameTime gameTime, List<Platform> platforms, Player player)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Aplica empurrão (velocidade do stomp)
            Position.X += Velocity.X * deltaTime;
            Position.Y += Velocity.Y * deltaTime;

            if (_isStunned)
            {
                _stunTimer -= deltaTime;
                if (_stunTimer <= 0)
                {
                    _isStunned = false;
                }
                else
                {
                    // Durante stun, só gravidade e colisões verticais (não anda)
                    Velocity.Y += _gravity * deltaTime;
                    Position.Y += Velocity.Y * deltaTime;
                    HandleVerticalCollisions(platforms);
                    Velocity.X = 0;
                    return;
                }
            }

            // Decidir velocidade (perseguição ou patrulha)
            float distanceToPlayer = player.Position.X - Position.X;
            bool playerIsClose = System.Math.Abs(distanceToPlayer) < _aggroRange;

            if (playerIsClose)
            {
                if (distanceToPlayer > 0) _direction = 1;
                else _direction = -1;
                Velocity.X = _direction * _chaseSpeed;
            }
            else
            {
                Velocity.X = _direction * _walkSpeed;
            }

            Position.X += Velocity.X * deltaTime;
            HandleHorizontalCollisions(platforms);

            Velocity.Y += _gravity * deltaTime;
            Position.Y += Velocity.Y * deltaTime;
            HandleVerticalCollisions(platforms);

            if (!playerIsClose)
                CheckTurnAround(platforms);

            Velocity.X *= 0.95f;
            
            if (Position.Y > 800) 
            {
                Position.Y = 800;   
                Velocity.Y = 0;
            }
        }

        private void HandleHorizontalCollisions(List<Platform> platforms)
        {
            Rectangle enemyRect = new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
            foreach (var platform in platforms)
            {
                if (enemyRect.Intersects(platform.Bounds))
                {
                    if (Velocity.X > 0)
                        Position.X = platform.Bounds.Left - _texture.Width;
                    else if (Velocity.X < 0)
                        Position.X = platform.Bounds.Right;
                    if (System.Math.Abs(Velocity.X) == _walkSpeed)
                        _direction = -_direction;
                    Velocity.X = 0;
                    break;
                }
            }
        }

        private void HandleVerticalCollisions(List<Platform> platforms)
        {
            Rectangle enemyRect = new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
            foreach (var platform in platforms)
            {
                if (enemyRect.Intersects(platform.Bounds))
                {
                    if (Velocity.Y > 0)
                    {
                        Position.Y = platform.Bounds.Top - _texture.Height;
                        Velocity.Y = 0;
                    }
                    else if (Velocity.Y < 0)
                    {
                        Position.Y = platform.Bounds.Bottom;
                        Velocity.Y = 0;
                    }
                    break;
                }
            }
        }

        private void CheckTurnAround(List<Platform> platforms)
        {
            int frontX = (int)(Position.X + (_direction == 1 ? _texture.Width : 0));
            int footY = (int)(Position.Y + _texture.Height) + 1;
            bool hasGround = false;
            foreach (var platform in platforms)
            {
                if (platform.Bounds.Contains(frontX, footY))
                {
                    hasGround = true;
                    break;
                }
            }
            if (!hasGround)
                _direction = -_direction;
        }

        public void Stun(float duration = 1.5f)
        {
            _isStunned = true;
            _stunTimer = duration;
            Velocity.X = 0;
        }

        public Rectangle GetBounds()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
        }

        public bool CollidesWith(Rectangle playerBounds)
        {
            return GetBounds().Intersects(playerBounds);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, Position, Color.Orange);
        }
    }
}