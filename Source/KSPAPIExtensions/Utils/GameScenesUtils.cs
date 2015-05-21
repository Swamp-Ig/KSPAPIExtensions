using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


// ReSharper disable once CheckNamespace
namespace KSPAPIExtensions
{
    /// <summary>
    /// Flags to filter particular game scenes.
    /// </summary>
    [Flags]
    public enum GameSceneFilter
    {
        Loading = 1 << GameScenes.LOADING,
        MainMenu = 1 << GameScenes.MAINMENU,
        SpaceCenter = 1 << GameScenes.SPACECENTER,
        VAB = 1 << GameScenes.EDITOR,
        SPH = 1 << GameScenes.EDITOR,
        Flight = 1 << GameScenes.FLIGHT,
        TrackingStation = 1 << GameScenes.TRACKSTATION,
        Settings = 1 << GameScenes.SETTINGS,
        Credits = 1 << GameScenes.CREDITS,

        AnyEditor = VAB | SPH, 
        AnyEditorOrFlight = AnyEditor | Flight,
        AnyInitializing = 0xFFFF & ~(AnyEditor | Flight), 
        Any = 0xFFFF
    }

    public static class GameScenesUtils
    {
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
}
