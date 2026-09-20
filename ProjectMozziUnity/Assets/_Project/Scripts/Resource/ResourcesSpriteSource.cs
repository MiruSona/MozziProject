using UnityEngine;

namespace Mozzi.Resource
{
    public sealed class ResourcesSpriteSource : ISpriteSource
    {
        public Sprite Load(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            return Resources.Load<Sprite>(SpriteKeys.Root + key);
        }
    }
}
