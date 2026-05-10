using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Light_Souls
{
    public class Player
    {
        // Position and hitbox (gameplay collider)
        public Vector2 Position;      // position of the hitbox center-bottom?
        public Vector2 Velocity;

        // Animation

        private Animation _currentAnimation;
        private bool _facingRight = true;

        // Dimensões do sprite (da sprite sheet)
        private const int SPRITE_WIDTH = 40;
        private const int SPRITE_HEIGHT = 50;
        private const int HITBOX_WIDTH = 40;
        private const int HITBOX_HEIGHT = 50;
        private Vector2 _drawOffset;

        // Death & respawn
        private bool _isDead = false;
        private float _deathTimer = 0f;
        private const float DEATH_RESPAWN_TIME = 1.2f;
        private Vector2 _startPosition;

        // Movement settings
        private float _moveSpeed = 300f;
        private float _jumpPower = -500f;
        private float _gravity = 1600f;
        private int _jumpCount = 0;
        private int _maxJumps = 2;
        private bool _isOnGround;

        // Stomp
        private float _stompForce = 800f;
        private bool _isStomping = false;
        private float _stompCooldown = 0f;
        private const float STOMP_COOLDOWN_TIME = 0.5f;

        // Invincibility
        private float _invincibleTimer = 0f;
        public bool IsInvincible => _invincibleTimer > 0f && !_isDead;

        private bool _previousJumpState = false;

        // Sound events
        public System.Action OnJump;
        public System.Action OnCoinPickup;
        public System.Action OnStomp;

        // Constructor
        public Player(Texture2D dummyTexture, Vector2 startPosition)
        {
            _startPosition = startPosition;
            Position = startPosition;
            Velocity = Vector2.Zero;
            // Hitbox centrada: o sprite deve ser desenhado com o fundo centralizado na hitbox
            _drawOffset = new Vector2(-SPRITE_WIDTH / 2, -SPRITE_HEIGHT + HITBOX_HEIGHT / 2);
        }

        // Load animations
        private Animation _idleAnimation, _runAnimation, _jumpAnimation, _deathAnimation;

        public void LoadAnimations(Animation idle, Animation run, Animation jump, Animation dead)
        {
            _idleAnimation = idle;
            _runAnimation = run;
            _jumpAnimation = jump;
            _deathAnimation = dead;
            _currentAnimation = _idleAnimation;

            // Verificação simples
            if (_idleAnimation == null || _runAnimation == null || _jumpAnimation == null || _deathAnimation == null)
                throw new Exception("Uma das animações é nula.");
        }

        // Hitbox rectangle (used for collision)
        public Rectangle GetBounds()
        {
            return new Rectangle(
                (int)(Position.X - HITBOX_WIDTH / 2),
                (int)(Position.Y - HITBOX_HEIGHT / 2),
                HITBOX_WIDTH,
                HITBOX_HEIGHT
            );
        }

        private void SetAnimation(Animation animation)
        {
            if (animation == null) return;  // prevent null reference
            if (_currentAnimation == animation) return;
            _currentAnimation = animation;
            _currentAnimation.Reset();
        }

        public void Kill()
        {
            if (_isDead) return;
            _isDead = true;
            _deathTimer = DEATH_RESPAWN_TIME;
            Velocity = Vector2.Zero;
            SetAnimation(_deathAnimation);
        }

        public void TakeHit()
        {
            if (IsInvincible || _isDead) return;
            _invincibleTimer = 0.5f;
        }

        public void Update(GameTime gameTime, List<Platform> platforms,
            List<Enemy> enemies, List<FlyingEnemy> flyingEnemies, List<ChasingEnemy> chasingEnemies)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_invincibleTimer > 0) _invincibleTimer -= deltaTime;

            if (_isDead)
            {
                _deathTimer -= deltaTime;
                if (_deathTimer <= 0)
                {
                    _isDead = false;
                    Position = _startPosition;
                    Velocity = Vector2.Zero;
                    _invincibleTimer = 0.5f;
                    SetAnimation(_idleAnimation);
                }
                else
                {
                    _deathAnimation.Update(deltaTime);
                    if (_deathAnimation.IsFinished && _deathTimer > 0)
                        _deathAnimation.Reset();
                }
                return;
            }

            // --- Normal living logic ---
            if (_stompCooldown > 0) _stompCooldown -= deltaTime;
            if (_stompCooldown <= 0) _isStomping = false;

            _isOnGround = IsGrounded(platforms);

            // Horizontal input
            var keyboard = Keyboard.GetState();
            float moveX = 0;
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) moveX = -1;
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) moveX = 1;
            Velocity.X = moveX * _moveSpeed;

            if (moveX != 0) _facingRight = moveX > 0;

            // Horizontal movement
            Position.X += Velocity.X * deltaTime;
            HandleHorizontalCollisions(platforms);

            // Gravity & vertical movement
            Velocity.Y += _gravity * deltaTime;
            Position.Y += Velocity.Y * deltaTime;
            HandleVerticalCollisions(platforms, enemies, flyingEnemies, chasingEnemies);

            // Jump input (edge triggered)
            bool jumpPressed = (keyboard.IsKeyDown(Keys.Space) || keyboard.IsKeyDown(Keys.Up)) && !_previousJumpState;
            _previousJumpState = keyboard.IsKeyDown(Keys.Space) || keyboard.IsKeyDown(Keys.Up);

            if (jumpPressed && !_isStomping && (_isOnGround || _jumpCount < _maxJumps))
            {
                Velocity.Y = _jumpPower;
                _jumpCount++;
                _isOnGround = false;
                OnJump?.Invoke();
            }

            // Stomp (Down while in air)
            bool downPressed = keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down);
            if (!_isOnGround && downPressed && _stompCooldown <= 0 && !_isStomping)
            {
                Velocity.Y = _stompForce;
                _isStomping = true;
                _stompCooldown = STOMP_COOLDOWN_TIME;
                OnStomp?.Invoke();
            }

            // Choose animation
            if (!_isOnGround)
                SetAnimation(_jumpAnimation);
            else if (System.Math.Abs(Velocity.X) > 0.1f)
                SetAnimation(_runAnimation);
            else
                SetAnimation(_idleAnimation);

            _currentAnimation.Update(deltaTime);
        }

        private bool IsGrounded(List<Platform> platforms)
        {
            // Cria um rectângulo ligeiramente abaixo da hitbox (2 pixels)
            Rectangle feetRect = new Rectangle(
                (int)(Position.X - HITBOX_WIDTH / 2),
                (int)(Position.Y + HITBOX_HEIGHT / 2),
                HITBOX_WIDTH,
                2
            );

            foreach (var platform in platforms)
            {
                if (feetRect.Intersects(platform.Bounds))
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
                        Position.X = platform.Bounds.Left - HITBOX_WIDTH / 2;
                    else if (Velocity.X < 0)
                        Position.X = platform.Bounds.Right + HITBOX_WIDTH / 2;
                    Velocity.X = 0;
                    playerRect = GetBounds();
                }
            }
        }

        private void HandleVerticalCollisions(List<Platform> platforms,List<Enemy> enemies, List<FlyingEnemy> flyingEnemies, List<ChasingEnemy> chasingEnemies)
        {
            Rectangle playerRect = GetBounds();
            foreach (var platform in platforms)
            {
                if (playerRect.Intersects(platform.Bounds))
                {
                    if (Velocity.Y > 0) // a cair
                    {
                        // Colocar o fundo da hitbox exactamente no topo da plataforma
                        Position.Y = platform.Bounds.Top - HITBOX_HEIGHT / 2;
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
                    else if (Velocity.Y < 0) // a subir (bate com cabeça)
                    {
                        // Colocar o topo da hitbox no fundo da plataforma
                        Position.Y = platform.Bounds.Bottom + HITBOX_HEIGHT / 2;
                        Velocity.Y = 0;
                    }
                    playerRect = GetBounds();
                }
            }
        }

        private void PushEnemiesAway(List<Enemy> enemies, List<FlyingEnemy> flyingEnemies, List<ChasingEnemy> chasingEnemies)
        {
            float pushForce = 1000f;
            Vector2 playerCenter = new Vector2(Position.X, Position.Y - HITBOX_HEIGHT / 2);
            float radius = 100f;

            foreach (var enemy in enemies)
            {
                Vector2 enemyCenter = new Vector2(enemy.Position.X + 16, enemy.Position.Y + 16);
                if (Vector2.Distance(playerCenter, enemyCenter) < radius)
                    enemy.FlipDirection();
            }

            foreach (var flying in flyingEnemies)
            {
                Vector2 enemyCenter = new Vector2(flying.Position.X + 16, flying.Position.Y + 16);
                if (Vector2.Distance(playerCenter, enemyCenter) < radius)
                {
                    float dir = (enemyCenter.X < playerCenter.X) ? -1f : 1f;
                    flying.Velocity = new Vector2(dir * pushForce, -200f);
                    flying.MarkToReturn(2f);
                }
            }

            foreach (var chasing in chasingEnemies)
            {
                Vector2 enemyCenter = new Vector2(chasing.Position.X + 16, chasing.Position.Y + 16);
                if (Vector2.Distance(playerCenter, enemyCenter) < radius)
                    chasing.Stun(1.5f);
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
            if (_isDead || _currentAnimation == null) return;

            // Efeito de piscar quando invencível
            if (IsInvincible && (DateTime.Now.Millisecond / 100) % 2 == 0) return;

            Texture2D currentTexture = _currentAnimation.GetCurrentFrame();

            SpriteEffects effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Calculamos a origem (centro horizontal, fundo vertical)
            Vector2 origin = new Vector2(currentTexture.Width / 2f, currentTexture.Height);

            // Desenhamos na posição exata do pé do personagem
            // Nota: Ajusta o Position.Y se a tua colisão usar o centro da hitbox
            spriteBatch.Draw(currentTexture, Position, null, Color.White, 0f, origin, 1f, effects, 0f);
        }
    }
}