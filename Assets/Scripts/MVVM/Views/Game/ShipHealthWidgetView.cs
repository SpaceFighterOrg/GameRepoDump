using Assets.Scripts.MVVM.Models.Ships;
using Assets.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.MVVM
{
    public class ShipHealthWidgetView : MonoBehaviour
    {
        public static ShipHealthWidgetView Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI _healthLabel;
        [SerializeField] private TextMeshProUGUI _shieldLabel;
        [SerializeField] private ShipData _defaultShipData;

        void Awake()
        {
            Instance = this;
            if (_defaultShipData != null)
            {
                SessionManager.ShipData = _defaultShipData;
                SetValues(_defaultShipData.MaxHealth, _defaultShipData.MaxHealth,
                          _defaultShipData.MaxShield, _defaultShipData.MaxShield);
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetValues(int health, int maxHealth, int shield, int maxShield)
        {
            if (_healthLabel) _healthLabel.text = $"{health}/{maxHealth}";
            if (_shieldLabel) _shieldLabel.text = $"{shield}/{maxShield}";
        }
    }
}
