using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

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
        private Texture2D[] _platformTextures;
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
            _level = new Level(_platformTextures, _enemyTex, _coinTex, _levelFiles[index]);
            if (_level == null) throw new Exception("Level failed to load");
            _player = new Player(_playerTex, _level.PlayerStart);
            if (_player == null) throw new Exception("Player failed to create");
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
            GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;

            // Texturas sólidas (dummy)
            _playerTex = CreateSolidTexture(32, 32, Color.White);
            _enemyTex = CreateSolidTexture(32, 32, Color.Red);
            _coinTex = CreateSolidTexture(24, 24, Color.Yellow);

            
            _platformTextures = new Texture2D[7];
            for (int i = 0; i < 7; i++)
                _platformTextures[i] = Content.Load<Texture2D>($"Platforms/grey_dirt{i + 1}");

            
            _backgroundTexture = Content.Load<Texture2D>("Background/Layer1");

            
            LoadLevel(0);

            var idleFrames = LoadFrameList("Player/Idle");
            var runFrames = LoadFrameList("Player/Run");
            var jumpFrames = LoadFrameList("Player/Jump");
            var deadFrames = LoadFrameList("Player/Dead");

            // Verificar se carregou alguma coisa
            if (idleFrames.Count == 0) throw new Exception("Idle frames not found");
            if (runFrames.Count == 0) throw new Exception("Run frames not found");
            if (jumpFrames.Count == 0) throw new Exception("Jump frames not found");
            if (deadFrames.Count == 0) throw new Exception("Dead frames not found");

            // Criar animações com os frames reais
            var idleAnim = new Animation(idleFrames, 1f / 7f, true);
            var runAnim = new Animation(runFrames, 1f / 11f, true);
            var jumpAnim = new Animation(jumpFrames, 1f / 10f, false);
            var deadAnim = new Animation(deadFrames, 1f / 10f, false);

            _player.LoadAnimations(idleAnim, runAnim, jumpAnim, deadAnim);
            System.Diagnostics.Debug.WriteLine($"Idle frames: {idleFrames.Count}");
            System.Diagnostics.Debug.WriteLine($"Run frames: {runFrames.Count}");
            System.Diagnostics.Debug.WriteLine($"Jump frames: {jumpFrames.Count}");
            System.Diagnostics.Debug.WriteLine($"Dead frames: {deadFrames.Count}");

            try
            {
                _player.LoadAnimations(idleAnim, runAnim, jumpAnim, deadAnim);
            }
            catch (Exception ex)
            {
                
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar animações: {ex.Message}");
            }
        }

        private List<Texture2D> LoadFrameList(string folderPath)
        {
            var list = new List<Texture2D>();
            int i = 0;
            while (true)
            {
                try
                {
                    list.Add(Content.Load<Texture2D>($"{folderPath}/{i}"));
                    i++;
                }
                catch
                {
                    break; // sair quando faltar o ficheiro
                }
            }
            return list;
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
                        _player.Kill();
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
                        _player.Kill(); ;
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

            
            float parallaxFactor = 0.5f;
            float screenWidth = GraphicsDevice.Viewport.Width;
            float screenHeight = GraphicsDevice.Viewport.Height;
            float texWidth = _backgroundTexture.Width;
            float texHeight = _backgroundTexture.Height;

            
            float scale = screenWidth / texWidth;
            float scaledWidth = texWidth * scale;
            float scaledHeight = texHeight * scale;
            float yOffset = (screenHeight - scaledHeight) / 2;

            
            float camOffset = _camera.Position.X * parallaxFactor;

            
            int startTile = (int)Math.Floor(camOffset / scaledWidth);
            float startX = startTile * scaledWidth - camOffset;

            _spriteBatch.Begin();
            
            for (float x = startX; x < startX + screenWidth + scaledWidth; x += scaledWidth)
            {
                Rectangle destRect = new Rectangle((int)x, (int)yOffset, (int)scaledWidth, (int)scaledHeight);
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