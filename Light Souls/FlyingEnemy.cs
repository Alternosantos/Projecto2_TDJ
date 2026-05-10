using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Light_Souls
{
    public class FlyingEnemy
    {
        public Vector2 Position;
        public Vector2 Velocity;
        private Texture2D _texture;
        private float _moveSpeed = 80f;
        private int _direction = 1;
        private float _leftBound, _rightBound;
        private float _originalY;
        private float _returnTimer = 0f;
        private bool _needsReturn = false;

        public FlyingEnemy(Texture2D texture, Vector2 startPosition, float leftBound, float rightBound)
        {
            _texture = texture;
            Position = startPosition;
            Velocity = Vector2.Zero;
            _leftBound = leftBound;
            _rightBound = rightBound;
            _originalY = startPosition.Y;
        }

        public void Update(GameTime gameTime, List<Platform> platforms)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Aplica empurrão (velocidade)
            Position.X += Velocity.X * deltaTime;
            Position.Y += Velocity.Y * deltaTime;

            // Regresso à altura original após empurrão
            if (_needsReturn)
            {
                _returnTimer -= deltaTime;
                if (_returnTimer <= 0)
                {
                    Position.Y = _originalY;
                    _needsReturn = false;
                    Velocity.Y = 0;
                }
                else
                {
                    Position.Y = MathHelper.Lerp(Position.Y, _originalY, 0.05f);
                }
            }

            // Movimento padrão (apenas se não estiver a ser empurrado com muita força)
            if (System.Math.Abs(Velocity.X) < _moveSpeed * 1.5f && !_needsReturn)
            {
                Position.X += _direction * _moveSpeed * deltaTime;
            }

            // Limites horizontais
            if (Position.X <= _leftBound)
            {
                Position.X = _leftBound;
                _direction = 1;
            }
            else if (Position.X + _texture.Width >= _rightBound)
            {
                Position.X = _rightBound - _texture.Width;
                _direction = -1;
            }

            // Amortecimento
            Velocity.X *= 0.95f;
            Velocity.Y *= 0.95f;
        }

        public void MarkToReturn(float delay = 2f)
        {
            _needsReturn = true;
            _returnTimer = delay;
        }

        public Rectangle GetBounds()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
        }

        public bool CollidesWith(Rectangle playerBounds)
        {
            return GetBounds().Intersects(playerBounds);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, Position, Color.Purple);
        }
    }
}