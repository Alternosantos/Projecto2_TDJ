using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Light_Souls
{
    public class Level
    {
        private Tile[,] _tiles;
        private int _width, _height;
        private Texture2D _grassTexture, _dirtTexture;

        // Define what each character in the level map means
        private const char SOLID_BLOCK = '#';
        private const char EMPTY = '.';

        public Level(Texture2D grassTex, Texture2D dirtTex)
        {
            _grassTexture = grassTex;
            _dirtTexture = dirtTex;

            // Hardcoded simple level (you can load from a .txt file later)
            string[] map = new string[]
            {
                "........................",
                "........................",
                "........................",
                "........................",
                ".......####.............",
                "...................###..",
                "########.............###",
                "########################"
            };

            _height = map.Length;
            _width = map[0].Length;
            _tiles = new Tile[_width, _height];

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    char c = map[y][x];
                    if (c == SOLID_BLOCK)
                        _tiles[x, y] = new Tile(_dirtTexture, true);
                    else
                        _tiles[x, y] = null; // empty air
                }
            }
        }

        public void Update(GameTime gameTime) { }

        public void Draw(SpriteBatch spriteBatch)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (_tiles[x, y] != null)
                        _tiles[x, y].Draw(spriteBatch, x, y);
                }
            }
        }

        public Tile[,] GetTiles() => _tiles;
    }
}