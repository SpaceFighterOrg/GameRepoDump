using System;
using Assets.Scripts.MVVM.Models.Projectiles;

namespace GenericEventSystem.EventData
{
	[Serializable]
	public class WeaponFiredEventData : EventData
	{
		public ProjectileType projectileType;
	}
}
