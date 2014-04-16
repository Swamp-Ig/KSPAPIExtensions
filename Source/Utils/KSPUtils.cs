using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace KSPAPIExtensions
{
    /// <summary>
    /// Flags to determine relationship between two parts.
    /// </summary>
    [Flags]
    public enum PartRelationship
    {
        Vessel = 0x1,
        Self = 0x2,
        Symmetry = 0x4,
        Decendent = 0x8 ,
        Child = 0x10,
        Ancestor = 0x20,
        Parent = 0x40,
        Sibling = 0x80,
        Unrelated = 0x100,
        Unknown = 0x0
    }

    /// <summary>
    /// Flags to filter particular game scenes.
    /// </summary>
    [Flags]
    public enum GameSceneFilter
    {
        Loading = 1 << (int)GameScenes.LOADING,
        MainMenu = 1 << (int)GameScenes.MAINMENU,
        SpaceCenter = 1 << (int)GameScenes.SPACECENTER,
        VAB = 1 << (int)GameScenes.EDITOR,
        SPH = 1 << (int)GameScenes.SPH,
        Flight = 1 << (int)GameScenes.FLIGHT,
        TrackingStation = 1 << (int)GameScenes.TRACKSTATION,
        Settings = 1 << (int)GameScenes.SETTINGS,
        Credits = 1 << (int)GameScenes.CREDITS,

        AnyEditor = VAB | SPH, 
        AnyInitializing = 0xFFFF & ~(AnyEditor | Flight), 
        Any = 0xFFFF
    }

    public static class PartUtils
    {
        private static FieldInfo windowListField;

        /// <summary>
        /// Find the UIPartActionWindow for a part. Usually this is useful just to mark it as dirty.
        /// </summary>
        public static UIPartActionWindow FindActionWindow(this Part part)
        {
            // We need to do quite a bit of piss-farting about with reflection to 
            // dig the thing out.
            UIPartActionController controller = UIPartActionController.Instance;

            if (windowListField == null)
            {
                Type cntrType = typeof(UIPartActionController);
                foreach (FieldInfo info in cntrType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (info.FieldType == typeof(List<UIPartActionWindow>))
                    {
                        windowListField = info;
                        goto foundField;
                    }
                }
                Debug.LogWarning("*PartUtils* Unable to find UIPartActionWindow list");
                return null;
            }
        foundField:

            foreach (UIPartActionWindow window in (List<UIPartActionWindow>)windowListField.GetValue(controller))
                if (window.part == part)
                    return window;

            return null;
        }

        /// <summary>
        /// Find the relationship between two parts.
        /// </summary>
        public static PartRelationship RelationTo(this Part part, Part other)
        {
            if (other == null || part == null)
                return PartRelationship.Unknown;

            if (other == part)
                return PartRelationship.Self;
            if (part.localRoot != other.localRoot)
                return PartRelationship.Unrelated;
            if (part.parent == other)
                return PartRelationship.Child;
            if (other.parent == part)
                return PartRelationship.Parent;
            if (other.parent == part.parent)
                return PartRelationship.Sibling;
            for (Part tmp = part.parent; tmp != null; tmp = tmp.parent)
                if (tmp == other)
                    return PartRelationship.Decendent;
            for (Part tmp = other.parent; tmp != null; tmp = tmp.parent)
                if (tmp == part)
                    return PartRelationship.Ancestor;
            if(part.localRoot == other.localRoot)
                return PartRelationship.Vessel;
            return PartRelationship.Unrelated;
        }

        /// <summary>
        /// Test if two parts are related by a set of criteria. Because PartRelationship is a flags
        /// enumeration, multiple flags can be tested at the same time.
        /// </summary>
        public static bool RelationTest(this Part part, Part other, PartRelationship relation)
        {
            if (relation == PartRelationship.Unknown)
                return true;
            if(part == null || other == null)
                return false;

            if (TestFlag(relation, PartRelationship.Self) && part == other)
                return true;
            if (TestFlag(relation, PartRelationship.Vessel) && part.localRoot == other.localRoot)
                return true;
            if (TestFlag(relation, PartRelationship.Unrelated) && part.localRoot != other.localRoot)
                return true;
            if (TestFlag(relation, PartRelationship.Sibling) && part.parent == other.parent)
                return true;
            if (TestFlag(relation, PartRelationship.Ancestor))
            {
                for (Part upto = other.parent; upto != null; upto = upto.parent)
                    if (upto == part)
                        return true;
            }
            else if (TestFlag(relation, PartRelationship.Parent) && part == other.parent)
                return true;

            if (TestFlag(relation, PartRelationship.Decendent))
            {
                for (Part upto = part.parent; upto != null; upto = upto.parent)
                    if (upto == other)
                        return true;
            }
            else if (TestFlag(relation, PartRelationship.Child) && part.parent == other)
                return true;

            if (TestFlag(relation, PartRelationship.Symmetry))
                foreach (Part sym in other.symmetryCounterparts)
                    if (part == sym)
                        return true;
            return false;
        }

        internal static bool TestFlag(this PartRelationship e, PartRelationship flags)
        {
            return (e & flags) == flags;
        }

        /// <summary>
        /// Convert GameScene enum into GameSceneFilter
        /// </summary>
        public static GameSceneFilter AsFilter(this GameScenes scene)
        {
            return (GameSceneFilter)(1 << (int)scene);
        }

        /// <summary>
        /// True if the current game scene matches the filter.
        /// </summary>
        public static bool IsLoaded(this GameSceneFilter filter)
        {
            return (int)(filter & HighLogic.LoadedScene.AsFilter()) != 0;
        }
    }

    /// <summary>
    /// KSPAddon with equality checking using an additional type parameter. Fixes the issue where AddonLoader prevents multiple start-once addons with the same start scene.
    /// </summary>
    public class KSPAddonFixed : KSPAddon, IEquatable<KSPAddonFixed>
    {
        private readonly Type type;

        public KSPAddonFixed(KSPAddon.Startup startup, bool once, Type type)
            : base(startup, once)
        {
            this.type = type;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != this.GetType()) { return false; }
            return Equals((KSPAddonFixed)obj);
        }

        public bool Equals(KSPAddonFixed other)
        {
            if (this.once != other.once) { return false; }
            if (this.startup != other.startup) { return false; }
            if (this.type != other.type) { return false; }
            return true;
        }

        public override int GetHashCode()
        {
            return this.startup.GetHashCode() ^ this.once.GetHashCode() ^ this.type.GetHashCode();
        }
    }
}