using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Light_Souls
{
    public class Layer
    {
        private readonly Texture2D _texture;
        private Vector2 _position;
        private readonly float _depth;
        private readonly float _movescale;

        public Layer(Texture2D texture, float depth, float movescale)
        {
            _texture = texture;
            _depth = depth;
            _movescale = movescale;
            _position = Vector2.Zero;
        }
        public void Update(float movement)
        {
            float screenHeight = Globals.SpriteBatch.GraphicsDevice.Viewport.Height;
            float scale = screenHeight / _texture.Height;
            float adjustedWidth = _texture.Width * scale;

            // Movimentação
            _position.X += movement * _movescale * Globals.ElapsedSeconds;

            // Wrap Suave: 
            // Isto garante que a _position.X esteja sempre entre 0 e -adjustedWidth
            _position.X %= adjustedWidth;
        }

        public void Draw()
        {
            float screenHeight = Globals.SpriteBatch.GraphicsDevice.Viewport.Height;
            float scale = screenHeight / _texture.Height;
            float adjustedWidth = _texture.Width * scale;

            // Desenha a cópia central/atual
            Globals.SpriteBatch.Draw(_texture, _position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, _depth);

            // Desenha a cópia à DIREITA
            Globals.SpriteBatch.Draw(_texture, new Vector2(_position.X + adjustedWidth, _position.Y), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, _depth);

            // Desenha a cópia à ESQUERDA (Caso o jogador ande para o outro lado)
            Globals.SpriteBatch.Draw(_texture, new Vector2(_position.X - adjustedWidth, _position.Y), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, _depth);
        }

    }

    }
