using MelonLoader;
using GunnerModPC;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Security.Policy;
using MelonLoader.ICSharpCode.SharpZipLib.Checksum;
using System;

[assembly: MelonInfo(typeof(GMPCTextureLoader), "Gunner, Mod, PC! Texture loader", "1.0.0", "Andrix")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace GunnerModPC
{
    public class GMPCTextureLoader : MelonMod
    {
        public static MelonPreferences_Category config;
        public static MelonPreferences_Entry<bool> reloadChangedTextures;

        public static Dictionary<string, byte[]> loaded;

        public override void OnInitializeMelon()
        {
            config = MelonPreferences.CreateCategory("GMPCTextureLoaderConfig");
            reloadChangedTextures = config.CreateEntry<bool>("reloadChangedTextures", false);
            reloadChangedTextures.Description = "When this is enabled, the mod will hash the texture files to see if they changed when loading a new scene. This is useful when designing textures, but can slow down map loading when a lot of data has to be processed.";

            loaded = new Dictionary<string, byte[]>();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            string path = "Mods/GMPCTextureLoader/";
            string extension = ".png";
            LoggerInstance.Msg($"Loading texture replacements for scene \"{sceneName}\".");

            HashSet<string> replacements = Directory.GetFiles(path, "*" + extension).Select(p => Path.GetFileNameWithoutExtension(p)).ToHashSet();
            LoggerInstance.Msg($"Found {replacements.Count} *{extension} texture replacements in \"{path}\".");

            Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            LoggerInstance.Msg($"Found {textures.Length} Texture2Ds.");
            for(int i = 0; i < textures.Length; ++i) {
                string texName = textures[i].name;
                if (replacements.Contains(texName))
                {
                    string filePath = path + texName + extension;
                    if (!loaded.ContainsKey(texName))
                    {
                        LoggerInstance.Msg($"Replacement texture for {texName} has not yet been loaded, reading from file...");
                        byte[] data = File.ReadAllBytes(filePath);

                        // This takes a long time, but it only has to be done once during a run
                        if (!textures[i].LoadImage(data, true)) {
                            LoggerInstance.Error("Failed to upload replacement texture into the GPU memory!");
                        }

                        byte[] hash = { 0 };
                        if (reloadChangedTextures.Value)
                        {
                            using (MD5 md5 = MD5.Create())
                            {
                                hash = md5.ComputeHash(data);
                            }
                            LoggerInstance.Msg($"Calculated hash for {texName}: {HashToString(hash)}");
                        }
                        
                        loaded[texName] = hash;
                    }
                    else
                    {
                        if (reloadChangedTextures.Value)
                        {
                            byte[] data = File.ReadAllBytes(filePath);
                            using (MD5 md5 = MD5.Create())
                            {
                                byte[] newHash = md5.ComputeHash(data);
                                byte[] oldHash = loaded[texName];
                                if (!newHash.SequenceEqual(oldHash))
                                {
                                    LoggerInstance.Msg($"Hash for {texName} changed! Old hash {HashToString(oldHash)}, new hash: {HashToString(newHash)}. Loading new texture...");
                                    textures[i].LoadImage(data);
                                    loaded[texName] = newHash;
                                }
                                else
                                {
                                    LoggerInstance.Msg($"Hash for {texName} is unchanged. Skipping...");
                                }
                            }
                        }
                        else
                        {
                            LoggerInstance.Msg($"Replacement texture for {texName} already loaded and reloading is disabled, skipping...");
                        }
                    }
                }
            }
        }

        private string HashToString(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", String.Empty);
        }
    }
}
