using APIClients;
using APIClients.API;
using APIClients.GameServerManager;
using APIClients.PresenceService;
using Assets.Scripts.Utils;
using Backend.Common.DTOs.Util;
using System;
using System.Collections;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_SERVER
using Assets.Scripts.Utils;
#endif

public static class ApiService
{
    private static readonly IAPIClient _apiClient;
    private static readonly IPresenceServiceClient _presenceServiceClient;
    private static readonly IGameServerManagerClient _gameServerManagerClient;

    public static IAPIClient Api => _apiClient;
    public static IPresenceServiceClient Presence => _presenceServiceClient;
    public static IGameServerManagerClient GameServer => _gameServerManagerClient;

    private static string _baseAddress = "http://49.13.235.223/";

    static ApiService()
    {
#if UNITY_SERVER
        _apiClient = new APIClient(CreateHttpClient(EnvironmentConfig.API_ADDRESS));
        _presenceServiceClient = new PresenceServiceClient(CreateHttpClient(EnvironmentConfig.PRESENCE_ADDRESS));
        _gameServerManagerClient = new GameServerManagerClient(CreateHttpClient(EnvironmentConfig.GAME_SERVER_MANAGER_ADDRESS));
#else
        _apiClient = new APIClient(CreateHttpClient($"{_baseAddress}api/"));
        _presenceServiceClient = new PresenceServiceClient(CreateHttpClient($"{_baseAddress}presence/"));
        _gameServerManagerClient = new GameServerManagerClient(CreateHttpClient($"{_baseAddress}game-server-manager/"));
#endif
    }

    // Coroutine wrapper for calls that return a response body
    public static IEnumerator Call<TResponse>(
        Func<Task<TResponse>> apiCall,
        Action<TResponse> onSuccess,
        Action<ErrorResponseDTO> onError)
    {
        var task = apiCall();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
            HandleException(task.Exception?.InnerException ?? task.Exception, onError);
        else
            onSuccess?.Invoke(task.Result);
    }

    // Coroutine wrapper for calls with no response body
    public static IEnumerator Call(
        Func<Task> apiCall,
        Action onSuccess,
        Action<ErrorResponseDTO> onError)
    {
        var task = apiCall();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
            HandleException(task.Exception?.InnerException ?? task.Exception, onError);
        else
            onSuccess?.Invoke();
    }

    private static void HandleException(Exception ex, Action<ErrorResponseDTO> onError)
    {
        if (ex is ApiException apiEx)
        {
            if (apiEx.StatusCode == 401 && !string.IsNullOrEmpty(SessionManager.Token))
            {
                SessionManager.Expire();
                return;
            }

            onError?.Invoke(apiEx.ToErrorResponse() ?? new ErrorResponseDTO
            {
                StatusCode = apiEx.StatusCode,
                Message = apiEx.Message
            });
        }
        else
        {
            onError?.Invoke(new ErrorResponseDTO { StatusCode = 0, Message = ex?.Message });
        }
    }

    // HttpClient with a delegating handler so the auth token is applied per-request
    private static HttpClient CreateHttpClient(string ipAddress)
    {
        var client = new HttpClient(new AuthorizationHandler { InnerHandler = new HttpClientHandler() })
        {
            BaseAddress = new Uri(ipAddress)
        };
        return client;
    }

    public static void OnError(ErrorResponseDTO error)
    {
        Debug.LogError($"API Error: {error.StatusCode} - {error.Message}");
    }

    private class AuthorizationHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string token = SessionManager.Token;
#if UNITY_SERVER
            if (string.IsNullOrEmpty(token))
                token = EnvironmentConfig.SERVER_AUTH_TOKEN;
#endif
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
