using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Light_Soulls.Map;

namespace Light_Soulls.Entities
{
    public class Player
    {
        private Texture2D _pixelTexture;
        public Vector2 Position;
        public Vector2 Velocity;
        public bool IsOnGround;
        public int Width = 16;   
        public int Height = 31;

        private float _speed = 350f;
        private float _jumpForce = -500f;
        private float _gravity = 1800f;

        public Player(Texture2D pixelTexture)
        {
            _pixelTexture = pixelTexture;
            Position = Vector2.Zero;
            Velocity = Vector2.Zero;
        }

        public Rectangle BoundingBox => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        public void Update(GameTime gameTime, TileMap tileMap)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyState = Keyboard.GetState();

            // Movimento horizontal
            if (keyState.IsKeyDown(Keys.A) || keyState.IsKeyDown(Keys.Left))
                Velocity.X = -_speed;
            else if (keyState.IsKeyDown(Keys.D) || keyState.IsKeyDown(Keys.Right))
                Velocity.X = _speed;
            else
                Velocity.X = 0;

            // Salto
            if ((keyState.IsKeyDown(Keys.Space) || keyState.IsKeyDown(Keys.Up)) && IsOnGround)
            {
                Velocity.Y = _jumpForce;
                IsOnGround = false;
            }

            // Gravidade
            Velocity.Y += _gravity * delta;

            // Aplicar movimento
            Position += Velocity * delta;

            // Colisão com tiles
            HandleCollision(tileMap);
        }

        private void HandleCollision(TileMap tileMap)
        {
            Rectangle playerRect = BoundingBox;
            int leftTile = playerRect.Left / tileMap.TileWidth;
            int rightTile = playerRect.Right / tileMap.TileWidth;
            int topTile = playerRect.Top / tileMap.TileHeight;
            int bottomTile = playerRect.Bottom / tileMap.TileHeight;

            IsOnGround = false;

            for (int y = topTile; y <= bottomTile; y++)
            {
                for (int x = leftTile; x <= rightTile; x++)
                {
                    if (tileMap.GetTile(x, y) != 0) // tile sólido
                    {
                        Rectangle tileRect = new Rectangle(
                            x * tileMap.TileWidth,
                            y * tileMap.TileHeight,
                            tileMap.TileWidth,
                            tileMap.TileHeight);

                        if (playerRect.Intersects(tileRect))
                        {
                            Rectangle intersection = Rectangle.Intersect(playerRect, tileRect);

                            if (intersection.Width < intersection.Height)
                            {
                                // Colisão horizontal
                                if (playerRect.Center.X < tileRect.Center.X)
                                    Position.X -= intersection.Width;
                                else
                                    Position.X += intersection.Width;
                            }
                            else
                            {
                                // Colisão vertical
                                if (playerRect.Center.Y < tileRect.Center.Y)
                                {
                                    Position.Y -= intersection.Height;
                                    Velocity.Y = 0;
                                    IsOnGround = true;
                                }
                                else
                                {
                                    Position.Y += intersection.Height;
                                    Velocity.Y = 0;
                                }
                            }
                            playerRect = BoundingBox;
                        }
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Desenha o player como um quadrado azul
            spriteBatch.Draw(_pixelTexture, BoundingBox, Color.DodgerBlue);
        }
    }
}