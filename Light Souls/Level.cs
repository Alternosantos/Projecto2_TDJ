using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    /// <summary>
    /// Parses a level definition file and owns all entities that belong to it.
    /// </summary>
    public sealed class Level
    {
        // ── Properties ───────────────────────────────────────────────────────────

        /// <summary>World-space spawn position for the player.</summary>
        public Vector2 PlayerStart { get; private set; }

        public IReadOnlyList<Platform>     Platforms      { get; private set; }
        public IReadOnlyList<Enemy>        Enemies        { get; private set; }
        public IReadOnlyList<FlyingEnemy>  FlyingEnemies  { get; private set; }
        public IReadOnlyList<ChasingEnemy> ChasingEnemies { get; private set; }
        public IReadOnlyList<Coin>         Coins          { get; private set; }

        /// <summary>Total pixel width of this level (derived from entity positions).</summary>
        public int WorldWidth  { get; private set; }

        /// <summary>Total pixel height of this level.</summary>
        public int WorldHeight { get; private set; }

        /// <summary>Number of coins that have not yet been collected.</summary>
        public int RemainingCoins
        {
            get
            {
                int count = 0;
                foreach (var coin in Coins)
                    if (!coin.IsCollected) count++;
                return count;
            }
        }

        // ── Private backing lists ────────────────────────────────────────────────

        private readonly List<Platform>     _platforms      = new List<Platform>();
        private readonly List<Enemy>        _enemies        = new List<Enemy>();
        private readonly List<FlyingEnemy>  _flyingEnemies  = new List<FlyingEnemy>();
        private readonly List<ChasingEnemy> _chasingEnemies = new List<ChasingEnemy>();
        private readonly List<Coin>         _coins          = new List<Coin>();

        // ── Constructor ──────────────────────────────────────────────────────────

        /// <param name="platformTextures">Pool of textures randomly assigned to platforms.</param>
        /// <param name="enemyTexture">Shared texture for all enemy types.</param>
        /// <param name="coinTexture">Texture for collectible coins.</param>
        /// <param name="levelPath">Path to the level definition text file.</param>
        public Level(Texture2D[] platformTextures, Texture2D enemyTexture,
                     Texture2D coinTexture, string levelPath)
        {
            ParseLevelFile(levelPath, platformTextures, enemyTexture, coinTexture);

            // Expose immutable views to the outside world
            Platforms      = _platforms.AsReadOnly();
            Enemies        = _enemies.AsReadOnly();
            FlyingEnemies  = _flyingEnemies.AsReadOnly();
            ChasingEnemies = _chasingEnemies.AsReadOnly();
            Coins          = _coins.AsReadOnly();

            CalculateWorldSize();
        }

        // ── Public methods ───────────────────────────────────────────────────────

        /// <summary>Draws all platforms in the level.</summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var platform in _platforms)
                platform.Draw(spriteBatch);
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private void ParseLevelFile(string path, Texture2D[] platformTextures,
                                     Texture2D enemyTex, Texture2D coinTex)
        {
            var rng = new Random();

            foreach (string rawLine in File.ReadAllLines(path))
            {
                // Strip // and # comments (including inline ones)
                string line = rawLine;
                int slashComment = line.IndexOf("//", StringComparison.Ordinal);
                if (slashComment >= 0) line = line.Substring(0, slashComment);
                int hashComment = line.IndexOf('#');
                if (hashComment >= 0) line = line.Substring(0, hashComment);

                line = line.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] parts = line.Split(new[] { ' ', '\t' },
                                            StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                switch (parts[0])
                {
                    case "START":
                        PlayerStart = new Vector2(float.Parse(parts[1]), float.Parse(parts[2]));
                        break;

                    case "PLATFORM":
                        var bounds  = new Rectangle(int.Parse(parts[1]), int.Parse(parts[2]),
                                                    int.Parse(parts[3]), int.Parse(parts[4]));
                        var texture = platformTextures[rng.Next(platformTextures.Length)];
                        _platforms.Add(new Platform(bounds, texture));
                        break;

                    case "ENEMY":
                        _enemies.Add(new Enemy(enemyTex,
                            new Vector2(float.Parse(parts[1]), float.Parse(parts[2]))));
                        break;

                    case "FLYING":
                        _flyingEnemies.Add(new FlyingEnemy(enemyTex,
                            new Vector2(float.Parse(parts[1]), float.Parse(parts[2])),
                            float.Parse(parts[3]), float.Parse(parts[4])));
                        break;

                    case "CHASING":
                        _chasingEnemies.Add(new ChasingEnemy(enemyTex,
                            new Vector2(float.Parse(parts[1]), float.Parse(parts[2]))));
                        break;

                    case "COIN":
                        _coins.Add(new Coin(coinTex,
                            new Vector2(float.Parse(parts[1]), float.Parse(parts[2]))));
                        break;
                }
            }
        }


        private void CalculateWorldSize()
        {
            const int defaultHeight = 480;
            WorldHeight = defaultHeight;
            WorldWidth  = 0;

            foreach (var p in _platforms)      WorldWidth = Math.Max(WorldWidth, p.Bounds.Right);
            foreach (var e in _enemies)        WorldWidth = Math.Max(WorldWidth, (int)e.Position.X + 32);
            foreach (var f in _flyingEnemies)  WorldWidth = Math.Max(WorldWidth, (int)f.Position.X + 32);
            foreach (var c in _chasingEnemies) WorldWidth = Math.Max(WorldWidth, (int)c.Position.X + 32);
            foreach (var c in _coins)          WorldWidth = Math.Max(WorldWidth, (int)c.Position.X + 24);
        }
    }
}