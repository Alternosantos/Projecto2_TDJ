using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    public class Platform
    {
        public Rectangle Bounds;
        public Texture2D Texture;  // cada plataforma tem a sua própria textura

        public Platform(Rectangle bounds, Texture2D texture)
        {
            Bounds = bounds;
            Texture = texture;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            int texW = Texture.Width;
            int texH = Texture.Height;
            int startX = Bounds.X;
            int y = Bounds.Y + (Bounds.Height - texH) / 2; // centraliza verticalmente

            for (int x = startX; x < startX + Bounds.Width; x += texW)
            {
                int width = System.Math.Min(texW, startX + Bounds.Width - x);
                Rectangle destRect = new Rectangle(x, y, width, texH);
                spriteBatch.Draw(Texture, destRect, Color.White);
            }
        }
    }
}