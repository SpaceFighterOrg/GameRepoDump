using Assets.Scripts.Utils;
using Backend.Common.DTOs;
using Backend.Common.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnlineHubService : MonoBehaviour
{
    private const string HubUrl = "https://49.13.235.223/presence/onlineHub";
    private const float HeartbeatInterval = 30f;

    public static OnlineHubService Instance { get; private set; }

    public static event Action<List<OnlineFriendsDTO>> OnFriendsUpdated;

    private HubConnection _connection;
    private Coroutine _heartbeatCoroutine;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SessionManager.OnExpired += DisconnectAsync;
    }

    private void OnDestroy()
    {
        SessionManager.OnExpired -= DisconnectAsync;
    }

    public async void ConnectAsync()
    {
        if (_connection?.State == HubConnectionState.Connected) return;

        _connection = new HubConnectionBuilder()
            .WithUrl(HubUrl, opts =>
            {
                opts.AccessTokenProvider = () =>
                    System.Threading.Tasks.Task.FromResult(SessionManager.Token);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<List<OnlineFriendsDTO>>(SignalRMethods.ONLINE_FRIENDS.ToString(), friends =>
        {
            OnFriendsUpdated?.Invoke(friends);
        });

        _connection.Reconnected += _ =>
        {
            SetOnlineAsync();
            return System.Threading.Tasks.Task.CompletedTask;
        };

        try
        {
            await _connection.StartAsync();
            await SetOnlineAsync();
            _heartbeatCoroutine = StartCoroutine(HeartbeatCoroutine());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OnlineHub] Connection failed: {ex.Message}");
        }
    }

    public async void DisconnectAsync()
    {
        if (_heartbeatCoroutine != null)
            StopCoroutine(_heartbeatCoroutine);

        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private async System.Threading.Tasks.Task SetOnlineAsync()
    {
        await _connection.InvokeAsync("SetOnline", SessionManager.LocationPlanetId);
    }

    private IEnumerator HeartbeatCoroutine()
    {
        var wait = new WaitForSeconds(HeartbeatInterval);
        while (true)
        {
            yield return wait;
            if (_connection?.State == HubConnectionState.Connected)
                _ = _connection.InvokeAsync("Heartbeat");
        }
    }
}
