using Assets.Scripts.Utils;
using Backend.Common.DTOs.Matchmaking;
using Backend.Common.DTOs.Party;
using Backend.Common.SignalR;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_SERVER
using Assets.Scripts.Utils;
#endif

public static class MatchmakingHubService
{

#if UNITY_SERVER
    private static readonly string HubUrl = $"{EnvironmentConfig.PRESENCE_ADDRESS}/matchmakingHub";
#else
    private static readonly string HubUrl = "http://49.13.235.223/presence/matchmakingHub";
#endif
    public static event Action<PartyDTO> OnPartyUpdated;
    public static event Action OnPartyDisbanded;
    public static event Action<PartyInviteDTO> OnPartyInvited;
    public static event Action<MatchmakeResultDTO> OnMatchFound;

    public static bool IsConnected => _connection?.State == HubConnectionState.Connected;

    private static HubConnection _connection;
    private static SynchronizationContext _mainThread;

    static MatchmakingHubService()
    {
        SessionManager.OnExpired += DisconnectAsync;
    }

    public static async void ConnectAsync()
    {
        if (_connection?.State == HubConnectionState.Connected) return;

        _mainThread = SynchronizationContext.Current;

        _connection = new HubConnectionBuilder()
            .WithUrl(HubUrl, opts =>
            {
                opts.AccessTokenProvider = () =>
                    Task.FromResult(SessionManager.Token);
#if !UNITY_EDITOR && UNITY_ANDROID
                opts.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
#endif
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<PartyDTO>(SignalRMethods.PARTY_UPDATED.ToString(),
            party => _mainThread.Post(_ => OnPartyUpdated?.Invoke(party), null));

        _connection.On(SignalRMethods.PARTY_DISBANDED.ToString(),
            () => _mainThread.Post(_ => OnPartyDisbanded?.Invoke(), null));

        _connection.On<PartyInviteDTO>(SignalRMethods.PARTY_INVITED.ToString(),
            invite => _mainThread.Post(_ => OnPartyInvited?.Invoke(invite), null));

        _connection.On<MatchmakeResultDTO>(SignalRMethods.MATCHMAKE_FOUND.ToString(),
            result => _mainThread.Post(_ => OnMatchFound?.Invoke(result), null));

        const int maxAttempts = 5;
        const int retryDelayMs = 3000;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _connection.StartAsync();
                Debug.Log($"[MatchmakingHub] Connected on attempt {attempt}.");
                return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchmakingHub] Attempt {attempt}/{maxAttempts} failed: " +
                               $"{ex.GetType().Name}: {ex.Message}" +
                               $"\nInner: {ex.InnerException?.GetType().Name}: {ex.InnerException?.Message}" +
                               $"\n{ex}");
                if (attempt < maxAttempts)
                    await Task.Delay(retryDelayMs);
            }
        }
    }

    public static async void DisconnectAsync()
    {
        if (_connection == null) return;

        await _connection.StopAsync();
        await _connection.DisposeAsync();
        _connection = null;
    }

    public static async void CreateParty(int elo) =>
        await _connection.InvokeAsync("CreateParty", elo);

    public static async void InvitePlayer(int userId) =>
        await _connection.InvokeAsync("InvitePlayer", userId);

    public static async void AcceptInvite(string partyId, int elo) =>
        await _connection.InvokeAsync("AcceptInvite", partyId, elo);

    public static async void DeclineInvite(string partyId) =>
        await _connection.InvokeAsync("DeclineInvite", partyId);

    public static async void LeaveParty() =>
        await _connection.InvokeAsync("LeaveParty");

    public static async void EnterMatchmaking(int attackingPlanetId, int defendingPlanetId) =>
        await _connection.InvokeAsync("EnterMatchmaking", attackingPlanetId, defendingPlanetId);

    public static async void LeaveMatchmaking() =>
        await _connection.InvokeAsync("LeaveMatchmaking");

    public static async void KickPlayer(int userId) =>
        await _connection.InvokeAsync("KickPlayer", userId);

    public static async void SetReady(bool isReady) =>
        await _connection.InvokeAsync("SetReady", isReady);
}