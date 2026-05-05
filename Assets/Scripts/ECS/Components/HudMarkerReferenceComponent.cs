using Assets.Scripts.Interfaces;
using Unity.Entities;

namespace Assets.Scripts.ECS.Components
{
    public class HudMarkerReferenceComponent : IComponentData
    {
        public IHudMarker Hud;
    }
}
