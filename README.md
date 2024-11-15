# Open Texture Loader

## Features
- This mod provides an easy way to replace textures in any Unity game that uses IL2CCP code and .png textures.
- A fork of [GMPCTextureLoader](https://github.com/Andrix44/GMPCTextureLoader) by [Andrix44](https://github.com/Andrix44) that was only for Gunner, HEAT, PC!

## Installation
- Install MelonLoader: [link](https://melonwiki.xyz/#/README?id=automated-installation).
- Download `OpenTextureLoader.dll` from the latest release and copy it to `<game folder>\Mods`.
- Run the game with the mod once to generate the needed folders.
- Copy your `*.png` files into `<game folder>\Mods\TextureLoader`. They must have the same name as the `Texture2D` objects that they belong to. It's probably a good idea to keep the replacement textures the same dimensions as the originals.

## Extracting textures
There are multiple ways to do this, I have found [AssetRipper](https://github.com/AssetRipper/AssetRipper) to be the easyest for extracting all the textures at once.
