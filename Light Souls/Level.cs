using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Light_Souls
{
    public class Level
    {
        private Tile?[,] _tiles;
        private int _width, _height;  // in tiles
        private Texture2D _grassTexture, _dirtTexture;

        public Vector2 PlayerStart { get; private set; }
        public List<Enemy> Enemies { get; private set; }
        public List<Coin> Coins { get; private set; }
        public int TotalCoins => Coins.Count;
        public int RemainingCoins => Coins.Count(c => !c.IsCollected);

        public int WorldWidth => _width * Tile.Width;
        public int WorldHeight => _height * Tile.Height;

        public Level(Texture2D grassTex, Texture2D dirtTex, Texture2D enemyTex, Texture2D coinTex, string levelPath)
        {
            _grassTexture = grassTex;
            _dirtTexture = dirtTex;
            PlayerStart = Vector2.Zero;

            Coins = new List<Coin>();

            string[] map = System.IO.File.ReadAllLines(levelPath);

            _height = map.Length;
            _width = 0;
            for (int i = 0; i < _height; i++)
                if (map[i].Length > _width) _width = map[i].Length;

            _tiles = new Tile?[_width, _height];

            Enemies = new List<Enemy>();

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    char c = (x < map[y].Length) ? map[y][x] : '.';
                    if (c == 'P')
                    {
                        PlayerStart = new Vector2(x * Tile.Width, y * Tile.Height);
                        _tiles[x, y] = null;
                    }
                    else if (c == 'E')
                    {
                        // Create enemy at this tile position
                        Vector2 enemyPos = new Vector2(x * Tile.Width, y * Tile.Height);
                        Enemies.Add(new Enemy(enemyTex, enemyPos));
                        _tiles[x, y] = null; 
                    }
                    else if (c == '#')
                    {
                        _tiles[x, y] = new Tile(_dirtTexture, TileCollision.Impassable);
                    }
                    else if (c == '=')
                    {
                        _tiles[x, y] = new Tile(_grassTexture, TileCollision.Platform);
                    }
                    else if (c == 'C')
                    {
                        Vector2 coinPos = new Vector2(x * Tile.Width, y * Tile.Height);
                        Coins.Add(new Coin(coinTex, coinPos));
                        _tiles[x, y] = null; // coin tile is empty
                    }
                    else
                    {
                        _tiles[x, y] = null;
                    }
                }
            }
        }

        public Tile?[,] GetTiles() => _tiles;

        public void Draw(SpriteBatch spriteBatch)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var tile = _tiles[x, y];
                    if (tile.HasValue)
                    {
                        spriteBatch.Draw(tile.Value.Texture,
                            new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height),
                            Color.White);
                    }
                }
            }
        }
    }
}