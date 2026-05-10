using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Light_Souls
{
    public class Animation
    {
        public List<Texture2D> Frames { get; }
        public float FrameTime { get; set; }
        public bool IsLooping { get; set; }
        public bool IsFinished { get; private set; }

        private int _currentFrame;
        private float _timer;

        public Animation(List<Texture2D> frames, float frameTime = 0.1f, bool looping = true)
        {
            Frames = frames;
            FrameTime = frameTime;
            IsLooping = looping;
            _currentFrame = 0;
            _timer = 0f;
            IsFinished = false;
        }

        public void Update(float deltaTime)
        {
            if (IsFinished) return;

            _timer += deltaTime;
            if (_timer >= FrameTime)
            {
                _timer -= FrameTime;
                _currentFrame++;

                if (_currentFrame >= Frames.Count)
                {
                    if (IsLooping)
                        _currentFrame = 0;
                    else
                    {
                        _currentFrame = Frames.Count - 1;
                        IsFinished = true;
                    }
                }
            }
        }

        public Texture2D GetCurrentFrame()
        {
            return Frames[_currentFrame];
        }

        public void Reset()
        {
            _currentFrame = 0;
            _timer = 0f;
            IsFinished = false;
        }
    }
}