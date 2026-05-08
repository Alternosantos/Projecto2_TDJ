using System;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls { 
public class GameManager
{
    private readonly BGManager _bgm = new();
    private readonly InputManager _im = new();

    public GameManager()
    {
        //_bgm.AddLayer(new(Globals.Content.Load<Texture2D>("background/Layer0"), 0.0f, 0.4f));
        _bgm.AddLayer(new(Globals.Content.Load<Texture2D>("background/Layer1"), 0.0f, 0.4f));
    }

    public void Update()
    {
        _im.Update();
        _bgm.Update(_im.Movement);
    }

    public void Draw()
    {
        _bgm.Draw();
    }
}

}