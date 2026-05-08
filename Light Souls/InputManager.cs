using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light_Souls
{
    public class InputManager
    {
        private readonly float _speed = 200f;
        public float Movement { get; set; }

        public void Update()
        {
            KeyboardState ks = Keyboard.GetState();
            Movement = 0;

            if (ks.IsKeyDown(Keys.D))
            {
                Movement = -_speed;
            }
            else if (ks.IsKeyDown(Keys.A))
            {
                Movement = _speed;
            }
        }
    }
}
