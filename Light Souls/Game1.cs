using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Light_Souls
{
    /// <summary>
    /// Main game class. Owns the game loop, level loading, rendering pipeline,
    /// and all overlay state (YOU DIED, level-complete, fullscreen button).
    /// </summary>
    public sealed class PlatformerGame : Game
    {
        // ── Virtual resolution ───────────────────────────────────────────────────
        // All gameplay logic and rendering targets this fixed resolution.
        // The result is letterbox-scaled to the real window in a second pass.

        private const int VirtualWidth  = 800;
        private const int VirtualHeight = 480;

        // ── Overlay timing ───────────────────────────────────────────────────────

        private const float YouDiedDuration       = 3.0f;
        private const float YouDiedFadeDuration   = 0.4f;
        private const float LevelCompleteDuration = 1.5f;

        // ── Level list ───────────────────────────────────────────────────────────

        private static readonly string[] LevelFiles =
        {
            "Content/Levels/Level1.txt",
            "Content/Levels/Level2.txt",
            "Content/Levels/Level3.txt",
            "Content/Levels/Level4.txt",
        };

        // ── Core infrastructure ───────────────────────────────────────────────────

        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch    _spriteBatch;
        private RenderTarget2D _renderTarget;   // offscreen buffer at VirtualWidth × VirtualHeight
        private Texture2D      _pixel;          // 1×1 white texture for filled rectangles

        // ── World objects ─────────────────────────────────────────────────────────

        private Player _player;
        private Level  _level;
        private Camera _camera;

        // ── Asset references ──────────────────────────────────────────────────────

        private Texture2D   _backgroundTexture;
        private Texture2D[] _platformTextures;
        private Texture2D   _playerTexture;
        private Texture2D   _enemyTexture;
        private Texture2D   _coinTexture;
        private SpriteFont  _font;             // null-safe — UI degrades gracefully without it

        // ── Reusable animations (re-attached on every level load) ─────────────────

        private Animation _idleAnim;
        private Animation _runAnim;
        private Animation _jumpAnim;
        private Animation _deadAnim;

        // ── Level state ───────────────────────────────────────────────────────────

        private int  _currentLevelIndex;
        private bool _levelTransitionPending;
        private bool _showLevelComplete;
        private float _levelCompleteTimer;

        // ── YOU DIED overlay ──────────────────────────────────────────────────────

        private bool  _showYouDied;
        private float _youDiedTimer;
        private float _youDiedAlpha;

        // ── Fullscreen ────────────────────────────────────────────────────────────

        private bool _isFullscreen;

        // ── Fullscreen button bounds (virtual-space) ──────────────────────────────

        private static readonly Rectangle FullscreenButtonRect
            = new Rectangle(VirtualWidth - 44, 8, 36, 24);

        // ── Input (previous-frame snapshots for edge detection) ───────────────────

        private KeyboardState _prevKeyboard;
        private MouseState    _prevMouse;

        // ── Constructor ───────────────────────────────────────────────────────────

        public PlatformerGame()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth  = VirtualWidth,
                PreferredBackBufferHeight = VirtualHeight,
            };

            Content.RootDirectory = "Content";
            IsMouseVisible        = true;
            _graphics.ApplyChanges();
        }

        // ── MonoGame overrides ────────────────────────────────────────────────────

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch  = new SpriteBatch(GraphicsDevice);
            _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);

            _pixel = CreateSolidTexture(1, 1, Color.White);

            LoadTextures();

            // Font is optional — if the asset is missing the game still runs
            try   { _font = Content.Load<SpriteFont>("Font"); }
            catch { _font = null; }

            LoadLevel(0);
            LoadPlayerAnimations();
            _player.LoadAnimations(_idleAnim, _runAnim, _jumpAnim, _deadAnim);
        }

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var   kb = Keyboard.GetState();
            var   ms = Mouse.GetState();

            HandleSystemInput(kb, ms);

            _prevKeyboard = kb;
            _prevMouse    = ms;

            UpdateYouDiedOverlay(dt);

            // Pause gameplay during the level-complete flash
            if (_showLevelComplete)
            {
                UpdateLevelCompleteOverlay(dt);
                base.Update(gameTime);
                return;
            }

            UpdateGameplay(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Pass 1 — render the full game into the virtual render target
            GraphicsDevice.SetRenderTarget(_renderTarget);
            GraphicsDevice.Clear(Color.CornflowerBlue);

            DrawBackground();
            DrawWorld();
            DrawHud();

            if (_showYouDied && _youDiedAlpha > 0.01f)
                DrawYouDiedOverlay();

            if (_showLevelComplete)
                DrawLevelCompleteOverlay();

            // Pass 2 — scale the virtual target to fill the real window
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);   // letterbox colour

            _spriteBatch.Begin(transformMatrix: GetScaleMatrix(),
                               samplerState: SamplerState.LinearClamp);
            _spriteBatch.Draw(_renderTarget,
                new Rectangle(0, 0, VirtualWidth, VirtualHeight), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        // ── Level management ──────────────────────────────────────────────────────

        private void LoadLevel(int index)
        {
            _currentLevelIndex = index;
            _level  = new Level(_platformTextures, _enemyTexture, _coinTexture, LevelFiles[index]);
            _player = new Player(_playerTexture, _level.PlayerStart);
            _player.SetWorldBounds(_level.WorldWidth, _level.WorldHeight);
            _camera = new Camera(VirtualWidth, VirtualHeight,
                                  _level.WorldWidth, _level.WorldHeight);
        }

        private void AdvanceLevel()
        {
            int next = _currentLevelIndex + 1;
            if (next < LevelFiles.Length)
                LoadLevel(next);
            else
                LoadLevel(0);   // wrap back to first level when all are completed

            ReattachAnimations();
        }

        // ── Animation helpers ─────────────────────────────────────────────────────

        private void LoadPlayerAnimations()
        {
            _idleAnim = BuildAnimation("Player/Idle", 1f / 7f,  looping: true);
            _runAnim  = BuildAnimation("Player/Run",  1f / 11f, looping: true);
            _jumpAnim = BuildAnimation("Player/Jump", 1f / 10f, looping: false);
            _deadAnim = BuildAnimation("Player/Dead", 1f / 10f, looping: false);
        }

        private void ReattachAnimations()
        {
            _idleAnim.Reset(); _runAnim.Reset();
            _jumpAnim.Reset(); _deadAnim.Reset();
            _player.LoadAnimations(_idleAnim, _runAnim, _jumpAnim, _deadAnim);
        }

        private Animation BuildAnimation(string folder, float frameTime, bool looping)
        {
            var frames = new List<Texture2D>();
            for (int i = 0; ; i++)
            {
                try   { frames.Add(Content.Load<Texture2D>($"{folder}/{i}")); }
                catch { break; }
            }

            if (frames.Count == 0)
                throw new Exception($"No animation frames found in '{folder}'.");

            return new Animation(frames, frameTime, looping);
        }

        // ── Texture helpers ───────────────────────────────────────────────────────

        private void LoadTextures()
        {
            // Solid-colour placeholders (overridden by the sprite animations at runtime)
            _playerTexture = CreateSolidTexture(32, 32, Color.White);
            _enemyTexture  = CreateSolidTexture(32, 32, Color.Red);
            _coinTexture   = CreateSolidTexture(24, 24, Color.Yellow);

            _platformTextures = new Texture2D[7];
            for (int i = 0; i < _platformTextures.Length; i++)
                _platformTextures[i] = Content.Load<Texture2D>($"Platforms/grey_dirt{i + 1}");

            _backgroundTexture = Content.Load<Texture2D>("Background/Layer1");
        }

        private Texture2D CreateSolidTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(GraphicsDevice, width, height);
            var data    = new Color[width * height];
            for (int i = 0; i < data.Length; i++) data[i] = color;
            texture.SetData(data);
            return texture;
        }

        // ── Fullscreen ────────────────────────────────────────────────────────────

        private void ToggleFullscreen()
        {
            _isFullscreen = !_isFullscreen;
            _graphics.IsFullScreen = _isFullscreen;

            if (_isFullscreen)
            {
                var dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                _graphics.PreferredBackBufferWidth  = dm.Width;
                _graphics.PreferredBackBufferHeight = dm.Height;
            }
            else
            {
                _graphics.PreferredBackBufferWidth  = VirtualWidth;
                _graphics.PreferredBackBufferHeight = VirtualHeight;
            }

            _graphics.ApplyChanges();
            // The camera always operates in virtual coordinates — no update required.
        }

        /// <summary>
        /// Returns a matrix that scales and centres the virtual viewport to fit
        /// the real window while preserving the aspect ratio.
        /// </summary>
        private Matrix GetScaleMatrix()
        {
            float scaleX  = (float)GraphicsDevice.Viewport.Width  / VirtualWidth;
            float scaleY  = (float)GraphicsDevice.Viewport.Height / VirtualHeight;
            float scale   = Math.Min(scaleX, scaleY);
            float offsetX = (GraphicsDevice.Viewport.Width  - VirtualWidth  * scale) / 2f;
            float offsetY = (GraphicsDevice.Viewport.Height - VirtualHeight * scale) / 2f;

            return Matrix.CreateScale(scale, scale, 1f)
                 * Matrix.CreateTranslation(offsetX, offsetY, 0f);
        }

        /// <summary>
        /// Converts a real-screen mouse position to virtual-space coordinates.
        /// </summary>
        private Vector2 ToVirtual(Point screenPoint)
        {
            float scaleX  = (float)GraphicsDevice.Viewport.Width  / VirtualWidth;
            float scaleY  = (float)GraphicsDevice.Viewport.Height / VirtualHeight;
            float scale   = Math.Min(scaleX, scaleY);
            float offsetX = (GraphicsDevice.Viewport.Width  - VirtualWidth  * scale) / 2f;
            float offsetY = (GraphicsDevice.Viewport.Height - VirtualHeight * scale) / 2f;

            return new Vector2(
                (screenPoint.X - offsetX) / scale,
                (screenPoint.Y - offsetY) / scale);
        }

        // ── Update helpers ────────────────────────────────────────────────────────

        private void HandleSystemInput(KeyboardState kb, MouseState ms)
        {
            // Quit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || kb.IsKeyDown(Keys.Escape))
                Exit();

            // Fullscreen — keyboard shortcut
            if (kb.IsKeyDown(Keys.F) && !_prevKeyboard.IsKeyDown(Keys.F))
                ToggleFullscreen();

            // Fullscreen — on-screen button (detect click release)
            if (ms.LeftButton  == ButtonState.Released &&
                _prevMouse.LeftButton == ButtonState.Pressed)
            {
                Vector2 vm = ToVirtual(ms.Position);
                if (FullscreenButtonRect.Contains((int)vm.X, (int)vm.Y))
                    ToggleFullscreen();
            }
        }

        private void UpdateYouDiedOverlay(float dt)
        {
            if (!_showYouDied) return;

            _youDiedTimer -= dt;

            // Fade in → hold → fade out
            float holdEnd   = YouDiedDuration - YouDiedFadeDuration;
            if (_youDiedTimer > holdEnd)
                _youDiedAlpha = 1f - (_youDiedTimer - holdEnd) / YouDiedFadeDuration;
            else if (_youDiedTimer < YouDiedFadeDuration)
                _youDiedAlpha = _youDiedTimer / YouDiedFadeDuration;
            else
                _youDiedAlpha = 1f;

            _youDiedAlpha = MathHelper.Clamp(_youDiedAlpha, 0f, 1f);

            if (_youDiedTimer <= 0f)
                _showYouDied = false;
        }

        private void UpdateLevelCompleteOverlay(float dt)
        {
            _levelCompleteTimer -= dt;
            if (_levelCompleteTimer > 0f) return;

            _showLevelComplete = false;

            if (_levelTransitionPending)
            {
                _levelTransitionPending = false;
                AdvanceLevel();
            }
        }

        private void UpdateGameplay(GameTime gameTime)
        {
            bool wasDeadBefore = _player.IsDead;

            // Update entities
            _player.Update(gameTime, _level.Platforms,
                           _level.Enemies, _level.FlyingEnemies, _level.ChasingEnemies);

            foreach (var e  in _level.Enemies)        e.Update(gameTime, _level.Platforms);
            foreach (var fe in _level.FlyingEnemies)  fe.Update(gameTime, _level.Platforms);
            foreach (var ce in _level.ChasingEnemies) ce.Update(gameTime, _level.Platforms, _player);

            // Check enemy collisions (lethal enemies bypass iFrames; see CheckEnemyCollisions)
            if (!_player.IsDead)
                CheckEnemyCollisions();

            // Collect coins
            if (!_player.IsDead)
                _player.CollectCoins(_level.Coins);

            // Trigger YOU DIED if player just died this frame
            if (!wasDeadBefore && _player.IsDead && !_showYouDied)
                TriggerYouDied();

            // Trigger level complete when all coins are collected
            bool hasCoins = _level.Coins.Count > 0;
            if (hasCoins && _level.RemainingCoins == 0 && !_showLevelComplete)
                TriggerLevelComplete();

            _camera.Follow(_player.Position);
        }

        private void CheckEnemyCollisions()
        {
            if (_player.IsDead) return;

            Rectangle pb = _player.GetBounds();

            // Normal patrol enemies: only hit the player if they are not already
            // invincible (prevents kill-spam when the player walks into a patrol).
            if (!_player.IsInvincible)
            {
                foreach (var e in _level.Enemies)
                    if (e.CollidesWith(pb)) { _player.TakeHit(); break; }
            }

            // Lethal enemies bypass invincibility frames — they always kill.
            foreach (var fe in _level.FlyingEnemies)
                if (fe.CollidesWith(pb)) { _player.Kill(); return; }

            foreach (var ce in _level.ChasingEnemies)
                if (ce.CollidesWith(pb)) { _player.Kill(); return; }
        }

        private void TriggerYouDied()
        {
            _showYouDied  = true;
            _youDiedTimer = YouDiedDuration;
            _youDiedAlpha = 0f;
        }

        private void TriggerLevelComplete()
        {
            _showLevelComplete      = true;
            _levelCompleteTimer     = LevelCompleteDuration;
            _levelTransitionPending = true;
        }

        // ── Draw helpers ──────────────────────────────────────────────────────────

        private void DrawBackground()
        {
            float texW      = _backgroundTexture.Width;
            float scaledW   = VirtualWidth;
            float scaledH   = _backgroundTexture.Height * (scaledW / texW);
            float yOffset   = (VirtualHeight - scaledH) / 2f;
            float parallaxX = _camera.Position.X * 0.5f;
            int   startTile = (int)Math.Floor(parallaxX / scaledW);
            float startX    = startTile * scaledW - parallaxX;

            _spriteBatch.Begin(samplerState: SamplerState.LinearWrap);
            for (float x = startX; x < startX + VirtualWidth + scaledW; x += scaledW)
            {
                _spriteBatch.Draw(_backgroundTexture,
                    new Rectangle((int)x, (int)yOffset, (int)scaledW, (int)scaledH),
                    Color.White);
            }
            _spriteBatch.End();
        }

        private void DrawWorld()
        {
            _spriteBatch.Begin(transformMatrix: _camera.GetTransformMatrix(),
                               samplerState: SamplerState.PointClamp);

            _level.Draw(_spriteBatch);

            foreach (var e  in _level.Enemies)        e.Draw(_spriteBatch);
            foreach (var fe in _level.FlyingEnemies)  fe.Draw(_spriteBatch);
            foreach (var ce in _level.ChasingEnemies) ce.Draw(_spriteBatch);
            foreach (var c  in _level.Coins)           c.Draw(_spriteBatch);

            _player.Draw(_spriteBatch);

            _spriteBatch.End();
        }

        private void DrawHud()
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            if (_font != null)
            {
                int collected = _level.Coins.Count - _level.RemainingCoins;
                _spriteBatch.DrawString(_font,
                    $"Moedas: {collected}/{_level.Coins.Count}",
                    new Vector2(10, 10), Color.White);

                _spriteBatch.DrawString(_font,
                    $"Nível {_currentLevelIndex + 1}",
                    new Vector2(10, 28), Color.LightGray);
            }

            DrawFullscreenButton();

            _spriteBatch.End();
        }

        private void DrawYouDiedOverlay()
        {
            byte alpha = (byte)(255 * _youDiedAlpha);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Dark vignette
            FillRect(new Rectangle(0, 0, VirtualWidth, VirtualHeight),
                     new Color((byte)0, (byte)0, (byte)0, (byte)(160 * _youDiedAlpha)));

            if (_font != null)
            {
                const string Text  = "YOU DIED";
                Vector2 size       = _font.MeasureString(Text);
                float   scale      = Math.Min(VirtualWidth / size.X * 0.55f, 5f);
                Vector2 origin     = size / 2f;
                Vector2 centre     = new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);

                // Drop shadow
                _spriteBatch.DrawString(_font, Text,
                    centre + new Vector2(3f * scale, 3f * scale),
                    new Color((byte)0, (byte)0, (byte)0, alpha),
                    0f, origin, scale, SpriteEffects.None, 0f);

                // Blood-red main text
                _spriteBatch.DrawString(_font, Text, centre,
                    new Color((byte)180, (byte)0, (byte)0, alpha),
                    0f, origin, scale, SpriteEffects.None, 0f);
            }
            else
            {
                int barH = VirtualHeight / 6;
                FillRect(new Rectangle(0, VirtualHeight / 2 - barH / 2, VirtualWidth, barH),
                         new Color((byte)120, (byte)0, (byte)0, alpha));
            }

            _spriteBatch.End();
        }

        private void DrawLevelCompleteOverlay()
        {
            float t     = MathHelper.Clamp(_levelCompleteTimer / LevelCompleteDuration, 0f, 1f);
            byte  alpha = (byte)(220 * t);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            FillRect(new Rectangle(0, 0, VirtualWidth, VirtualHeight),
                     new Color((byte)0, (byte)0, (byte)0, (byte)(100 * t)));

            if (_font != null)
            {
                const string Text  = "NIVEL COMPLETO!";
                Vector2 size       = _font.MeasureString(Text);
                float   scale      = Math.Min(VirtualWidth / size.X * 0.5f, 3.5f);
                Vector2 origin     = size / 2f;
                Vector2 centre     = new Vector2(VirtualWidth / 2f, VirtualHeight / 2f);

                _spriteBatch.DrawString(_font, Text,
                    centre + new Vector2(2f * scale, 2f * scale),
                    new Color((byte)0, (byte)0, (byte)0, alpha),
                    0f, origin, scale, SpriteEffects.None, 0f);

                _spriteBatch.DrawString(_font, Text, centre,
                    new Color((byte)255, (byte)215, (byte)0, alpha),
                    0f, origin, scale, SpriteEffects.None, 0f);
            }
            else
            {
                int barH = VirtualHeight / 6;
                FillRect(new Rectangle(0, VirtualHeight / 2 - barH / 2, VirtualWidth, barH),
                         new Color((byte)200, (byte)180, (byte)0, alpha));
            }

            _spriteBatch.End();
        }

        private void DrawFullscreenButton()
        {
            var r = FullscreenButtonRect;

            // Shadow border + button face
            FillRect(new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4),
                     new Color((byte)0, (byte)0, (byte)0, (byte)160));
            FillRect(r, new Color((byte)60, (byte)60, (byte)90, (byte)210));

            // Icon: arrows indicating expand or shrink
            int   cx = r.X + r.Width  / 2;
            int   cy = r.Y + r.Height / 2;
            const int S = 5;

            if (_isFullscreen)
            {
                // Inward arrows (shrink to window)
                FillRect(new Rectangle(cx - S,     cy - S,     S, 2), Color.White);
                FillRect(new Rectangle(cx - S,     cy - S,     2, S), Color.White);
                FillRect(new Rectangle(cx + 1,     cy + 1,     S, 2), Color.White);
                FillRect(new Rectangle(cx + S - 1, cy + 1,     2, S), Color.White);
            }
            else
            {
                // Outward arrows (go fullscreen)
                FillRect(new Rectangle(r.X + 4,         r.Y + 4,          S, 2), Color.White);
                FillRect(new Rectangle(r.X + 4,         r.Y + 4,          2, S), Color.White);
                FillRect(new Rectangle(r.Right - 4 - S, r.Bottom - 4 - S, S, 2), Color.White);
                FillRect(new Rectangle(r.Right - 5,     r.Bottom - 4 - S, 2, S), Color.White);
            }

            // Tooltip on hover
            if (_font != null)
            {
                Vector2 vm = ToVirtual(Mouse.GetState().Position);
                if (r.Contains((int)vm.X, (int)vm.Y))
                {
                    string tip = _isFullscreen ? "[F] Janela" : "[F] Fullscreen";
                    _spriteBatch.DrawString(_font, tip,
                        new Vector2(r.X - 82, r.Y + 5), Color.White,
                        0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
                }
            }
        }

        // ── Utility ───────────────────────────────────────────────────────────────

        /// <summary>Draws a solid-colour filled rectangle using the 1×1 pixel texture.</summary>
        private void FillRect(Rectangle rect, Color color)
            => _spriteBatch.Draw(_pixel, rect, color);
    }
}