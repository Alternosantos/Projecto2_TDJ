using Microsoft.Xna.Framework.Input;

namespace Light_Soulls.Input
{
    public static class InputManager
    {
        private static KeyboardState _currentKeyState;
        private static KeyboardState _previousKeyState;

        public static void Update()
        {
            _previousKeyState = _currentKeyState;
            _currentKeyState = Keyboard.GetState();
        }

        public static bool IsKeyDown(Keys key) => _currentKeyState.IsKeyDown(key);
        public static bool IsKeyPressed(Keys key) =>
            _currentKeyState.IsKeyDown(key) && !_previousKeyState.IsKeyDown(key);
    }
}   