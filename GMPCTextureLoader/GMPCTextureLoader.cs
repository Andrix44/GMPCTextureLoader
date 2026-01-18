using GHPC;
using GHPC.State;
using GunnerModPC;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

[assembly: MelonInfo(typeof(GMPCTextureLoader), "Gunner, Mod, PC! Texture loader", "1.1.0", "Andrix")]
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
        static string handlerInstalledForScene;
        const string notInstalled = "TextureLoaderHandlerNotInstalled";

        readonly string folderPath = "Mods/GMPCTextureLoader/";
        readonly string imageExtension = ".png";

        public override void OnInitializeMelon()
        {
            config = MelonPreferences.CreateCategory("GMPCTextureLoaderConfig");
            reloadChangedTextures = config.CreateEntry<bool>("reloadChangedTextures", false);
            reloadChangedTextures.Description = "When this is enabled, the mod will hash the texture files to see if they changed when loading a new scene. This is useful when designing textures, but can slow down map loading when a lot of data has to be processed.";

            loaded = new Dictionary<string, ReplacedTexture>();
            handlerInstalledForScene = notInstalled;

            Directory.CreateDirectory(folderPath);
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (handlerInstalledForScene != notInstalled)
            {
                LoggerInstance.Msg($"Texture loader handler is already installed for scene {handlerInstalledForScene}, it must be unloaded first.");
            }
            else if (sceneName == "LOADER_INITIAL" || sceneName == "LOADER_MENU") return;
            else if (sceneName == "MainMenu2_Scene" || sceneName == "t64_menu" || sceneName == "MainMenu2-1_Scene")
            {
                LoggerInstance.Msg($"Patching textures in main menu {sceneName}.");
                MelonCoroutines.Start(LoadTextures(GameState.AppLoaded));
            }
            else
            {
                // Vehicles are loaded after OnSceneWasInitialized finishes, so we have to install an event to load the textures when everything is ready
                var status = StateController.RunOrDefer(GameState.GameInitialization, new GameStateEventHandler(LoadTextures));
                LoggerInstance.Msg($"Trying to add texture loader event handler on scene {sceneName}, result: {status}");
                if (status != GameStateInvocationStatus.Fail)
                {
                    handlerInstalledForScene = sceneName;
                }
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (handlerInstalledForScene != notInstalled)
            {
                LoggerInstance.Msg($"Texture loader handler is being unloaded together with scene {handlerInstalledForScene}, a new one can now be installed.");
                handlerInstalledForScene = notInstalled;
            }
        }
        
        IEnumerator LoadTextures(GameState state)
        {
            LoggerInstance.Msg($"GameState is {state}, loading texture replacements.");

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
                        // Only mark as loaded if it was uploaded to the GPU successfully
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
                        if (reloadChangedTextures.Value)
                        {
                            byte[] data = File.ReadAllBytes(filePath);
                            using (MD5 md5 = MD5.Create())
                            {
                                byte[] newHash = md5.ComputeHash(data);
                                byte[] oldHash = loaded[texName].hash;
                                if (!newHash.SequenceEqual(oldHash))
                                {
                                    LoggerInstance.Msg($"Hash for \"{texName}\" changed! Old hash {HashToString(oldHash)}, new hash: {HashToString(newHash)}. Loading new texture...");
                                    texture.LoadImage(data, true);
                                    loaded[texName] = new ReplacedTexture { hash = newHash, texture = texture, instances = new HashSet<int> { texture.GetInstanceID() } };
                                }
                                else if (replacedTexture.texture.GetInstanceID() != texture.GetInstanceID())
                                {
                                    LoggerInstance.Msg($"Copying already replaced texture \"{texName}\"...");
                                    Graphics.CopyTexture(replacedTexture.texture, texture);
                                }
                                else
                                {
                                    LoggerInstance.Msg($"Hash for \"{texName}\" is unchanged and replacement texture has already been loaded.");
                                }
                            }
                        }
                        else if (replacedTexture.texture.GetInstanceID() != texture.GetInstanceID())
                        {
                            LoggerInstance.Msg($"Copying already replaced texture \"{texName}\"...");
                            Graphics.CopyTexture(replacedTexture.texture, texture);
                        }
                        else
                        {
                            LoggerInstance.Msg($"Replacement texture for \"{texName}\" has already been loaded.");
                        }
                    }
                }
            }

            yield break;
        }

        private string HashToString(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", String.Empty);
        }
    }
}
