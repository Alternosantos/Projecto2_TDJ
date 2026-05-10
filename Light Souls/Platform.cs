using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    public class Platform
    {
        public Rectangle Bounds;
        public bool IsSolid = true;   

        public Platform(Rectangle bounds)
        {
            Bounds = bounds;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D texture)
        {
            spriteBatch.Draw(texture, Bounds, Color.White);
        }
    }
}