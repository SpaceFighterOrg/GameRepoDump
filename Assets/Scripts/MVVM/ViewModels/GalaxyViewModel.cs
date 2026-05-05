using Assets.Scripts.Utils;
using Backend.Common.DTOs.Map;
using GenericEventSystem;
using GenericEventSystem.EventData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GalaxyViewModel : MonoBehaviour
{
    [SerializeField] private EventDefinition _galaxyUpdatedChannel;

    private float _pollingTimeElapsed;
    private const float PollingInterval = 8 * 60; // 8 minutes

    private IEnumerator FetchPlanets()
    {
        yield return ApiService.Call(ApiService.Api.GalaxyAsync,
            response =>
            {
                response.Planets[0].BattleStats.AddRange(new List<PlanetBattleStatsDTO>() { new() { FactionId = 1, ControlPoints = 12 }, new() { FactionId = 2, ControlPoints = 45 }, new() { FactionId = 3, ControlPoints = 67 } });
                SessionManager.LatestGalaxyPull = response;                
                _galaxyUpdatedChannel.Raise(new GalaxyChangedEventData { Galaxy = response });
            },
            ApiService.OnError);
    }

    private void Start()
    {
        _pollingTimeElapsed = PollingInterval;
        StartCoroutine(InitialLoad());
    }

    public void HandleLoginEvent(EventData data)
    {
        StartCoroutine(InitialLoad());
    }

    private IEnumerator InitialLoad()
    {
        yield return FetchPlanets();
    }

    private void Update()
    {
        _pollingTimeElapsed -= Time.deltaTime;
        if(_pollingTimeElapsed < 0)
        {
            StartCoroutine(FetchPlanets());
            _pollingTimeElapsed = PollingInterval;
            Debug.Log($"Polled planets at time {Time.time}");
        }
    }
}
