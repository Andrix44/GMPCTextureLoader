using MelonLoader;
using GunnerModPC;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System;

[assembly: MelonInfo(typeof(GMPCTextureLoader), "Gunner, Mod, PC! Texture loader", "1.1.1", "Andrix")]
[assembly: MelonPriority(101)]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace GunnerModPC
{
    struct ReplacedTexture
    {
        public byte[] hash;
        public Texture2D texture;
        public HashSet<int> instances;
    }

    public class GMPCTextureLoader : MelonMod
    {
        static MelonPreferences_Category config;
        static MelonPreferences_Entry<bool> reloadChangedTextures;

        static Dictionary<string, ReplacedTexture> loaded;

        readonly string folderPath = "Mods/GMPCTextureLoader/";
        readonly string imageExtension = ".png";

        public override void OnInitializeMelon()
        {
            config = MelonPreferences.CreateCategory("GMPCTextureLoaderConfig");
            reloadChangedTextures = config.CreateEntry<bool>("reloadChangedTextures", false);
            reloadChangedTextures.Description = "When this is enabled, the mod will hash the texture files to see if they changed when loading a new scene. This is useful when designing textures, but can slow down map loading when a lot of data has to be processed.";

            loaded = new Dictionary<string, ReplacedTexture>();

            Directory.CreateDirectory(folderPath);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            
            LoggerInstance.Msg($"Loading texture replacements for scene \"{sceneName}\".");

            HashSet<string> replacements = Directory.GetFiles(folderPath, "*" + imageExtension).Select(p => Path.GetFileNameWithoutExtension(p)).ToHashSet();
            LoggerInstance.Msg($"Found {replacements.Count} *{imageExtension} texture replacements in \"{folderPath}\".");

            Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            LoggerInstance.Msg($"Found {textures.Length} Texture2Ds.");
            for(int i = 0; i < textures.Length; ++i) {
                Texture2D texture = textures[i];
                string texName = texture.name;
                if (replacements.Contains(texName))
                {
                    string filePath = folderPath + texName + imageExtension;
                    if (!loaded.TryGetValue(texName, out ReplacedTexture replacedTexture))
                    {
                        LoggerInstance.Msg($"Replacement texture for \"{texName}\" has not yet been loaded, reading from file...");
                        byte[] data = File.ReadAllBytes(filePath);

                        // This takes a long time, but it only has to be done once during a run
                        if (!texture.LoadImage(data, true))
                        {
                            LoggerInstance.Error("Failed to upload replacement texture into the GPU memory!");
                        }
                        // Only mark as loaded if it was uploaded to the GPU succesfully
                        else
                        {
                            byte[] hash = { 0 };
                            if (reloadChangedTextures.Value)
                            {
                                using (MD5 md5 = MD5.Create())
                                {
                                    hash = md5.ComputeHash(data);
                                }
                                LoggerInstance.Msg($"Calculated hash for \"{texName}\": {HashToString(hash)}");
                            }

                            HashSet<int> instances = new HashSet<int>
                            {
                                texture.GetInstanceID()
                            };
                            loaded[texName] = new ReplacedTexture { hash = hash, texture = texture, instances = instances };
                        }
                    }
                    else
                    {
                        // Multiple textures can have the same name, so we have to check if this was already replaced or not
                        foreach (var item in replacedTexture.instances)
                        {
                            LoggerInstance.Msg(item);
                        }
                        if (replacedTexture.instances.Add(texture.GetInstanceID()))
                        {
                            LoggerInstance.Msg($"Copying already replaced texture \"{texName}\", as a new Texture2D with the same name but a different ID was found...");
                            Graphics.CopyTexture(replacedTexture.texture, texture);
                        }
                        else if (reloadChangedTextures.Value)
                        {
                            byte[] data = File.ReadAllBytes(filePath);
                            using (MD5 md5 = MD5.Create())
                            {
                                byte[] newHash = md5.ComputeHash(data);
                                ReplacedTexture loadedTexture = loaded[texName];
                                byte[] oldHash = loadedTexture.hash;
                                if (!newHash.SequenceEqual(oldHash))
                                {
                                    LoggerInstance.Msg($"Hash for \"{texName}\" changed! Old hash {HashToString(oldHash)}, new hash: {HashToString(newHash)}. Loading new texture...");
                                    textures[i].LoadImage(data);
                                    loadedTexture.hash = newHash;
                                    loaded[texName] = loadedTexture;
                                }
                                else
                                {
                                    LoggerInstance.Msg($"Hash for \"{texName}\" is unchanged. Skipping...");
                                }
                            }
                        }
                        else
                        {
                            LoggerInstance.Msg($"Replacement texture for \"{texName}\" already loaded and reloading is disabled, skipping...");
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
