using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Light_Souls
{
    /// <summary>
    /// Frame-based sprite animation. Supports looping and one-shot playback.
    /// </summary>
    public sealed class Animation
    {
        // ── Properties ───────────────────────────────────────────────────────────

        /// <summary>All frames in the animation sequence.</summary>
        public IReadOnlyList<Texture2D> Frames { get; }

        /// <summary>Time in seconds each frame is displayed.</summary>
        public float FrameTime { get; set; }

        /// <summary>Whether the animation loops when it reaches the last frame.</summary>
        public bool IsLooping { get; set; }

        /// <summary>True once a non-looping animation has played through completely.</summary>
        public bool IsFinished { get; private set; }

        // ── Private state ────────────────────────────────────────────────────────

        private readonly List<Texture2D> _frames;
        private int   _currentFrameIndex;
        private float _elapsed;

        // ── Constructor ──────────────────────────────────────────────────────────

        /// <param name="frames">Ordered list of textures that make up this animation.</param>
        /// <param name="frameTime">Seconds per frame.</param>
        /// <param name="looping">If true the animation repeats; otherwise it stops at the last frame.</param>
        public Animation(List<Texture2D> frames, float frameTime = 0.1f, bool looping = true)
        {
            _frames            = frames;
            Frames             = frames.AsReadOnly();
            FrameTime          = frameTime;
            IsLooping          = looping;
            _currentFrameIndex = 0;
            _elapsed           = 0f;
            IsFinished         = false;
        }

        // ── Public methods ───────────────────────────────────────────────────────

        /// <summary>Advances the animation by <paramref name="deltaTime"/> seconds.</summary>
        public void Update(float deltaTime)
        {
            if (IsFinished) return;

            _elapsed += deltaTime;
            while (_elapsed >= FrameTime)
            {
                _elapsed -= FrameTime;
                _currentFrameIndex++;

                if (_currentFrameIndex >= _frames.Count)
                {
                    if (IsLooping)
                    {
                        _currentFrameIndex = 0;
                    }
                    else
                    {
                        _currentFrameIndex = _frames.Count - 1;
                        IsFinished = true;
                        return;
                    }
                }
            }
        }

        /// <summary>Returns the texture for the current frame.</summary>
        public Texture2D GetCurrentFrame() => _frames[_currentFrameIndex];

        /// <summary>Resets the animation to its first frame.</summary>
        public void Reset()
        {
            _currentFrameIndex = 0;
            _elapsed           = 0f;
            IsFinished         = false;
        }
    }
}