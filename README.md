# GunnerModPC Texture loader

## Features
- This mod provides an easy way to replace textures in the game.
- In the config you can enable a setting that will update the textures when reloading a scene or loading a new one. This is useful when working on new textures.

![Sample texture](https://github.com/Andrix44/GMPCTextureLoader/assets/13806656/a7597b6b-1008-4e67-96cc-69c9aa2e1f46)

## Installation
- Install MelonLoader: [link](https://github.com/LavaGang/MelonLoader.Installer/blob/master/README.md#how-to-install-re-install-or-update-melonloader).
- Download `GMPCTextureLoader.dll` from the latest release ([link](https://github.com/Andrix44/GMPCTextureLoader/releases/latest)) and copy it to `\Gunner, HEAT, PC!\Bin\Mods`.
- Either create the `Gunner, HEAT, PC!\Bin\Mods\GMPCTextureLoader` folder manually, or run the mod once to generate it.
- Copy your `*.png` files into `Gunner, HEAT, PC!\Bin\Mods\GMPCTextureLoader`. They must have the same name as the `Texture2D` objects that they belong to. It's probably a good idea to keep the replacement textures the same dimensions as the originals.

## Extracting textures
There are multiple ways to do this, probably the easiest is to use ![CinematicUnityExplorer](https://github.com/originalnicodr/CinematicUnityExplorer/) and save every file you need individually.
Go to `Object Explorer` -> `Object Search` -> set `Class filter` to `UnityEngine.Texture2D` -> set a name filter -> press `Search` -> select some texture -> in the new window click `View Texture` -> `Save .PNG`

![Saving textures in UnityExplorer](https://github.com/Andrix44/GMPCTextureLoader/assets/13806656/db5ca46e-560c-48b5-89c1-62184bb3336c)

## Building
You can simply build the project with Visual Studio, but first you have to fix the references so that they point to your game DLLs.
