using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace KSPAPIExtensions {
	public static class GameObjectExtension
	{
		public static T AddTaggedComponent<T> (this GameObject go) where T : Component
		{
			Type taggedType = SystemUtils.VersionTaggedType(typeof(T));
			return (T)go.AddComponent(taggedType);
		}
		public static T GetTaggedComponent<T> (this GameObject go) where T : Component
		{
			Type taggedType = SystemUtils.VersionTaggedType(typeof(T));
			return (T)go.GetComponent(taggedType);
		}
	}
}
