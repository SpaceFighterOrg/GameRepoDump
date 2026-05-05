using Assets.Scripts.Utils;
using Backend.Common.DTOs.Map;
using GenericEventSystem.EventData;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.MVVM.Views.GalaxyMap
{
    public class PlanetView : ViewBase
    {
        [SerializeField] private FactionRegistry _factionRegistry;

        private TextMeshPro _label;
        private PlanetDonutFactionView _factionView;
        private GameObject _playerShip;

        private int _planetId;
        public int PlnaetId => _planetId;

        private MeshRenderer _renderer;
        private MeshRenderer _atmosphereRenderer;

        public void HandlePlayerShipMoved(EventData data)
        {
            var eventData = (PlanetIdEventData)data;
            _playerShip.SetActive(eventData.PlanetId == _planetId);
            _playerShip.transform.Rotate(Vector3.right, -90f);
            _playerShip.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));
        }

        private void Awake()
        {
            _label = GetComponentInChildren<TextMeshPro>();
            _factionView = GetComponentInChildren<PlanetDonutFactionView>();
            _playerShip = transform.Find("PlayerShip")?.gameObject;
            _playerShip.GetComponentInChildren<MeshRenderer>().sortingOrder = 11;
            _renderer = GetComponent<MeshRenderer>();
            _atmosphereRenderer = transform.Find("Atmosphere")?.GetComponent<MeshRenderer>();
        }

        public void Bind(MapPlanetDTO dto)
        {
            _planetId = dto.Id;
            SetMaterialSeed(dto.Seed);
            _playerShip.SetActive(SessionManager.LocationPlanetId == _planetId);
            transform.localPosition = new Vector3(dto.X, dto.Y, 0);
            _factionView.Refresh(dto.BattleStats, _factionRegistry);
            _label.text = PlanetTitleConverter.Translate(dto.Name);
        }

        private void SetMaterialSeed(PlanetSeedDTO dto)
        {
            static Color ToUnityColor(ColorDTO c) => new(c.R / 255f, c.G / 255f, c.B / 255f);

            var mat = _renderer.material;

            mat.SetColor("_ColorA", ToUnityColor(dto.Color1));
            mat.SetColor("_ColorB", ToUnityColor(dto.Color2));
            mat.SetColor("_ColorC", ToUnityColor(dto.Color3));
            mat.SetFloat("_Smoothness", (float)dto.Smoothness);
            mat.SetFloat("_Scale", (float)dto.Scale);
            mat.SetFloat("_LandThreshold", (float)dto.LandThreshold);
            mat.SetFloat("_EdgeSmoothness", (float)dto.EdgeSmoothness);
            mat.SetFloat("_NormalStrength", (float)dto.NormalStrength);
            mat.SetFloat("_Octaves", dto.Octaves);
            mat.SetFloat("_Contrast", (float)dto.Contrast);

            var atmosMat = _atmosphereRenderer.material;
            atmosMat.SetColor("_Color", ToUnityColor(dto.AtmosphereColor));
            atmosMat.SetFloat("_Intensity", (float)dto.AtmosphereIntensity);
            atmosMat.SetFloat("_Rim", dto.AtmosphereRim);
            atmosMat.SetFloat("_Opacity", dto.AtmosphereOpacity);
        }

        public void HandleLangaugeUpdated(EventData data)
        {
            var langData = data as LanguageEventData;
            Debug.Log(langData.Language);
            _label.text=PlanetTitleConverter.Translate(langData.Language,_label.text);
        }
    }
}