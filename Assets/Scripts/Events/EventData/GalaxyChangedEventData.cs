using System;
using Backend.Common.DTOs.Map;

namespace GenericEventSystem.EventData
{
	[Serializable]
	public class GalaxyChangedEventData : EventData
	{
		public GalaxyViewDTO Galaxy;
	}
}
