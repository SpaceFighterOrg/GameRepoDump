using UnityEngine;
using System;

namespace GenericEventSystem.EventData
{
	[Serializable]
	public class ThrottleChangedEventData : EventData
	{
		public int newValue;
	}
}
