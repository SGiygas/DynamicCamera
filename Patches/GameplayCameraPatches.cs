using HarmonyLib;
using UnityEngine;

namespace DynamicCamera.Patches;

[HarmonyPatch(typeof(Reptile.GameplayCamera), "Awake")]
public class GameplayCameraAwakePatch
{
    public static void Postfix(Camera ___cam)
    {
        Plugin.Instance.Initialize(___cam.fieldOfView);
    }
}

[HarmonyPatch(typeof(Reptile.GameplayCamera), nameof(Reptile.GameplayCamera.UpdateCamera))]
public class GameplayCameraUpdatePatch
{
    public static void Postfix(Reptile.GameplayCamera __instance,
                               Camera ___cam,
                               Transform ___realTf,
                               Reptile.Player ___player,
                               Reptile.CameraMode ___cameraMode,
                               Reptile.CameraMode ___cameraModePrev,
                               float ___cameraModeFade)
    {
        Plugin plugin = Plugin.Instance;

        float speed = plugin.UseTotalSpeed.Value ? ___player.GetTotalSpeed() : ___player.GetForwardSpeed();
        float speedNormalized = speed / plugin.MaxSpeed;
        ___cam.fieldOfView = plugin.UpdateFOV(speedNormalized);

        if (plugin.EnableDolly.Value)
        {
            Vector3 lookAtPosition;

            if (___cameraModePrev != null)
            {
                float t = FadeOutCubic(___cameraModeFade);
                lookAtPosition = Vector3.Lerp(___cameraModePrev.lookAtPos, ___cameraMode.lookAtPos, t);
            }
            else
            {
                lookAtPosition = ___cameraMode.lookAtPos;
            }

            // Direction from A to B = B - A
            Vector3 viewVector = ___realTf.position - lookAtPosition;
            float distanceToCharacter = viewVector.magnitude;
            // Normalized view direction
            Vector3 viewDirection = viewVector / distanceToCharacter;

            Vector3 newDirection = viewDirection * plugin.UpdateDistance(distanceToCharacter);

            ___realTf.position = lookAtPosition + newDirection;
        }
    }

    private static float FadeOutCubic(float f)
    {
        f = 1f - f;
        return 1.0f - f * f * f;
    }
}
