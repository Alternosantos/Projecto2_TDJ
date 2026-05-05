using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    public class Tile
    {
        public const int TileSize = 32;
        public Texture2D Texture;
        public bool IsSolid;

        public Tile(Texture2D texture, bool isSolid)
        {
            Texture = texture;
            IsSolid = isSolid;
        }

        public void Draw(SpriteBatch spriteBatch, int x, int y)
        {
            spriteBatch.Draw(Texture, new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize), Color.White);
        }
    }
}