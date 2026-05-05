using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Light_Souls
{
    public class Player
    {
        public Vector2 Position;
        public Vector2 Velocity;
        private Texture2D _texture;
        private bool _isOnGround;

        // Movement settings
        private float _moveSpeed = 300f;
        private float _jumpPower = -500f;
        private float _gravity = 1600f;

        public Player(Texture2D texture, Vector2 startPosition)
        {
            _texture = texture;
            Position = startPosition;
            Velocity = Vector2.Zero;
        }

        public void Update(GameTime gameTime, Tile[,] tiles)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Input
            var keyboard = Keyboard.GetState();
            float moveX = 0;
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
                moveX = -1;
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
                moveX = 1;

            // Horizontal movement
            Velocity.X = moveX * _moveSpeed;

            // Jumping
            _isOnGround = IsGrounded(tiles);
            if ((keyboard.IsKeyDown(Keys.Space) || keyboard.IsKeyDown(Keys.Up)) && _isOnGround)
            {
                Velocity.Y = _jumpPower;
            }

            // Apply gravity
            Velocity.Y += _gravity * deltaTime;

            // Move and collide
            Position += Velocity * deltaTime;
            HandleCollisions(tiles);
        }

        private bool IsGrounded(Tile[,] tiles)
        {
            // Simple check: if the pixel just below the player is a solid tile
            int playerFootY = (int)(Position.Y + _texture.Height);
            int tileX = (int)(Position.X + _texture.Width / 2) / Tile.TileSize;
            int tileY = playerFootY / Tile.TileSize;

            if (tileY >= 0 && tileY < tiles.GetLength(1) && tileX >= 0 && tileX < tiles.GetLength(0))
            {
                return tiles[tileX, tileY]?.IsSolid == true && Velocity.Y >= 0;
            }
            return false;
        }

        private void HandleCollisions(Tile[,] tiles)
        {
            // Simple AABB collision resolution (left/right/top/bottom)
            Rectangle playerRect = new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);

            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    var tile = tiles[x, y];
                    if (tile != null && tile.IsSolid)
                    {
                        Rectangle tileRect = new Rectangle(x * Tile.TileSize, y * Tile.TileSize, Tile.TileSize, Tile.TileSize);
                        if (playerRect.Intersects(tileRect))
                        {
                            // Resolve collision by pushing player out
                            Rectangle intersection = Rectangle.Intersect(playerRect, tileRect);
                            if (intersection.Width < intersection.Height)
                            {
                                // Left/right
                                if (playerRect.Center.X < tileRect.Center.X)
                                    Position.X -= intersection.Width;
                                else
                                    Position.X += intersection.Width;
                            }
                            else
                            {
                                // Top/bottom
                                if (playerRect.Center.Y < tileRect.Center.Y)
                                {
                                    Position.Y -= intersection.Height;
                                    Velocity.Y = 0; // Stop vertical movement
                                }
                                else
                                {
                                    Position.Y += intersection.Height;
                                    Velocity.Y = 0;
                                }
                            }
                            // Update rectangle after position change
                            playerRect = new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
                        }
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, Position, Color.White);
        }
    }
}