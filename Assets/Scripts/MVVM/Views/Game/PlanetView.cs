using Assets.Scripts.Utils;
using Backend.Common.DTOs.Map;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.MVVM.Views.Game
{
    public class PlanetView : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = 0.01f;
        private MeshRenderer _planetRenderer;
        private MeshRenderer _atmosphereRenderer;

        void Start()
        {
            var seed = SessionManager.LatestGalaxyPull.Planets.First(x => x.Id == SessionManager.AttackingPlanetId).Seed;
            _atmosphereRenderer = transform.Find("Atmosphere")?.GetComponent<MeshRenderer>();
            _planetRenderer = transform.Find("Planet")?.GetComponent<MeshRenderer>();
            SetMaterialSeed(seed);
        }

        void Update()
        {
            transform.Rotate(Vector3.up, _rotationSpeed);
        }

        private void SetMaterialSeed(PlanetSeedDTO dto)
        {
            static Color ToUnityColor(ColorDTO c) => new(c.R / 255f, c.G / 255f, c.B / 255f);

            var mat = _planetRenderer.material;

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
    }
}
