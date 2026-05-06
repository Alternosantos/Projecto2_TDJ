using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Light_Souls
{
    public class Level
    {
        private Tile?[,] _tiles;
        private int _width, _height;  // in tiles
        private Texture2D _grassTexture, _dirtTexture;

        public Vector2 PlayerStart { get; private set; }
        public List<Enemy> Enemies { get; private set; }
        public int WorldWidth => _width * Tile.Width;
        public int WorldHeight => _height * Tile.Height;

        public Level(Texture2D grassTex, Texture2D dirtTex, Texture2D enemyTex)
        {
            _grassTexture = grassTex;
            _dirtTexture = dirtTex;
            PlayerStart = Vector2.Zero;
            

            // Example map – replace with your own
            string[] map = new string[]
            {
                "................................",
                "........................P.......",
                ".........E.........===..........",
                "........####....................",
                "...................###..........",
                "########.............###........",
                "##############################.."
            };

            _height = map.Length;
            _width = map[0].Length;
            _tiles = new Tile?[_width, _height];

            Enemies = new List<Enemy>();

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    char c = map[y][x];
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
                        _tiles[x, y] = null; // no tile underneath (place enemies on top of existing ground)
                                             // But careful: you may want a solid tile below E. Better to place E above ground.
                    }
                    else if (c == '#')
                    {
                        _tiles[x, y] = new Tile(_dirtTexture, TileCollision.Impassable);
                    }
                    else if (c == '=')
                    {
                        _tiles[x, y] = new Tile(_grassTexture, TileCollision.Platform);
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