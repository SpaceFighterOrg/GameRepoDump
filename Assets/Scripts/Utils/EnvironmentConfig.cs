using Backend.Common.EnvironmentConfig;

namespace Assets.Scripts.Utils
{
    public class EnvironmentConfig : EnvironmentConfigBase
    {
        public static string MAX_MATCHES =>
            Require(nameof(MAX_MATCHES));

        public static string INITIAL_TEAM_LIFEPOOL_SIZE =>
            Require(nameof(INITIAL_TEAM_LIFEPOOL_SIZE));

        public static string SERVER_AUTH_TOKEN =>
            Require(nameof(SERVER_AUTH_TOKEN));

        public static string API_ADDRESS =>
            Require(nameof(API_ADDRESS));

        public static string PRESENCE_ADDRESS =>
            Require(nameof(PRESENCE_ADDRESS));

        public static string GAME_SERVER_MANAGER_ADDRESS =>
            Require(nameof(GAME_SERVER_MANAGER_ADDRESS));

        public static string GAME_PORT =>
            Require(nameof(GAME_PORT));
    }
}
