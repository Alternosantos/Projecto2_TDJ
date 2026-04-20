using Light_Soulls.Entities;
using Light_Soulls.Graphics;
using Light_Soulls.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Light_Soulls
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private TileMap _tileMap;
        private Camera _camera;
        private Texture2D _pixelTexture;
        private Player _player;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _pixelTexture = CreatePixelTexture();

            string levelPath = "\\Content\\Levels\\level1.txt";//Path.Combine(Content.RootDirectory, "level1.txt");
            LoadLevelFromFile(levelPath);


            _camera = new Camera();
            _camera.Viewport = GraphicsDevice.Viewport;
            _camera.Position = Vector2.Zero; // começa no canto superior esquerdo
            _player = new Player(_pixelTexture);
            _player = new Player(_pixelTexture);
            _player.Position = GetSafeSpawnPosition();

        }
        private Vector2 GetSafeSpawnPosition()
        {
            for (int x = 0; x < _tileMap.Width; x++)
            {
                for (int y = 0; y < _tileMap.Height; y++)
                {
                    if (_tileMap.GetTile(x, y) == 1) // apenas plataformas seguras
                    {
                        float spawnX = x * _tileMap.TileWidth;
                        float spawnY = y * _tileMap.TileHeight - _player.Height;
                        return new Vector2(spawnX, spawnY);
                    }
                }
            }
            return new Vector2(100, 100);
        }

        private Texture2D CreatePixelTexture()
        {
            Texture2D tex = new Texture2D(GraphicsDevice, 1, 1);
            tex.SetData(new[] { Color.White });
            return tex;
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) Exit();

            _player.Update(gameTime, _tileMap);

            // Verificar se o jogador tocou em algum tile ID 2 (chão mortal)
            if (IsPlayerTouchingGroundOfDeath())
            {
                _player.Position = GetSafeSpawnPosition();
                _player.Velocity = Vector2.Zero;
            }

            _camera.Update(_player.Position, _tileMap.Bounds);

            base.Update(gameTime);
        }
        private bool IsPlayerTouchingGroundOfDeath()
        {
            Rectangle playerRect = _player.BoundingBox;
            int leftTile = playerRect.Left / _tileMap.TileWidth;
            int rightTile = playerRect.Right / _tileMap.TileWidth;
            int topTile = playerRect.Top / _tileMap.TileHeight;
            int bottomTile = playerRect.Bottom / _tileMap.TileHeight;

            for (int y = topTile; y <= bottomTile; y++)
            {
                for (int x = leftTile; x <= rightTile; x++)
                {
                    if (_tileMap.GetTile(x, y) == 2) // chão mortal
                        return true;
                }
            }
            return false;
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(transformMatrix: _camera.Transform);
            _tileMap.DrawDebug(_spriteBatch, _pixelTexture, _camera);
            _player.Draw(_spriteBatch);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
        private void LoadLevelFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Level file not found: {filePath}");
            }

            string[] lines = File.ReadAllLines(filePath);
            int height = lines.Length;
            int width = lines[0].Length;

            // Criar tilemap com as dimensões do ficheiro
            _tileMap = new TileMap(width, height, 32, 32);

            // Percorrer cada caractere
            for (int y = 0; y < height; y++)
            {
                string line = lines[y];
                for (int x = 0; x < width; x++)
                {
                    char c = line[x];
                    switch (c)
                    {
                        case '#':
                            _tileMap.SetTile(x, y, 1);  // plataforma sólida
                            break;
                        case '=':
                            _tileMap.SetTile(x, y, 2);  // chão mortal
                            break;
                        case '&':
                            // Posição inicial do jogador
                            _player.Position = new Vector2(x * _tileMap.TileWidth, y * _tileMap.TileHeight - _player.Height);
                            _tileMap.SetTile(x, y, 0);  // tile vazio no lugar do spawn
                            break;
                        case '.':
                        case ' ':
                        default:
                            _tileMap.SetTile(x, y, 0);  // vazio
                            break;
                    }
                }
            }
        }

    }
}