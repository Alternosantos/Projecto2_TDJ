using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;

namespace Light_Souls
{
    public static class Globals
    {
        public static float ElapsedSeconds { get; set; }
        public static SpriteBatch SpriteBatch { get; set; }
        public static ContentManager Content { get; set; }
    }

    public class PlatformerGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Player _player;
        private Level _level;
        private Camera _camera;

        private GameManager _gameManager;
        private BGManager _bgManager;

        // FIX 1: Textures are now class-level fields so LoadLevel() can access them
        private Texture2D _grassTex;
        private Texture2D _dirtTex;
        private Texture2D _enemyTex;
        private Texture2D _coinTex;
        private Texture2D _playerTex;

        private int _currentLevelIndex = 0;
        private string[] _levelFiles = new string[]
        {
            "Content/Levels/Level1.txt",
            "Content/Levels/Level2.txt"
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
            // FIX 2: Now works correctly because textures are fields, not locals
            _level = new Level(_grassTex, _dirtTex, _enemyTex, _coinTex, _levelFiles[index]);
            _player = new Player(_playerTex, _level.PlayerStart);
            _camera = new Camera(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight,
                _level.WorldWidth,
                _level.WorldHeight
            );
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // FIX 3: Assign to fields instead of local variables
            _grassTex = CreateSolidTexture(32, 32, Color.Green);
            _dirtTex = CreateSolidTexture(32, 32, Color.Brown);
            _enemyTex = CreateSolidTexture(32, 32, Color.Red);
            _coinTex = CreateSolidTexture(24, 24, Color.Yellow);
            _playerTex = CreateSolidTexture(32, 32, Color.Green);

            // FIX 4: Removed the duplicate Level/Player/Camera creation that was here.
            // LoadLevel() handles all of that cleanly.
            LoadLevel(0);

            Globals.SpriteBatch = _spriteBatch;
            Globals.Content = Content;
            _gameManager = new GameManager();
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
            Globals.ElapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _gameManager?.Update();

            _player.Update(gameTime, _level.GetTiles());
            foreach (var enemy in _level.Enemies)
            {
                enemy.Update(gameTime, _level.GetTiles());
            }

            // Enemy-player collision
            Rectangle playerBounds = _player.GetBounds();
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
                    break; // only need to reset once per frame
                }
            }

            // Coin collection
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

            // Background (no camera)
            _spriteBatch.Begin();
            _gameManager?.Draw();
            _spriteBatch.End();

            // World with camera
            var cameraMatrix = Matrix.CreateTranslation(-_camera.Position.X, -_camera.Position.Y, 0);
            _spriteBatch.Begin(transformMatrix: cameraMatrix);
            try
            {
                _level?.Draw(_spriteBatch);

                if (_level?.Enemies != null)
                {
                    foreach (var enemy in _level.Enemies)
                        enemy.Draw(_spriteBatch);
                }

                // ✅ ADD THIS: draw coins
                if (_level?.Coins != null)
                {
                    foreach (var coin in _level.Coins)
                        coin.Draw(_spriteBatch);
                }

                _player?.Draw(_spriteBatch);
            }
            finally
            {
                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        private void LoadNextLevel()
        {
            _currentLevelIndex++;
            if (_currentLevelIndex < _levelFiles.Length)
                LoadLevel(_currentLevelIndex);
            else
                Exit();
        }
    }
}