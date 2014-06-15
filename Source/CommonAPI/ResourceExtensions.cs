// Copyright 2014 Bill Currie <bill@taniwha.org>
// This file is in the public domain (use it as you see fit).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace KSPAPIExtensions {
	/// <summary>
	/// Extended resource attributes
	/// </summary>
	public class ExtendedResourceDefinition
	{
		PartResourceDefinition res_def;

		public bool isMineable = false;
		public bool isHullResource = false;
		public float volume = 5;		// liters per unit

		public string name
		{
			get {
				return res_def.name;
			}
		}
		public Color color
		{
			get {
				return res_def.color;
			}
		}
		public float density
		{
			get {
				return res_def.density;
			}
		}
		public int id
		{
			get {
				return res_def.id;
			}
		}
		public bool isTweakable
		{
			get {
				return res_def.isTweakable;
			}
		}
		public ResourceFlowMode resourceFlowMode
		{
			get {
				return res_def.resourceFlowMode;
			}
		}
		public ResourceTransferMode resourceTransferMode
		{
			get {
				return res_def.resourceTransferMode;
			}
		}

		public void Load (ConfigNode node)
		{
			string name = node.GetValue ("name");
			res_def = PartResourceLibrary.Instance.GetDefinition (name);
			if (node.HasValue ("isMineable")) {
				bool.TryParse (node.GetValue ("isMineable"), out isMineable);
			}
			if (node.HasValue ("isHullResource")) {
				bool.TryParse (node.GetValue ("isHullResource"),
							   out isHullResource);
			}
			if (node.HasValue ("volume")) {
				float.TryParse (node.GetValue ("volume"), out volume);
			}
		}
	}

	/// <summary>
	/// Extended resource attributes
	/// </summary>
	public static class PartResourceDefinitionExtension
	{
		static Dictionary<string, ExtendedResourceDefinition> resource_dict;

		/// <summary>
		/// List of all resources as extended resources. The list is a private
		/// copy.
		/// </summary>
		public static List<ExtendedResourceDefinition> resources
		{
			get {
				if (resource_dict == null) {
					Initialize ();
				}
				return new List<ExtendedResourceDefinition> (resource_dict.Values);
			}
		}

		/// <summary>
		/// Find and load all resources as extended resources.
		/// </summary>
		static void Initialize ()
		{
			var dbase = GameDatabase.Instance;
			var resourceNodes = dbase.GetConfigNodes ("RESOURCE_DEFINITION");
			resource_dict = new Dictionary<string, ExtendedResourceDefinition> ();
			foreach (var resource in resourceNodes) {
				var res = new ExtendedResourceDefinition ();
				res.Load (resource);
				resource_dict.Add (res.name, res);
			}
		}

		/// <summary>
		/// Get an extended resource.
		/// </summary>
		/// <param name="name">Resource name</param>
		/// <returns>An extended resource defintiion or null if not found</returns>
		public static ExtendedResourceDefinition GetResource (string name)
		{
			if (resource_dict == null) {
				Initialize ();
			}
			if (resource_dict.ContainsKey (name)) {
				return resource_dict[name];
			}
			return null;
		}

		/// <summary>
		/// Can the resource be mined? That is, is this a raw resource (eg,
		/// Kethane, ore, etc).
		/// </summary>
		/// <param name="resdef">The resource defintion</param>
		/// <returns>Whether the resource can be mined.</returns>
		public static bool isMineable (this PartResourceDefinition resdef)
		{
			return GetResource (resdef.name).isMineable;
		}

		/// <summary>
		/// Is this a resource needed for building ship "hulls". Used primarily
		/// by Extraplanetary Launchpads.
		/// </summary>
		/// <param name="resdef">The resource defintion</param>
		/// <returns>Whether the resource is mull material.</returns>
		public static bool isHullResource (this PartResourceDefinition resdef)
		{
			return GetResource (resdef.name).isHullResource;
		}

		/// <summary>
		/// The amount of space in liters taken up by one unit of the resource.
		/// 5l/u is common (LiquidFuel, Oxidizer, Ore, Metal), though RealFuels
		/// will likely set it to 1l/u.
		/// </summary>
		/// <param name="resdef">The resource defintion</param>
		/// <returns>The volume of a single unit of the resource in liters</returns>
		public static float getVolume (this PartResourceDefinition resdef)
		{
			return GetResource (resdef.name).volume;
		}
	}

}
