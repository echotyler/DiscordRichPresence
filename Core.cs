using MelonLoader;
using Discord;
using UnityEngine;
using HarmonyLib;
using HutongGames.PlayMaker;
using UnityEngine.SocialPlatforms;

[assembly: MelonInfo(typeof(DiscordRichPresence.Init), "DiscordRichPresence", "1.0.0", "EchoTyler", null)]
[assembly: MelonGame("Slope Plus", "Slope Plus")]

namespace DiscordRichPresence
{
    public class Init : MelonMod
    {
        static bool customGame = false;
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg("Loaded Discord Rich Presence Mod");
            GameObject.Find("EventManager").AddComponent<DiscordController>();
            HarmonyLib.Harmony harmony = new("DiscordRichPresence");
            harmony.Patch(
                AccessTools.Method(typeof(GuiController), "HandlePlay_Main"), null,
                new HarmonyMethod(AccessTools.Method(typeof(Init), "HandlePlay_Main"))
            );
            harmony.Patch(
                AccessTools.Method(typeof(GuiController), "HandlePlay_Custom"), null,
                new HarmonyMethod(AccessTools.Method(typeof(Init), "HandlePlay_Custom"))
            );
            harmony.Patch(
                AccessTools.Method(typeof(scorePoint), "OnTriggerEnter"), null,
                new HarmonyMethod(AccessTools.Method(typeof(Init), "AlterGameStatus"))
            );
            harmony.Patch(
                AccessTools.Method(typeof(GuiController), "Handle_ShowGameOver"), new HarmonyMethod(AccessTools.Method(typeof(Init), "Handle_ShowGameOver"))
            );
            if (DiscordController.current.discord == null)
            {
                DiscordController.current.discord = new(1412340914931761263, (ulong)CreateFlags.NoRequireDiscord);
            }
        }

        public static void AlterGameStatus()
        {
            if (customGame)
            {
                DiscordController.current.UpdateStatus($"Playing a normal game", $"Score: {FsmVariables.GlobalVariables.GetFsmInt("Score").Value}", "slope_icon", "Logo");
            }
            else
            {
                DiscordController.current.UpdateStatus($"Playing a custom game", $"Score: {FsmVariables.GlobalVariables.GetFsmInt("Score").Value}", "slope_icon", "Logo");
            }
        }

        public static void HandlePlay_Main()
        {
            DiscordController.current.ResetTime();
            customGame = false;
            DiscordController.current.UpdateStatus($"Playing a normal game", $"Score: {FsmVariables.GlobalVariables.GetFsmInt("Score").Value}", "slope_icon", "Logo");
        }

        public static void HandlePlay_Custom()
        {
            DiscordController.current.ResetTime();
            customGame = true;
            DiscordController.current.UpdateStatus($"Playing a custom game", $"Current Score: {FsmVariables.GlobalVariables.GetFsmInt("Score").Value}", "slope_icon", "Logo");
        }

        public static void Handle_ShowGameOver()
        {
            DiscordController.current.ResetTime();
            customGame = false;
            DiscordController.current.UpdateStatus("In the menu.", "", "slope_icon", "Logo");
        }
    }
    public class DiscordController : MonoBehaviour
    {
        public Discord.Discord discord;
        public void Awake()
        {
            if (current == null)
            {
                current = this;
                return;
            }
        }

        public void Start()
        {
            discord = new(1412340914931761263, (UInt64)CreateFlags.Default);
            time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            UpdateStatus("In the menu.", "", "slope_icon", "Logo");
        }

        public void UpdateStatus(string details = "", string state = "", string largeImage = "", string largeText = "")
        {
            MelonLogger.Msg("Attempting to update status.");
            try
            {

                if (discord == null)
                {
                    discord = new(1412340914931761263, (ulong)CreateFlags.NoRequireDiscord);
                }
                ActivityManager activityManager = discord.GetActivityManager();
                Activity activity = default;
                activity.Details = details;
                activity.State = state;
                activity.Assets.LargeImage = largeImage;
                activity.Assets.LargeText = largeText;
                activity.Timestamps.Start = time;
                Activity activity2 = activity;
                activityManager.UpdateActivity(activity2, delegate (Result res)
                {
                    if (res != Result.Ok)
                    {
                        activityManager.ClearActivity((result) => { });
                        MelonLogger.Error("Failed to connect to Discord.");
                    }
                });
            }
            catch
            {
                MelonLogger.Error("Failed to update activity.");
                Destroy(this);
            }
        }

        public void OnApplicationQuit()
        {
            discord.Dispose();
        }

        public void ResetTime()
        {
            time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public void Update()
        {
            try
            {
                if (discord == null)
                {
                    discord = new(1412340914931761263, (ulong)CreateFlags.NoRequireDiscord);
                }
                discord.RunCallbacks();
            }
            catch (Exception ex) {
                MelonLogger.Error($"Failed to run callbacks: {ex}");
            }
        }

        public static DiscordController current;

        private long time;
    }
}