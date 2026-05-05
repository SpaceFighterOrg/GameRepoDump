using Assets.Scripts.MVVM.Models.Ships;
using Assets.Scripts.Utils.I18N;
using Backend.Common.DTOs.Map;
using System;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public static class SessionManager
    {
        private static string _token = string.Empty;
        public static string Token
        {
            get => _token;
            set
            {
                _token = value;
                PlayerPrefs.SetString("auth_token", value);
            }
        }

        public static void LoadFromPrefs()
        {
            _token = PlayerPrefs.GetString("auth_token", string.Empty);
            _language = (Languages)PlayerPrefs.GetInt("language", (int)Languages.English);
        }

        public static event Action OnLanguageChanged;

        private static Languages _language = Languages.English;
        public static Languages Language
        {
            get => _language;
            set
            {
                _language = value;
                PlayerPrefs.SetInt("language", (int)value);
                OnLanguageChanged?.Invoke();
            }
        }

        public static int UserId { get; set; }
        public static string Username { get; set; }

        public static GalaxyViewDTO LatestGalaxyPull { get; set; }

        public static int LocationPlanetId { get; set; }
        public static int FactionId { get; set; }

        public static int RoleId { get; set; }

        // Populated from JoinMatchResponseDTO when the player is assigned to a match
        public static string MatchId { get; set; }
        public static byte MatchSlotIndex { get; set; }
        public static byte Team { get; set; }
        public static int AttackingPlanetId { get; set; }
        public static int DefendingPlanetId { get; set; }
        public static string ServerIp { get; set; }
        public static int ServerPort { get; set; }

        public static ShipData ShipData { get; set; }
        public static float LatestFiredProjectileSpeed { get; set; }

        public static event Action OnExpired;

        public static void Expire()
        {
            Token = string.Empty;
            UserId = 0;
            Username = null;
            FactionId = 0;
            LocationPlanetId = 0;
            RoleId = 0;
            LatestGalaxyPull = null;
            MatchId = null;
            MatchSlotIndex = 0;
            Team = 0;
            AttackingPlanetId = 0;
            DefendingPlanetId = 0;
            ServerIp = null;
            ServerPort = 0;
            OnExpired?.Invoke();
        }
    }
}
