using Assets.Scripts.Controllers;
using UnityEngine.UI;

namespace Assets.Scripts.MVVM.ViewModels
{
    public class CalibrationViewModel : ViewModelBase
    {
        void Start()
        {
            var button = GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                GyroSpaceshipController.Calibrate();
            });
        }
    }
}