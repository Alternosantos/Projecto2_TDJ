using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Light_Souls
{
    public class Player
    {
        public Vector2 Position;
        public Vector2 Velocity;
        private Texture2D _texture;

        private float _moveSpeed = 300f;
        private float _jumpPower = -500f;
        private float _gravity = 1600f;
        private int _jumpCount = 0;
        private int _maxJumps = 2;
        private bool _isOnGround;

        private float _stompForce = 800f;
        private bool _isStomping = false;
        private float _stompCooldown = 0f;
        private const float STOMP_COOLDOWN_TIME = 0.5f;

        private float _invincibleTimer = 0f;
        public bool IsInvincible => _invincibleTimer > 0f;

        private bool _previousJumpState = false;

        public System.Action OnJump;
        public System.Action OnCoinPickup;
        public System.Action OnStomp;

        public Player(Texture2D texture, Vector2 startPosition)
        {
            _texture = texture;
            Position = startPosition;
            Velocity = Vector2.Zero;
        }

        public void TakeHit()
        {
            if (IsInvincible) return;
            _invincibleTimer = 0.5f;
        }

        public Rectangle GetBounds()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
        }

        public void Update(GameTime gameTime, List<Platform> platforms,
            List<Enemy> enemies, List<FlyingEnemy> flyingEnemies, List<ChasingEnemy> chasingEnemies)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_invincibleTimer > 0) _invincibleTimer -= deltaTime;
            if (_stompCooldown > 0) _stompCooldown -= deltaTime;
            if (_stompCooldown <= 0) _isStomping = false;

            _isOnGround = IsGrounded(platforms);

            var keyboard = Keyboard.GetState();
            float moveX = 0;
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) moveX = -1;
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) moveX = 1;
            Velocity.X = moveX * _moveSpeed;

            Position.X += Velocity.X * deltaTime;
            HandleHorizontalCollisions(platforms);

            Velocity.Y += _gravity * deltaTime;
            Position.Y += Velocity.Y * deltaTime;
            HandleVerticalCollisions(platforms, enemies, flyingEnemies, chasingEnemies);

            bool jumpPressed = (keyboard.IsKeyDown(Keys.Space) || keyboard.IsKeyDown(Keys.Up)) && !_previousJumpState;
            _previousJumpState = keyboard.IsKeyDown(Keys.Space) || keyboard.IsKeyDown(Keys.Up);

            if (jumpPressed && !_isStomping && (_isOnGround || _jumpCount < _maxJumps))
            {
                Velocity.Y = _jumpPower;
                _jumpCount++;
                _isOnGround = false;
                OnJump?.Invoke();
            }

            bool downPressed = keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down);
            if (!_isOnGround && downPressed && _stompCooldown <= 0 && !_isStomping)
            {
                Velocity.Y = _stompForce;
                _isStomping = true;
                _stompCooldown = STOMP_COOLDOWN_TIME;
                OnStomp?.Invoke();
            }
        }

        private bool IsGrounded(List<Platform> platforms)
        {
            Rectangle playerRect = GetBounds();
            playerRect.Offset(0, 2);

            foreach (var platform in platforms)
            {
                if (playerRect.Intersects(platform.Bounds))
                {
                    _jumpCount = 0;
                    return true;
                }
            }
            return false;
        }

        private void HandleHorizontalCollisions(List<Platform> platforms)
        {
            Rectangle playerRect = GetBounds();
            foreach (var platform in platforms)
            {
                if (playerRect.Intersects(platform.Bounds))
                {
                    if (Velocity.X > 0)
                        Position.X = platform.Bounds.Left - _texture.Width;
                    else if (Velocity.X < 0)
                        Position.X = platform.Bounds.Right;
                    Velocity.X = 0;
                    playerRect = GetBounds();
                }
            }
        }

        private void HandleVerticalCollisions(List<Platform> platforms,
            List<Enemy> enemies, List<FlyingEnemy> flyingEnemies, List<ChasingEnemy> chasingEnemies)
        {
            Rectangle playerRect = GetBounds();
            foreach (var platform in platforms)
            {
                if (playerRect.Intersects(platform.Bounds))
                {
                    if (Velocity.Y > 0)
                    {
                        Position.Y = platform.Bounds.Top - _texture.Height;
                        Velocity.Y = 0;
                        _isOnGround = true;
                        _jumpCount = 0;

                        if (_isStomping)
                        {
                            PushEnemiesAway(enemies, flyingEnemies, chasingEnemies);
                            Velocity.Y = -300f; // ressalta
                            _isStomping = false;
                        }
                    }
                    else if (Velocity.Y < 0)
                    {
                        Position.Y = platform.Bounds.Bottom;
                        Velocity.Y = 0;
                    }
                    playerRect = GetBounds();
                }
            }
        }

        private void PushEnemiesAway(List<Enemy> enemies, List<FlyingEnemy> flyingEnemies, List<ChasingEnemy> chasingEnemies)
        {
            float pushForce = 1000f;
            Vector2 playerCenter = new Vector2(Position.X + _texture.Width / 2, Position.Y + _texture.Height / 2);
            float radius = 100f;


            
            foreach (var enemy in enemies)
            {
                Vector2 enemyCenter = new Vector2(enemy.Position.X + _texture.Width / 2, enemy.Position.Y + _texture.Height / 2);
                if (Vector2.Distance(playerCenter, enemyCenter) < radius)
                {
                    enemy.FlipDirection();
                }
            }


            foreach (var flying in flyingEnemies)
            {
                Vector2 enemyCenter = new Vector2(flying.Position.X + _texture.Width / 2, flying.Position.Y + _texture.Height / 2);
                if (Vector2.Distance(playerCenter, enemyCenter) < radius)
                {
                    float dir = (enemyCenter.X < playerCenter.X) ? -1f : 1f;
                    flying.Velocity = new Vector2(dir * pushForce, -200f);
                    flying.MarkToReturn(2f);
                }
            }

            
            foreach (var chasing in chasingEnemies)
            {
                Vector2 enemyCenter = new Vector2(chasing.Position.X + _texture.Width / 2, chasing.Position.Y + _texture.Height / 2);
                if (Vector2.Distance(playerCenter, enemyCenter) < radius)
                {
                    chasing.Stun(1.5f);
                    // não altera a velocidade - fica parado onde está
                }
            }
        }
        public void CollectCoins(List<Coin> coins)
        {
            Rectangle playerBounds = GetBounds();
            foreach (var coin in coins)
            {
                if (!coin.IsCollected && coin.GetBounds().Intersects(playerBounds))
                {
                    coin.IsCollected = true;
                    OnCoinPickup?.Invoke();
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsInvincible && (int)(_invincibleTimer * 30) % 2 == 0) return;
            spriteBatch.Draw(_texture, Position, Color.White);
        }
    }
}