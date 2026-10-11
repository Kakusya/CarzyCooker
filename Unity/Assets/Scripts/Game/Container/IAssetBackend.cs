using System;
using GameFramework.Resource;

namespace Game
{
    // A captured load/release pair; implementations must refer to the original resource runtime.
    public interface IAssetBackend
    {
        void Load(string assetName, Type type, int priority, LoadAssetCallbacks callbacks);
        void Unload(object asset);
    }
}
