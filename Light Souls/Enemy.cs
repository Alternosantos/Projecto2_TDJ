using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    public class Enemy
    {
        public Vector2 Position;
        public Vector2 Velocity;
        private Texture2D _texture;
        private float _moveSpeed = 100f;
        private float _gravity = 1200f;
        private int _direction = 1;   // 1 = right, -1 = left

        public Enemy(Texture2D texture, Vector2 startPosition)
        {
            _texture = texture;
            Position = startPosition;
            Velocity = Vector2.Zero;
        }

        public void Update(GameTime gameTime, Tile?[,] tiles)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Horizontal movement
            Velocity.X = _direction * _moveSpeed;
            Position.X += Velocity.X * deltaTime;
            HandleHorizontalCollisions(tiles);

            // Vertical movement & gravity
            Velocity.Y += _gravity * deltaTime;
            Position.Y += Velocity.Y * deltaTime;
            HandleVerticalCollisions(tiles);

            // Turn around if no ground ahead or hitting wall
            CheckTurnAround(tiles);
        }

        private void HandleHorizontalCollisions(Tile?[,] tiles)
        {
            Rectangle enemyRect = new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    var tile = tiles[x, y];
                    if (tile.HasValue && tile.Value.Collision == TileCollision.Impassable)
                    {
                        Rectangle tileRect = new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
                        if (enemyRect.Intersects(tileRect))
                        {
                            // Resolve horizontal collision
                            if (Velocity.X > 0)
                                Position.X = tileRect.Left - _texture.Width;
                            else if (Velocity.X < 0)
                                Position.X = tileRect.Right;

                            // Turn around due to wall
                            _direction = -_direction;
                            Velocity.X = 0;

                            enemyRect = new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
                            break; // exit loops after first collision (optional, improves performance)
                        }
                    }
                }
            }
        }

        private void HandleVerticalCollisions(Tile?[,] tiles)
        {
            Rectangle enemyRect = new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                for (int y = 0; y < tiles.GetLength(1); y++)
                {
                    var tile = tiles[x, y];
                    if (tile.HasValue && (tile.Value.Collision == TileCollision.Impassable || tile.Value.Collision == TileCollision.Platform))
                    {
                        Rectangle tileRect = new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
                        if (enemyRect.Intersects(tileRect))
                        {
                            if (enemyRect.Center.Y < tileRect.Center.Y)
                            {
                                Position.Y = tileRect.Top - _texture.Height;
                                Velocity.Y = 0;
                            }
                            else if (tile.Value.Collision == TileCollision.Impassable)
                            {
                                // Hit head on impassable (only for blocks, not platforms)
                                Position.Y = tileRect.Bottom;
                                Velocity.Y = 0;
                            }
                            enemyRect = new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
                        }
                    }
                }
            }
        }

        private void CheckTurnAround(Tile?[,] tiles)
        {
            // Get the front foot position (bottom corner in movement direction)
            int frontX = (int)(Position.X + (_direction == 1 ? _texture.Width : 0));
            int footY = (int)(Position.Y + _texture.Height) + 1; // one pixel below feet

            int tileX = frontX / Tile.Width;
            int tileY = footY / Tile.Height;

            // Check if there is ground directly below the front corner
            if (tileX >= 0 && tileX < tiles.GetLength(0) && tileY >= 0 && tileY < tiles.GetLength(1))
            {
                var tileBelow = tiles[tileX, tileY];
                bool hasGround = tileBelow.HasValue &&
                    (tileBelow.Value.Collision == TileCollision.Impassable || tileBelow.Value.Collision == TileCollision.Platform);

                if (!hasGround)
                {
                    _direction = -_direction;
                }
            }
            else
            {
                // Outside level bounds → turn around
                _direction = -_direction;
            }
        }
        public bool CollidesWith(Rectangle playerBounds)
        {
            Rectangle enemyBounds = new Rectangle((int)Position.X, (int)Position.Y, _texture.Width, _texture.Height);
            return enemyBounds.Intersects(playerBounds);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(_texture, Position, Color.Red);
        }
    }
}