using Unity.Entities;
using UnityEngine;

public struct ChangeColorTag : IComponentData
{
    public Color newColor;
}
