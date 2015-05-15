KSPAPIEL
================

A smaller set of utilities for plugin developers for Kerbal Space Program

Forked from KSPAPIExtensions by taniwha, swamp_ig, et al.

This add in is useful for providing some functions that make interacting with the KSP API functionally nicer and with an improved interface. 

# Utility classes

There are a number of utility classes available that do different functions. If you don't want to distribute the whole DLL with your plugin, you can just copy the appropriate source file into your project.

## Math Utils

The main feature of this is formatting of floats and doubles with SI prefixes. 

Examples:

````
	12.ToStringExt("S") -> "12"
	12.ToStringExt("S3") -> "12.0"
	120.ToStringExt("S3") -> "120"
	1254.ToStringExt("S3") -> "1250"  (4 digit numbers do not use k as a special case)
	12540.ToStringExt("S3") -> "1.25 k"  (using SI prefixes)
	12540.ToStringExt("S4") -> "1.254 k"  (more significant figures)
	(1.254).ToStringExt("S4+3") -> "1.254 k"  (+3 means the 'natural prefix' is k)
	(1.254).ToStringExt("S4-3") -> "1.254 m"  (-3 means the 'natural prefix' is m)
````

## Other utility classes

Utility methods to determine relationships between parts, plus some debugging code.

# Improvements to tweakables

Available is two extra tweakable controls, plus improvements to the stock tweakers. To use these you *must* include the KSIAPIUtils.dll in your project rather than just copying the code as there's an election process to ensure the latest version is being run. If backwards compatibility breaks, I will ensure that the user is warned to upgrade plugins.

## UI_ChooseOption

This allows the user to chose from a range of options. It's equivalent to a dropdown list only without the dropdown (dropdowns were difficult to do with the API).

Use it like this:

````c#
	[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Option"), 
		UI_ChooseOption(options=new string [] { "cheese", "pickles" })]
	public string toppingOption;
````

You can also have one set of options for the field, and one set to display if you use the display parameter.

Usually it's more appropriate to set the list of options at runtime:

````c#
	[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Tank Type"), UI_ChooseOption(scene=UI_Scene.Editor)]
	public string tankType;
	
	\\ ...
	
	public override void OnStart(PartModule.StartState state)
	{
            BaseField field = Fields["tankType"];
            UI_ChooseOption options = (UI_ChooseOption)field.uiControlEditor;

            options.options = new string [] { "cheese", "pickles" };
	}
````

## UI_FloatEdit

This is a much improved version of UI_FloatRange. 

You can select a float value with a set range. The value can be edited with optional large and small offsets, plus a slider to choose values between. Naturally SI formatting is available.

````c#
	[KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Top", guiFormat = "S4", guiUnits="m"),
	 UI_FloatEdit(scene = UI_Scene.Editor, minValue = 0.25f, incrementLarge = 1.25f, incrementSmall = 0.25f, incrementSlide = 0.001f)]
	public float topDiameter = 1.25f;
````

if incrementSmall is not set, then no button is visible in the control. If incrementLarge is not set then it has just a slider.

The slider is set to run between the smallest available increment, so in the above if the current value was 1.3, then the slider would run from 1.25 to 1.5.

