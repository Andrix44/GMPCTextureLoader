# Open Texture Loader

## Features
- This mod provides an easy way to replace textures in any Unity game that uses IL2CCP code and .png textures.
- A fork of [link](https://github.com/Andrix44/GMPCTextureLoader) that was only for Gunner, HEAT, PC!

## Installation
- Install MelonLoader: [link](https://github.com/LavaGang/MelonLoader.Installer/blob/master/README.md#how-to-install-re-install-or-update-melonloader).
- Download `OpenTextureLoader.dll` from the latest release and copy it to `<game folder>\Mods`.
- Run the mod once to generate it.
- Copy your `*.png` files into `<game folder>\Mods\TextureLoader`. They must have the same name as the `Texture2D` objects that they belong to. It's probably a good idea to keep the replacement textures the same dimensions as the originals.

## Extracting textures
There are multiple ways to do this, probably the easiest is to use ![UnityExplorer](https://github.com/sinai-dev/UnityExplorer) and save every file you need individually.
Go to `Object Explorer` -> `Object Search` -> set `Class filter` to `UnityEngine.Texture2D` -> set a name filter -> press `Search` -> select some texture -> in the new window click `View Texture` -> `Save .PNG`

![Saving textures in UnityExplorer](https://github.com/Andrix44/GMPCTextureLoader/assets/13806656/db5ca46e-560c-48b5-89c1-62184bb3336c)
