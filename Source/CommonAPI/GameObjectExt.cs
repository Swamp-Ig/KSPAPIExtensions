using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace KSPAPIExtensions {
	internal static class GameObjectExtension
	{
		internal static T AddTaggedComponent<T> (this GameObject go) where T : Component
		{
			Type taggedType = SystemUtils.VersionTaggedType(typeof(T));
			return (T)go.AddComponent(taggedType);
		}
		internal static T GetTaggedComponent<T> (this GameObject go) where T : Component
		{
			Type taggedType = SystemUtils.VersionTaggedType(typeof(T));
			return (T)go.GetComponent(taggedType);
		}
	}
}
