using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Authorings
{
    public class IncludeAuthoring : MonoBehaviour
    {
        private class MyBaker : Baker<IncludeAuthoring>
        {
            public override void Bake(IncludeAuthoring authoring)
            {
                _ = GetEntity(TransformUsageFlags.Dynamic);
            }
        }
    }
}
