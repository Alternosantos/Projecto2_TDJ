using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Light_Souls
{
    public class Level
    {
        public Vector2 PlayerStart { get; private set; }
        public List<Platform> Platforms { get; private set; }
        public List<Enemy> Enemies { get; private set; }
        public List<FlyingEnemy> FlyingEnemies { get; private set; }
        public List<ChasingEnemy> ChasingEnemies { get; private set; }
        public List<Coin> Coins { get; private set; }

        public int WorldWidth { get; private set; }
        public int WorldHeight { get; private set; }

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

        private Texture2D _platformTexture;

        // Construtor recebe texturas e o caminho do ficheiro de nível
        public Level(Texture2D[] platformTextures, Texture2D enemyTex, Texture2D coinTex, string levelPath)
        {
            Platforms = new List<Platform>();
            Enemies = new List<Enemy>();
            FlyingEnemies = new List<FlyingEnemy>();
            ChasingEnemies = new List<ChasingEnemy>();
            Coins = new List<Coin>();

            string[] lines = File.ReadAllLines(levelPath);
            Random rand = new Random();

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                string[] parts = line.Split(' ');
                string type = parts[0];

                switch (type)
                {
                    case "START":
                        float startX = float.Parse(parts[1]);
                        float startY = float.Parse(parts[2]);
                        PlayerStart = new Vector2(startX, startY);
                        break;

                    case "PLATFORM":
                        int x = int.Parse(parts[1]);
                        int y = int.Parse(parts[2]);
                        int w = int.Parse(parts[3]);
                        int h = int.Parse(parts[4]);
                        Texture2D chosenTex = platformTextures[rand.Next(platformTextures.Length)];
                        Platforms.Add(new Platform(new Rectangle(x, y, w, h), chosenTex));
                        break;

                    case "ENEMY":
                        float ex = float.Parse(parts[1]);
                        float ey = float.Parse(parts[2]);
                        Enemies.Add(new Enemy(enemyTex, new Vector2(ex, ey)));
                        break;

                    case "FLYING":
                        float fx = float.Parse(parts[1]);
                        float fy = float.Parse(parts[2]);
                        float left = float.Parse(parts[3]);
                        float right = float.Parse(parts[4]);
                        FlyingEnemies.Add(new FlyingEnemy(enemyTex, new Vector2(fx, fy), left, right));
                        break;

                    case "CHASING":
                        float cx = float.Parse(parts[1]);
                        float cy = float.Parse(parts[2]);
                        ChasingEnemies.Add(new ChasingEnemy(enemyTex, new Vector2(cx, cy)));
                        break;

                    case "COIN":
                        float cox = float.Parse(parts[1]);
                        float coy = float.Parse(parts[2]);
                        Coins.Add(new Coin(coinTex, new Vector2(cox, coy)));
                        break;
                }
            }


            // Calcular a largura do mundo (para a câmara)
            WorldWidth = 0;
            WorldHeight = 480; // valor padrão, pode ajustar
            foreach (var p in Platforms)
                WorldWidth = System.Math.Max(WorldWidth, p.Bounds.Right);
            foreach (var e in Enemies)
                WorldWidth = System.Math.Max(WorldWidth, (int)e.Position.X + 32);
            foreach (var f in FlyingEnemies)
                WorldWidth = System.Math.Max(WorldWidth, (int)f.Position.X + 32);
            foreach (var c in ChasingEnemies)
                WorldWidth = System.Math.Max(WorldWidth, (int)c.Position.X + 32);
            foreach (var c in Coins)
                WorldWidth = System.Math.Max(WorldWidth, (int)c.Position.X + 24);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var platform in Platforms)
                platform.Draw(spriteBatch);
        }
    }
}