using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;

namespace Light_Souls
{
    public class PlatformerGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Player _player;
        private Level _level;
        private Camera _camera;

        // Textures
        private Texture2D _platformTexture;
        private Texture2D _playerTex;
        private Texture2D _enemyTex;
        private Texture2D _coinTex;
        private Texture2D _backgroundTexture;

        private int _currentLevelIndex = 0;
        private string[] _levelFiles = new string[]
        {
            "Content/Levels/Level1.txt",
            "Content/Levels/Level2.txt",
            "Content/Levels/Level3.txt",
            "Content/Levels/Level4.txt"
        };

        public PlatformerGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 480;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        private void LoadLevel(int index)
        {
            _level = new Level(_platformTexture, _enemyTex, _coinTex, _levelFiles[index]);
            _player = new Player(_playerTex, _level.PlayerStart);
            _camera = new Camera(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight,
                _level.WorldWidth,
                _level.WorldHeight
            );
        }

        private void LoadNextLevel()
        {
            _currentLevelIndex++;
            if (_currentLevelIndex < _levelFiles.Length)
                LoadLevel(_currentLevelIndex);
            else
                Exit();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create textures
            _platformTexture = CreateSolidTexture(1, 1, Color.Gray);
            _playerTex = CreateSolidTexture(32, 32, Color.Green);
            _enemyTex = CreateSolidTexture(32, 32, Color.Red);
            _coinTex = CreateSolidTexture(24, 24, Color.Yellow);

            _backgroundTexture = Content.Load<Texture2D>("Background/Layer1");

            LoadLevel(0);
        }

        private Texture2D CreateSolidTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(GraphicsDevice, width, height);
            Color[] data = new Color[width * height];
            for (int i = 0; i < data.Length; i++) data[i] = color;
            texture.SetData(data);
            return texture;
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Update player
            _player.Update(gameTime, _level.Platforms, _level.Enemies, _level.FlyingEnemies, _level.ChasingEnemies);

            // Update all enemies
            foreach (var enemy in _level.Enemies)
                enemy.Update(gameTime, _level.Platforms);
            foreach (var flying in _level.FlyingEnemies)
                flying.Update(gameTime, _level.Platforms);
            foreach (var chasing in _level.ChasingEnemies)
                chasing.Update(gameTime, _level.Platforms, _player);

            // Check collisions with ALL enemies
            Rectangle playerBounds = _player.GetBounds();

            // Normal enemies
            foreach (var enemy in _level.Enemies)
            {
                if (enemy.CollidesWith(playerBounds))
                {
                    if (!_player.IsInvincible)
                    {
                        _player.Position = _level.PlayerStart;
                        _player.Velocity = Vector2.Zero;
                        _player.TakeHit();
                    }
                    break;  // only reset once per frame
                }
            }

            // Flying enemies
            foreach (var flying in _level.FlyingEnemies)
            {
                if (flying.CollidesWith(playerBounds))
                {
                    if (!_player.IsInvincible)
                    {
                        _player.Position = _level.PlayerStart;
                        _player.Velocity = Vector2.Zero;
                        _player.TakeHit();
                    }
                    break;
                }
            }

            // Chasing enemies
            foreach (var chasing in _level.ChasingEnemies)
            {
                if (chasing.CollidesWith(playerBounds))
                {
                    if (!_player.IsInvincible)
                    {
                        _player.Position = _level.PlayerStart;
                        _player.Velocity = Vector2.Zero;
                        _player.TakeHit();
                    }
                    break;
                }
            }

            // Collect coins
            _player.CollectCoins(_level.Coins);

            // Level completion
            if (_level.RemainingCoins == 0)
            {
                LoadNextLevel();
                return;
            }

            _camera.Follow(_player.Position);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // 1. Draw background with parallax
            float parallaxFactor = 0.5f;
            float bgWidth = _backgroundTexture.Width;
            float bgHeight = _backgroundTexture.Height;
            float screenWidth = GraphicsDevice.Viewport.Width;
            float screenHeight = GraphicsDevice.Viewport.Height;

            float scale = screenWidth / bgWidth;
            float scaledHeight = bgHeight * scale;
            float startX = -_camera.Position.X * parallaxFactor;
            float yOffset = (screenHeight - scaledHeight) / 2;

            _spriteBatch.Begin();
            int tilesNeeded = (int)Math.Ceiling(screenWidth / (bgWidth * scale)) + 2;
            for (int i = 0; i < tilesNeeded; i++)
            {
                Vector2 pos = new Vector2(startX + i * bgWidth * scale, yOffset);
                Rectangle destRect = new Rectangle((int)pos.X, (int)pos.Y, (int)(bgWidth * scale), (int)scaledHeight);
                _spriteBatch.Draw(_backgroundTexture, destRect, Color.White);
            }
            _spriteBatch.End();

            // 2. Draw game world with camera
            var cameraMatrix = Matrix.CreateTranslation(-_camera.Position.X, -_camera.Position.Y, 0);
            _spriteBatch.Begin(transformMatrix: cameraMatrix);
            try
            {
                _level?.Draw(_spriteBatch);

                // Draw all enemies
                foreach (var enemy in _level.Enemies)
                    enemy.Draw(_spriteBatch);
                foreach (var flying in _level.FlyingEnemies)
                    flying.Draw(_spriteBatch);
                foreach (var chasing in _level.ChasingEnemies)
                    chasing.Draw(_spriteBatch);

                foreach (var coin in _level.Coins)
                    coin.Draw(_spriteBatch);

                _player?.Draw(_spriteBatch);
            }
            finally
            {
                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}