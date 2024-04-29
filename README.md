# GunnerModPC Texture loader

## Features
- This mod provides an easy way to replace textures in the game.
- Copy your *.png files into `Gunner, HEAT, PC!\Bin\Mods\GMPCTextureLoader`. They must have the same name as the `Texture2D` objects that they belong to.
- It's probably a good idea to keep the replacement textures the same size as the originals.
- In the config you can enable a setting that will update the textures when reloading a scene or loading a new one. This is useful when working on new textures.

## Installation
- Install MelonLoader: [link](https://github.com/LavaGang/MelonLoader.Installer/blob/master/README.md#how-to-install-re-install-or-update-melonloader).
- Download `GMPCTextureLoader.dll` from the latest release ([link](https://github.com/Andrix44/GunnerModPCTextureLoader/releases/latest)) and copy it to `\Gunner, HEAT, PC!\Bin\Mods`.

![Sample texture]()

## Extracting textures
There are multiple ways to do this, probably the easiest is to use ![UnityExplorer](https://github.com/sinai-dev/UnityExplorer) and save every file you need individually.
Go to `Object Explorer` -> `Object Search` -> set `Class filter` to `UnityEngine.Texture2D` -> set a name filter -> press `Search` -> in the new window click `View Texture` -> `Save .PNG`

![Saving textures in UnityExplorer]()

## Building
You can simply build the project with Visual Studio, but first you have to fix the references so that they point to your game DLLs.
