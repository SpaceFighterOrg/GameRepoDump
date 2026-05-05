using TMPro;
using UnityEngine;

namespace Assets.Scripts.MVVM
{
    public class TeamCounterWidgetView : MonoBehaviour
    {
        public static TeamCounterWidgetView Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI _team1LivesLabel;
        [SerializeField] private TextMeshProUGUI _team2LivesLabel;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetLiveCounts(int team1Lives, int team2Lives)
        {
            if (_team1LivesLabel) _team1LivesLabel.text = team1Lives.ToString();
            if (_team2LivesLabel) _team2LivesLabel.text = team2Lives.ToString();
        }
    }
}
