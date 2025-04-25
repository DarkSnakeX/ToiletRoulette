using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace ToiletRoulette;

[HarmonyPatch(typeof(ToiletFun))]
static class ToiletFunPatch
{
    private static PlayerAvatar? lastPlayerWhoFlushed;
    private static float lastFlushTime = 0f;
    private static float flushInterval = 5f;

    
    [HarmonyPostfix, HarmonyPatch("FlushStartRPC")]
    private static void FlushStartRPC_Postfix(ToiletFun __instance)
    {
        if (Time.time - lastFlushTime > flushInterval)
        {
            ToiletRoulette.Logger.LogInfo("The toilet has been flushed. Good luck!");
            lastFlushTime = Time.time;

            foreach (PhysGrabObject physGrabObject in __instance.physGrabObjects)
            {
                if (physGrabObject != null)
                {
                    PlayerDeathHead component = physGrabObject.GetComponent<PlayerDeathHead>();
                    if (component != null)
                    {
                        ToiletRoulette.Logger.LogInfo($"The head has been detected: {component.name}");
                        if (Random.value < 0.5f)
                        {
                            DelayedRevive(component, __instance.transform.position + Vector3.up * 2);
                        }
                        else
                        {
                            DelayedKill();
                        }
                    }
                }
            }
        }
    }
    
    private static async void DelayedKill()
    {
        await Task.Delay(3000);
        KillPlayer();
    }

    private static async void DelayedRevive(PlayerDeathHead head, Vector3 revivePosition)
    {
        await Task.Delay(3000);
        ReviveSpecificPlayer(head, revivePosition);
    }

    [HarmonyPrefix, HarmonyPatch("Flush")]
    private static void Flush_Prefix(ToiletFun __instance)
    {
        // Almacena el jugador que interactuó con el inodoro
        lastPlayerWhoFlushed = FindNearestPlayer(__instance.transform.position);
        if (lastPlayerWhoFlushed != null)
        {
            lastPlayerWhoFlushed.ChatMessageSpeak("LET'S GO GAMBLING!",false);
            ToiletRoulette.Logger.LogInfo($"Player {lastPlayerWhoFlushed.name} interacted with the toilet.");
        }
        else
        {
            ToiletRoulette.Logger.LogWarning("Could not find the player who flushed the toilet.");
        }
    }

    private static void KillPlayer()
    {
        if (lastPlayerWhoFlushed != null)
        {
            lastPlayerWhoFlushed.ChatMessageSpeak("AW DANG IT!",false);
            lastPlayerWhoFlushed.PlayerDeath(-1);
            ToiletRoulette.Logger.LogInfo($"Player {lastPlayerWhoFlushed.name} killed for failing the revive.");
        }
        else
        {
            ToiletRoulette.Logger.LogWarning("Could not find the player who flushed the toilet.");
        }
    }
    
    

    private static void ReviveSpecificPlayer(PlayerDeathHead head, Vector3 revivePosition)
    {
        if (head.playerAvatar != null && (bool)AccessTools.Field(typeof(PlayerAvatar), "deadSet").GetValue(head.playerAvatar))
        {
            head.playerAvatar.Revive(false);
            head.playerAvatar.transform.position = revivePosition;
            if (lastPlayerWhoFlushed != null)
            {
                lastPlayerWhoFlushed.ChatMessageSpeak("I CANT STOP WINNING!",false);
            }
            ToiletRoulette.Logger.LogInfo($"Player {head.playerAvatar.name} revived.");
        }
        else
        {
            ToiletRoulette.Logger.LogWarning("The player is not dead or null. Cannot revive.");
        }
    }

    private static PlayerAvatar? FindNearestPlayer(Vector3 position)
    {
        PlayerAvatar[] allPlayers = GameObject.FindObjectsOfType<PlayerAvatar>();
        PlayerAvatar? nearestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (var player in allPlayers)
        {
            float distance = Vector3.Distance(position, player.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPlayer = player;
            }
        }

        return nearestPlayer;
    }
}