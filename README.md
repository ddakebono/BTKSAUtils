# BTKSAUtils
BTK Standalone: Utils is a collection of smaller mods and tweaks that I've had kicking around!

This mod replaces NameplateMod since the changes to the CVR nameplate kinda gutted it, I've implemented some of the left over features here!

This mod requires [BTKUILib](https://github.com/BTK-Development/BTKUILib) or it will not work at all.

The main features of this mod are:
 - Hide nameplates on friends only
 - Toggle nameplate visibility on a per user basis
 - Access fade start and end settings for the new CVR nameplate shader
 - Gesture Parameter Driver, this allows you to use the CVR Gesture Recognizer to control avatar parameters
 - Alternate Advanced Avatars tab, this is a UILib based advanced avatars tab

## Feature explanation

### Alt Advanced Avatars
This is a replacement for CVR's built in advanced avatar page, it's built using BTKUILib's UI so it's a bit easier to navigate. Some types that are in the normal advanced avatar page won't appear here, such as joystick parameters.

This page supports the following types:
 - GameObject Toggle
 - GameObject Dropdown
 - Slider
 - Input Single

This tab is off by default, but you can toggle it from the Bono's Utils settings

### Gesture Param Driver
This feature takes advantage of the CVR Gesture Recognizer, the same system that handles the gesture based camera thing, like enabling/resizing and taking a picture.

This system is a bit messy to use, but can let you do some silly stuff with the gestures, currently it supports driving slider parameters and gameobject toggles.

To use it in slider mode you need to set the Gesture Type to Hold and set the Gesture Direction to anything other then Static, for GameObject Toggles you'll want to set the Gesture Type to OneShot.
Once you've made your choice you can set it to drive whatever parameter you want, you'll probably need to experiment to see what works and what doesn't!

## Install
Install [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) version 0.6.1 or higher, this is required as the mod has been updated specifically for 0.6.1!

Download the latest release from [Releases](https://github.com/ddakebono/BTKSAUtils/releases) and place in your ChilloutVR/Mods folder!

## Disclamer
### BTKSAUtils is not made by or affiliated with Alpha Blend Interactive

## Credits
* [HerpDerpinstine/MelonLoader](https://github.com/HerpDerpinstine/MelonLoader)
* [ChilloutVR](https://store.steampowered.com/app/661130/ChilloutVR/)
