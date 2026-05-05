using TMPro;
using UnityEngine;

namespace Assets.Scripts.MVVM
{
    public class GameOverView : MonoBehaviour
    {
        public static GameOverView Instance { get; private set; }

        public TextMeshProUGUI text;

        private void Awake()
        {
            Instance = this;
            text.text = string.Empty;
        }

        public void SetText(string newText)
        {
            if (text)
            {
                text.text = newText;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
