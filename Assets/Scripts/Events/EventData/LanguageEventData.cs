using Assets.Scripts.Utils.I18N;
using UnityEngine;
using System;

namespace GenericEventSystem.EventData
{
	[Serializable]
	public class LanguageEventData : EventData
	{
		public Languages Language;
	}
}
