using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Reptile;
using UnityEngine;

namespace DynamicCamera;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; }

    public float DefaultFOV { get; private set; }
    public float MaxSpeed { get; private set; }

    public ConfigEntry<bool> UseTotalSpeed { get; private set; }
    public ConfigEntry<bool> EnableDolly { get; private set; }
    public ConfigEntry<float> MaxFov { get; private set; }
    public ConfigEntry<float> MaxSpeedKmh { get; private set; }
    public ConfigEntry<float> AdjustmentSpeed { get; private set; }

    private float _currentFOV;
    private float _currentFOVT;
    private float _fovTangent;

    private readonly AnimationCurve _easeIn = new(
        new Keyframe(0.0f, 0.0f, 0.0f, 0.0f),
        new Keyframe(1.0f, 1.0f, 2.0f, 0.0f)
    );

    private void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        EnableDolly = Config.Bind("Settings", "Enable Dolly Zoom Effect", true, "Makes the FOV change while the visual position of the camera doesn't.");
        MaxFov = Config.Bind("Settings", "Maximum FOV", 80.0f);
        AdjustmentSpeed = Config.Bind("Settings", "Adjustment Speed", 3.5f, "How fast the FOV adjusts in degrees per second.");
        UseTotalSpeed = Config.Bind("Settings", "Use Total Speed", true, "Whether to use the total speed or only the lateral (horizontal) speed.");
        MaxSpeedKmh = Config.Bind("Settings", "Maximum Speed (km/h)", 60.0f);

        MaxSpeed = MaxSpeedKmh.Value / 3.6f;

        Instance = this;

        var patches = new Harmony("sgiygas.fastCamera");
        patches.PatchAll();
    }

    public void Initialize(float defaultFOV)
    {
        DefaultFOV = defaultFOV;
        _currentFOV = defaultFOV;
        _currentFOVT = 0.0f;

        if (EnableDolly.Value)
        {
            _fovTangent = Mathf.Tan(defaultFOV * 0.5f * Mathf.Deg2Rad);
        }
    }

    public float UpdateFOV(float playerSpeedNormalized)
    {
        float adjustmentAmount = AdjustmentSpeed.Value * Core.dt;

        if (_currentFOVT < playerSpeedNormalized)
        {
            _currentFOVT += adjustmentAmount;
            _currentFOVT = Mathf.Min(_currentFOVT, playerSpeedNormalized);
        }
        else if (_currentFOVT > playerSpeedNormalized)
        {
            _currentFOVT -= adjustmentAmount;
            _currentFOVT = Mathf.Max(_currentFOVT, playerSpeedNormalized);
        }

        float t = _easeIn.Evaluate(_currentFOVT);
        _currentFOV = Mathf.Lerp(DefaultFOV, MaxFov.Value, t);

        return _currentFOV;
    }

    public float UpdateDistance(float targetDistance)
    {
        return GetDistanceRelativeToFOV(targetDistance, _currentFOV);
    }

    private float GetDistanceRelativeToFOV(float targetDistance, float fieldOfView)
    {
        float frustumHeight = 2.0f * targetDistance * _fovTangent;
        float adjustedDistance = frustumHeight / (2.0f * Mathf.Tan(0.5f * fieldOfView * Mathf.Deg2Rad));
        return adjustedDistance;
    }
}
