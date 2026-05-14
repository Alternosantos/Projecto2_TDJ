using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Light_Souls
{
    /// <summary>
    /// The player character. Handles input, physics, animation selection,
    /// collision resolution, coin collection, and death/respawn logic.
    /// </summary>
    public sealed class Player
    {
        // ── Physics constants ────────────────────────────────────────────────────

        private const float MoveSpeed = 300f;
        private const float JumpPower = -550f;
        private const float Gravity = 1600f;
        //private const float StompBounceSpeed  = -300f;
        //private const float StompForce        = 800f;          
        private const float StompCooldown = 0.5f;
        private const float RespawnDelay = 3f;
        private const float PostRespawnIFrames = 0.5f;
        private const float HitIFrameDuration = 0.5f;
        private const int MaxJumps = 2;

        // ── Sprite / hitbox dimensions ───────────────────────────────────────────

        private const int HitboxWidth = 50;
        private const int HitboxHeight = 60;

        // ── Public state ─────────────────────────────────────────────────────────

        /// <summary>World-space centre of the player's hitbox.</summary>
        public Vector2 Position;

        /// <summary>Current frame velocity in pixels per second.</summary>
        public Vector2 Velocity;

        /// <summary>True while the player's death animation is playing.</summary>
        public bool IsDead => _isDead;

        /// <summary>True while the player cannot take damage.</summary>
        public bool IsInvincible => _invincibleTimer > 0f && !_isDead;
        public Action OnRespawn;

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired once when the player leaves the ground via a jump.</summary>
        public Action OnJump;

        /// <summary>Fired once each time a coin is collected.</summary>
        public Action OnCoinPickup;

        // ── Animation ────────────────────────────────────────────────────────────

        private Animation _currentAnimation;
        private Animation _idleAnimation;
        private Animation _runAnimation;
        private Animation _jumpAnimation;
        private Animation _deathAnimation;
        private bool _facingRight = true;
        private Animation _attackAnimation;
        private bool _isAttacking;
        private float _attackTimer;
        private const float AttackDuration = 0.3f;


        // ── Physics state ────────────────────────────────────────────────────────

        private bool _isOnGround;
        private int _jumpCount;
        private bool _jumpWasPressed;   // edge-detection for jump input
        private float _invincibleTimer;

        // ── Stomp state ──────────────────────────────────────────────────────────

        private bool _isStomping;
        private float _stompCooldownTimer;
        private bool _stompWasPressed;
        public Action OnStomp;


        // ── Death / respawn ───────────────────────────────────────────────────────

        private bool _isDead;
        private float _deathTimer;
        private Vector2 _spawnPosition;

        // ── World bounds ─────────────────────────────────────────────────────────

        private int _worldWidth = int.MaxValue;
        private int _worldHeight = int.MaxValue;

        // ── Constructor ──────────────────────────────────────────────────────────

        public Player(Texture2D texture, Vector2 spawnPosition)
        {
            _spawnPosition = spawnPosition;
            Position = spawnPosition;
            Velocity = Vector2.Zero;
        }

        // ── Public setup ─────────────────────────────────────────────────────────

        /// <summary>
        /// Attaches the four animations that drive the player's visual state.
        /// Must be called before the first <see cref="Update"/> call.
        /// </summary>
        public void LoadAnimations(Animation idle, Animation run,
                                   Animation jump, Animation dead, Animation attack)
        {
            _idleAnimation = idle ?? throw new ArgumentNullException(nameof(idle));
            _runAnimation = run ?? throw new ArgumentNullException(nameof(run));
            _jumpAnimation = jump ?? throw new ArgumentNullException(nameof(jump));
            _deathAnimation = dead ?? throw new ArgumentNullException(nameof(dead));
            _attackAnimation = attack ?? throw new ArgumentNullException(nameof(attack));
            _currentAnimation = _idleAnimation;
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

        // ── Core game loop ────────────────────────────────────────────────────────

        /// <summary>Updates physics, input, collision, and animation each frame.</summary>
        public void Update(GameTime gameTime,
                           IReadOnlyList<Platform> platforms,
                           IReadOnlyList<Enemy> enemies,
                           IReadOnlyList<FlyingEnemy> flyingEnemies,
                           IReadOnlyList<ChasingEnemy> chasingEnemies)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_invincibleTimer > 0f) _invincibleTimer -= dt;

            if (_isDead)
            {
                UpdateDead(dt);
                return;
            }
            if (_isAttacking)
            {
                _attackTimer -= dt;
                if (_attackTimer <= 0f)
                {
                    _isAttacking = false;

                }
            }
            UpdateStompCooldown(dt);
            ReadInput(dt, platforms);
            ApplyGravityAndMove(dt, platforms, enemies, flyingEnemies, chasingEnemies);
            SelectAnimation();
            _currentAnimation?.Update(dt);
            EnforceWorldBounds();
        }

        /// <summary>Instantly kills the player and starts the death sequence.</summary>
        public void Kill()
        {
            if (_isDead) return;

            _isDead = true;
            _deathTimer = RespawnDelay;
            Velocity = Vector2.Zero;

            _deathAnimation.Reset();
            ChangeAnimation(_deathAnimation);
        }

        /// <summary>
        /// Grants a brief invincibility window (used by normal-enemy hits).
        /// Has no effect if the player is already invincible or dead.
        /// </summary>
        public void TakeHit()
        {
            if (IsInvincible || _isDead) return;
            _invincibleTimer = HitIFrameDuration;
        }

        /// <summary>
        /// Checks each coin in <paramref name="coins"/> and marks any that
        /// overlap the player's hitbox as collected.
        /// </summary>
        public void CollectCoins(IReadOnlyList<Coin> coins)
        {
            Rectangle bounds = GetBounds();
            foreach (var coin in coins)
            {
                if (!coin.IsCollected && coin.GetBounds().Intersects(bounds))
                {
                    coin.IsCollected = true;
                    OnCoinPickup?.Invoke();
                }
            }
        }

        /// <returns>The world-space AABB used for all collision tests.</returns>
        public Rectangle GetBounds()
            => new Rectangle(
                (int)(Position.X - HitboxWidth / 2),
                (int)(Position.Y - HitboxHeight / 2),
                HitboxWidth, HitboxHeight);

        /// <summary>Draws the player, applying a flicker effect while invincible.</summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (_currentAnimation == null) return;

            // Flicker effect: skip every other 100ms slice while invincible
            if (IsInvincible && (DateTime.Now.Millisecond / 100) % 2 == 0) return;

            var effects = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            var destRect = new Rectangle(
                (int)Position.X - HitboxWidth / 2,
                (int)Position.Y - HitboxHeight / 2,
                HitboxWidth, HitboxHeight);

            spriteBatch.Draw(_currentAnimation.GetCurrentFrame(),
                             destRect, null, Color.White, 0f, Vector2.Zero, effects, 0f);
        }

        // ── Private update helpers ───────────────────────────────────────────────

        private void UpdateDead(float dt)
        {
            _deathTimer -= dt;
            _deathAnimation?.Update(dt);

            if (_deathTimer <= 0f)
                Respawn();
        }

        private void Respawn()
        {
            _isDead = false;
            Position = _spawnPosition;
            Velocity = Vector2.Zero;
            _invincibleTimer = PostRespawnIFrames;
            _jumpCount = 0;
            ChangeAnimation(_idleAnimation);
            OnRespawn?.Invoke();
        }

        private void UpdateStompCooldown(float dt)
        {
            if (_stompCooldownTimer > 0f)
            {
                _stompCooldownTimer -= dt;
                if (_stompCooldownTimer <= 0f)
                    _isStomping = false;
            }
        }



        private void ReadInput(float dt, IReadOnlyList<Platform> platforms)
        {
            _isOnGround = IsGrounded(platforms);

            var kb = Keyboard.GetState();
            bool left = kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left);
            bool right = kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right);
            bool jumpHeld = kb.IsKeyDown(Keys.Space) || kb.IsKeyDown(Keys.Up);

            // Horizontal movement
            float moveX = 0f;
            if (left) moveX = -1f;
            if (right) moveX = 1f;

            Velocity.X = moveX * MoveSpeed;
            if (moveX != 0f) _facingRight = moveX > 0f;

            // Jump (detect press edge)
            bool jumpPressed = jumpHeld && !_jumpWasPressed;
            _jumpWasPressed = jumpHeld;

            if (jumpPressed && !_isStomping && (_isOnGround || _jumpCount < MaxJumps))
            {
                Velocity.Y = JumpPower;
                _jumpCount++;
                _isOnGround = false;
                OnJump?.Invoke();
            }

            // --- STOMP (tecla E) -------------------------------------------------
            bool stompHeld = kb.IsKeyDown(Keys.E);
            bool stompPressed = stompHeld && !_stompWasPressed;
            _stompWasPressed = stompHeld;

            if (stompPressed && _stompCooldownTimer <= 0f && !_isStomping)
            {
                //Velocity.Y = StompForce;
                _isStomping = true;
                _stompCooldownTimer = StompCooldown;
                _isAttacking = true;
                _attackTimer = AttackDuration;
                ChangeAnimation(_attackAnimation);
                OnStomp?.Invoke();
            }
        }

        private void ApplyGravityAndMove(float dt,
                                          IReadOnlyList<Platform> platforms,
                                          IReadOnlyList<Enemy> enemies,
                                          IReadOnlyList<FlyingEnemy> flyingEnemies,
                                          IReadOnlyList<ChasingEnemy> chasingEnemies)
        {
            // Horizontal
            Position.X += Velocity.X * dt;
            ResolveHorizontalCollisions(platforms);

            // Vertical
            Velocity.Y += Gravity * dt;
            Position.Y += Velocity.Y * dt;
            ResolveVerticalCollisions(platforms, enemies, flyingEnemies, chasingEnemies);
        }

        private void SelectAnimation()
        {
            // Attack takes priority — don't interrupt it with movement or jump anims
            if (_isAttacking) return;

            if (!_isOnGround)
                ChangeAnimation(_jumpAnimation);
            else if (Math.Abs(Velocity.X) > 1f)
                ChangeAnimation(_runAnimation);
            else
                ChangeAnimation(_idleAnimation);
        }

        private void EnforceWorldBounds()
        {
            // Horizontal clamp
            float halfW = HitboxWidth / 2f;
            if (Position.X - halfW < 0f)
            {
                Position.X = halfW;
                Velocity.X = 0f;
            }
            else if (Position.X + halfW > _worldWidth)
            {
                Position.X = _worldWidth - halfW;
                Velocity.X = 0f;
            }

            // Fall death
            if (Position.Y > _worldHeight + 300f)
                Kill();
        }

        // ── Animation helper ─────────────────────────────────────────────────────

        private void ChangeAnimation(Animation next)
        {
            if (next == null || next == _currentAnimation) return;
            _currentAnimation = next;
            _currentAnimation.Reset();
        }

        // ── Collision resolution ─────────────────────────────────────────────────

        private bool IsGrounded(IReadOnlyList<Platform> platforms)
        {
            var feetProbe = new Rectangle(
                (int)(Position.X - HitboxWidth / 2),
                (int)(Position.Y + HitboxHeight / 2),
                HitboxWidth, 2);

            foreach (var platform in platforms)
            {
                if (feetProbe.Intersects(platform.Bounds))
                {
                    _jumpCount = 0;
                    return true;
                }
            }
            return false;
        }

        private void ResolveHorizontalCollisions(IReadOnlyList<Platform> platforms)
        {
            Rectangle bounds = GetBounds();
            foreach (var platform in platforms)
            {
                if (!bounds.Intersects(platform.Bounds)) continue;

                if (Velocity.X > 0f)
                    Position.X = platform.Bounds.Left - HitboxWidth / 2;
                else if (Velocity.X < 0f)
                    Position.X = platform.Bounds.Right + HitboxWidth / 2;

                Velocity.X = 0f;
                bounds = GetBounds();
            }
        }

        private void ResolveVerticalCollisions(IReadOnlyList<Platform> platforms,
                                               IReadOnlyList<Enemy> enemies,
                                               IReadOnlyList<FlyingEnemy> flyingEnemies,
                                               IReadOnlyList<ChasingEnemy> chasingEnemies)
        {
            Rectangle bounds = GetBounds();
            foreach (var platform in platforms)
            {
                if (!bounds.Intersects(platform.Bounds)) continue;

                if (Velocity.Y > 0f) // falling
                {
                    Position.Y = platform.Bounds.Top - HitboxHeight / 2;
                    Velocity.Y = 0f;
                    _isOnGround = true;
                    _jumpCount = 0;

                    if (_isStomping)
                    {
                        PushEnemiesAway(enemies, flyingEnemies, chasingEnemies);
                        //Velocity.Y          = StompBounceSpeed;
                        _isStomping = false;
                        _stompCooldownTimer = 0f;
                    }
                }
                else if (Velocity.Y < 0f) // rising — hit ceiling
                {
                    Position.Y = platform.Bounds.Bottom + HitboxHeight / 2;
                    Velocity.Y = 0f;
                }

                bounds = GetBounds();
            }
        }

        private void PushEnemiesAway(IReadOnlyList<Enemy> enemies,
                                     IReadOnlyList<FlyingEnemy> flyingEnemies,
                                     IReadOnlyList<ChasingEnemy> chasingEnemies)
        {
            const float PushForce = 1500f;
            const float Radius = 150f;

            Vector2 centre = new Vector2(Position.X, Position.Y - HitboxHeight / 2f);

            foreach (var e in enemies)
            {
                if (Vector2.Distance(centre, EnemyCentre(e.Position)) < Radius)
                    e.FlipDirection();
            }

            foreach (var fe in flyingEnemies)
            {
                Vector2 ec = EnemyCentre(fe.Position);
                if (Vector2.Distance(centre, ec) < Radius)
                {
                    float dir = ec.X < centre.X ? -1f : 1f;
                    fe.Velocity = new Vector2(dir * PushForce, -200f);
                    fe.MarkToReturn(2f);
                }
            }

            foreach (var ce in chasingEnemies)
            {
                if (Vector2.Distance(centre, EnemyCentre(ce.Position)) < Radius)
                    ce.Stun(1.5f);
            }
        }

        /// <summary>Returns the visual centre of an enemy given its top-left position.</summary>
        private static Vector2 EnemyCentre(Vector2 topLeft)
            => topLeft + new Vector2(16f, 16f);
    }
}