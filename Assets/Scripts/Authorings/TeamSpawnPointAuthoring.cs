using Assets.Scripts.ECS.Components;
using Assets.Scripts.ECS.Components.Spawners;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Authorings
{
    internal enum Teams : byte
    {
        Team1,
        Team2,
    }

    public class TeamSpawnPointAuthoring : MonoBehaviour
    {
        [SerializeField] private float _radius = 50f;
        [SerializeField] private Teams _team;

        class Baker : Baker<TeamSpawnPointAuthoring>
        {
            public override void Bake(TeamSpawnPointAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TeamSpawnPointComponent { Radius = authoring._radius });

                if (authoring._team == Teams.Team1)
                    AddComponent(entity, new TeamComponent { Value = 1 });
                else
                    AddComponent(entity, new TeamComponent { Value = 2 });
            }
        }
    }
}
