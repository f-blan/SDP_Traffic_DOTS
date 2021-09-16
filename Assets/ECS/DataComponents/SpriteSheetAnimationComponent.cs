using Unity.Entities;
using UnityEngine;

public struct SpriteSheetAnimationComponent : IComponentData
{
    public int currentFrame;
    public int frameCount;
    public float frameTimer;
    public Vector4 uv;
    public Matrix4x4 matrix;
}
