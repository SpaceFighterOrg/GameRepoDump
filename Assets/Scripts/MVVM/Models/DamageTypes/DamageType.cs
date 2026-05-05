using System;

namespace Assets.Scripts.MVVM.Models.DamageTypes
{
    public enum DamageType
    {
        Regular,
        Ion,
        Kinetic,
    }

    public static class DamageTypeExtensions
    {
        private static readonly int _length = Enum.GetValues(typeof(DamageType)).Length;

        public static int ElementCount(this DamageType type)
        {
            return _length;
        }
    }
}