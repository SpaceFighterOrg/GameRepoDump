using Assets.Scripts.Utils;
using Unity.NetCode;

namespace Assets.Scripts.Networking
{
    [UnityEngine.Scripting.Preserve]
    public class GameBootstrap : ClientServerBootstrap
    {
        public override bool Initialize(string defaultWorldName)
        {
#if UNITY_SERVER
            AutoConnectPort = ushort.TryParse(EnvironmentConfig.GAME_PORT, out var p) ? p : (ushort)7979;
#elif !UNITY_EDITOR
            CreateClientWorld(defaultWorldName);
            return false;
#endif
            return base.Initialize(defaultWorldName);
        }
    }
}