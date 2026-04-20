using Light_Soulls.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Light_Soulls.Map
{
    public class TileMap
    {
        public int Width { get; private set; }      // em tiles
        public int Height { get; private set; }
        public int TileWidth { get; private set; }
        public int TileHeight { get; private set; }

        private int[,] tiles;   // 0 = vazio, >0 = índice do tile

        public TileMap(int width, int height, int tileWidth, int tileHeight)
        {
            Width = width;
            Height = height;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            tiles = new int[width, height];
        }

        public void SetTile(int x, int y, int tileId)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                tiles[x, y] = tileId;
        }

        public int GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return 0; // fora do mapa é vazio
            return tiles[x, y];
        }

        // Limites do mundo em pixels
        public Rectangle Bounds => new Rectangle(0, 0, Width * TileWidth, Height * TileHeight);

        public void DrawDebug(SpriteBatch spriteBatch, Texture2D pixelTexture, Camera camera)
        {
            int startX = (int)(camera.Position.X / TileWidth);
            int startY = (int)(camera.Position.Y / TileHeight);
            int endX = startX + (camera.Viewport.Width / TileWidth) + 2;
            int endY = startY + (camera.Viewport.Height / TileHeight) + 2;

            startX = Math.Max(0, startX);
            startY = Math.Max(0, startY);
            endX = Math.Min(Width, endX);
            endY = Math.Min(Height, endY);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    int tileId = tiles[x, y];
                    if (tileId == 0) continue;

                    Color color;
                    if (tileId == 1)
                        color = Color.SaddleBrown;  // plataforma normal
                    else if (tileId == 2)
                        color = Color.DarkRed;      // chão mortal
                    else
                        color = Color.Gray;

                    Rectangle dest = new Rectangle(x * TileWidth, y * TileHeight, TileWidth, TileHeight);
                    spriteBatch.Draw(pixelTexture, dest, color);
                }
            }
        }
    }
}