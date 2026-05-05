using Assets.Scripts.MVVM.Models.Config;
using GenericEventSystem;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MVVM.ViewModels
{
    public class WeaponPanelViewModel : ViewModelBase
    {
        public GlobalConfig GlobalConfig;
        public EventDefinition WeaponFiredChannel;
        public GameObject WeaponButtonViewPrefab;

        public static bool IsWeaponFired(int index) => _buttonViewModels[index].IsPressedForThisTick;
        public static int WeaponCount => _buttonViewModels.Count;

        private static List<WeaponButtonViewModel> _buttonViewModels;

        private void Start()
        {
            _buttonViewModels = new();

            GlobalConfig.playerShip.Weapons.ForEach(w => {
                var weaponButtonView = Instantiate(WeaponButtonViewPrefab, transform);
                weaponButtonView.transform.SetParent(transform, false);
                var weaponButtonVM = weaponButtonView.GetComponent<WeaponButtonViewModel>();
                weaponButtonVM.Initialize(w.ProjectileType, WeaponFiredChannel);


                _buttonViewModels.Add(weaponButtonVM);
            });
        }
    }
}
