using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.Reflection.Emit;
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

        // We'll add our player, level, etc. here later
        private Player _player;
        private Level _level;
        private Camera _camera;

        private GameManager _gameManager;
        private BGManager _bgManager;

        public PlatformerGame()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set window size (optional)
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 480;
            _graphics.ApplyChanges();
        }


        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create tile textures first
            Texture2D grassTex = CreateSolidTexture(32, 32, Color.Green);
            Texture2D dirtTex = CreateSolidTexture(32, 32, Color.Brown);
            //Creates enemys
            Texture2D enemyTex = CreateSolidTexture(32, 32, Color.Red);

            // Create level FIRST (so it exists for player start)
            _level = new Level(grassTex, dirtTex, enemyTex);

            // Now create player using level's start position
            Texture2D playerTex = CreateSolidTexture(32, 32, Color.Green);
            _player = new Player(playerTex, _level.PlayerStart);

            

            // Create camera using level dimensions
            _camera = new Camera(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight,
                _level.WorldWidth,
                _level.WorldHeight
            );

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

            // Update game objects
            _player.Update(gameTime, _level.GetTiles());
            //_level?.Update(gameTime);
            // Make camera follow player
            _camera.Follow(_player.Position);

            foreach (var enemy in _level.Enemies)
            {
                enemy.Update(gameTime, _level.GetTiles());
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // --- BLOCO 1: FUNDO ---
            _spriteBatch.Begin();
            // Em vez de _bgManager, usa o _gameManager que contém o fundo e o input!
            _gameManager?.Draw();
            _spriteBatch.End();

            // --- BLOCO 2: MUNDO (Com Câmera) ---
            // Aqui calculamos a matriz apenas UMA vez
            var cameraMatrix = Matrix.CreateTranslation(-_camera.Position.X, -_camera.Position.Y, 0);

            // Abrimos o Begin com a matriz
            _spriteBatch.Begin(transformMatrix: cameraMatrix);

            try
            {
                _level?.Draw(_spriteBatch);

                if (_level != null && _level.Enemies != null)
                {
                    foreach (var enemy in _level.Enemies)
                    {
                        enemy.Draw(_spriteBatch);
                    }
                }

                _player?.Draw(_spriteBatch);
            }
            finally
            {
                // Fecha o bloco da câmera
                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }
    }
}