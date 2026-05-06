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

        private float _moveSpeed = 300f;
        private float _jumpPower = -500f;
        private float _gravity = 1600f;

        public Player(Texture2D texture, Vector2 startPosition)
        {
            _texture = texture;
            Position = startPosition;
            Velocity = Vector2.Zero;
        }

        public void Update(GameTime gameTime, Tile?[,] tiles)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Input
            var keyboard = Keyboard.GetState();
            float moveX = 0;
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left)) moveX = -1;
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right)) moveX = 1;
            Velocity.X = moveX * _moveSpeed;

            // Horizontal movement & collision
            Position.X += Velocity.X * deltaTime;
            HandleHorizontalCollisions(tiles);

            // Vertical movement & gravity
            Velocity.Y += _gravity * deltaTime;
            Position.Y += Velocity.Y * deltaTime;
            HandleVerticalCollisions(tiles);

            // Jumping
            if ((keyboard.IsKeyDown(Keys.Space) || keyboard.IsKeyDown(Keys.Up)) && IsGrounded(tiles))
            {
                Velocity.Y = _jumpPower;
            }
        }

        private bool IsGrounded(Tile?[,] tiles)
        {
            int footY = (int)(Position.Y + _texture.Height);
            int tileX = (int)(Position.X + _texture.Width / 2) / Tile.Width;
            int tileY = footY / Tile.Height;

            if (tileX >= 0 && tileX < tiles.GetLength(0) && tileY >= 0 && tileY < tiles.GetLength(1))
            {
                var tile = tiles[tileX, tileY];
                return tile.HasValue &&
                       (tile.Value.Collision == TileCollision.Impassable || tile.Value.Collision == TileCollision.Platform) &&
                       Velocity.Y >= 0;
            }
            return false;
        }

        private void HandleHorizontalCollisions(Tile?[,] tiles)
        {
            Rectangle playerRect = new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    var tile = tiles[x, y];
                    if (tile.HasValue && tile.Value.Collision == TileCollision.Impassable)
                    {
                        Rectangle tileRect = new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
                        if (playerRect.Intersects(tileRect))
                        {
                            if (playerRect.Center.X < tileRect.Center.X)
                                Position.X = tileRect.Left - _texture.Width;
                            else
                                Position.X = tileRect.Right;
                            Velocity.X = 0;
                            playerRect = new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
                        }
                    }
                }
            }
        }

        private void HandleVerticalCollisions(Tile?[,] tiles)
        {
            Rectangle playerRect = new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    var tile = tiles[x, y];
                    if (tile.HasValue && tile.Value.Collision != TileCollision.Passable)
                    {
                        Rectangle tileRect = new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
                        if (playerRect.Intersects(tileRect))
                        {
                            // Platform: allow passing through when moving upward
                            if (tile.Value.Collision == TileCollision.Platform && Velocity.Y < 0)
                                continue;

                            // Resolve collision
                            if (playerRect.Center.Y < tileRect.Center.Y)
                            {
                                // Player is above the tile → land on top
                                Position.Y = tileRect.Top - _texture.Height;
                                Velocity.Y = 0;
                            }
                            else
                            {
                                // Player below tile (hitting head) – only for impassable
                                if (tile.Value.Collision == TileCollision.Impassable)
                                {
                                    Position.Y = tileRect.Bottom;
                                    Velocity.Y = 0;
                                }
                            }
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