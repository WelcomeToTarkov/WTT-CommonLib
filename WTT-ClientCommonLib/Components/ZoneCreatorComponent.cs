using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.UI;
using UnityEngine;
using WTTClientCommonLib.Configuration;
using WTTClientCommonLib.Helpers;

namespace WTTClientCommonLib.Components;

public class ZoneCreatorComponent : MonoBehaviour
{
    private static Player Player;
    private static Camera Camera;
    private GameObject LookPositionGameObject;
    private bool isIncreaseKeyHeld;
    private bool isDecreaseKeyHeld;
    private string _cachedPath;
    private bool _prefabDirty;
    private bool _forceDefault;
    private bool _usingDefaultCube;

    public EInputMode Mode = EInputMode.Position;

    public enum EInputMode
    {
        Position,
        Scale,
        Rotate
    }

    private static readonly string[] PrefixesToSkip = new[]
    {
        "Default", "Base Human", "Root_Joint", "Player", "AICollider",
        "BornPositions", "BP.", "AITerrain", "TEMP_", "Slice"
    };

    protected ManualLogSource Logger { get; private set; }

    private ZoneCreatorComponent()
    {
        Logger ??= BepInEx.Logging.Logger.CreateLogSource(nameof(ZoneCreatorComponent));
    }

    internal static void Enable()
    {
        if (Singleton<IBotGame>.Instantiated)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            gameWorld.GetOrAddComponent<ZoneCreatorComponent>();
            Player = gameWorld.MainPlayer;
            Camera = Camera.main;
            Settings.CurrentZoneCubePosition.Value = Vector3.zero;
        }
    }

    internal void OnDisable()
    {
        if (!string.IsNullOrWhiteSpace(_cachedPath)) ZoneHelper.UnloadBundle(_cachedPath);
    }

    private void Start()
    {
        _cachedPath = GetPrefabPath();
        _prefabDirty = true;

        Settings.ZoneCubeTransparency.SettingChanged += (s, e) => SetTransparentColor(Settings.ZoneCubeTransparency.Value);
        Settings.ZoneCubePrefab.SettingChanged += (s, e) =>
        {
            var oldPath = _cachedPath;
            _cachedPath = GetPrefabPath();
            _prefabDirty = true;

            if (!string.IsNullOrWhiteSpace(oldPath)) ZoneHelper.UnloadBundle(oldPath);
            if (LookPositionGameObject)
            {
                Destroy(LookPositionGameObject);
                LookPositionGameObject = null;
            }
        };
    }

    private void Update()
    {
        if (!Player || !Camera) return;

        TransformSpeedCheck();

        if (Settings.IsKeyPressed(Settings.RemoveZoneCube.Value) && LookPositionGameObject)
        {
            Destroy(LookPositionGameObject);
            LookPositionGameObject = null;
            _usingDefaultCube = false;
            Settings.CurrentZoneCubePosition.Value = Vector3.zero;
            Settings.CurrentZoneCubeRotation.Value = Quaternion.identity;
            Settings.CurrentZoneCubeScale.Value = Vector3.zero;
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] Removed 'Zone Cube'.");
            return;
        }

        if (Settings.IsKeyPressed(Settings.ZonePrefabToggle.Value))
        {
            bool hasPrefab = !string.IsNullOrWhiteSpace(Settings.ZoneCubePrefab.Value);
            if (!hasPrefab)
            {
                NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] No bundle set.");
            }
            else
            {
                _forceDefault = !_forceDefault;

                if (LookPositionGameObject)
                {
                    var pos = LookPositionGameObject.transform.position;
                    var rot = LookPositionGameObject.transform.rotation;

                    Destroy(LookPositionGameObject);

                    bool usedDefault;
                    var instance = CreateZoneCubeInstance(!_forceDefault, out usedDefault);
                    _usingDefaultCube = usedDefault;

                    instance.transform.position = pos;
                    instance.transform.rotation = rot;
                    instance.transform.localScale = _usingDefaultCube ? Settings.DefaultScale.Value : Vector3.one;
                    instance.name = "Zone Cube";

                    LookPositionGameObject = instance;
                    Settings.CurrentZoneCubePosition.Value = pos;
                    Settings.CurrentZoneCubeRotation.Value = rot;
                    Settings.CurrentZoneCubeScale.Value = instance.transform.localScale;

                    if (_usingDefaultCube)
                    {
                        SetColor(Color.green);
                        SetTransparentColor(Settings.ZoneCubeTransparency.Value);
                    }

                    NotificationManagerClass.DisplayMessageNotification(
                        _usingDefaultCube
                            ? "[WTT-ClientCommonLib] Switched to default cube."
                            : "[WTT-ClientCommonLib] Switched to custom prefab."
                    );
                }
                else
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        _forceDefault
                            ? "[WTT-ClientCommonLib] Toggle: default cube selected."
                            : "[WTT-ClientCommonLib] Toggle: custom prefab selected."
                    );
                }
            }
        }

        if (Settings.IsKeyPressed(Settings.ZoneCube.Value))
        {
            Vector3 hitPoint = Vector3.zero;
            bool validHitFound = false;

            if (Settings.SpawnZoneCubeAtLookingPosition.Value)
            {
                validHitFound = ZoneHelper.TryGetLookHit(Camera, PrefixesToSkip, out hitPoint);
            }

            if (!validHitFound && Settings.SpawnZoneCubeAtLookingPosition.Value)
            {
                WTTClientCommonLib.Logger.LogError("[WTT-ClientCommonLib] No valid hit found to spawn the 'Zone Cube'.");
                return;
            }

            if (!LookPositionGameObject)
            {
                bool usedDefault;
                var instance = CreateZoneCubeInstance(useCustom: !_forceDefault, out usedDefault);
                _usingDefaultCube = usedDefault;

                Vector3 spawnPosition = Settings.SpawnZoneCubeAtLookingPosition.Value
                    ? hitPoint + (Player.Transform.position - hitPoint).normalized * 0.01f
                    : Camera.transform.position;

                instance.transform.position = spawnPosition;
                instance.transform.localScale = _usingDefaultCube ? Settings.DefaultScale.Value : Vector3.one;
                instance.name = "Zone Cube";

                LookPositionGameObject = instance;
                Settings.CurrentZoneCubePosition.Value = instance.transform.position;
                Settings.CurrentZoneCubeRotation.Value = Quaternion.Euler(0, Camera.transform.localRotation.eulerAngles.y, 0);
                instance.transform.rotation = Settings.CurrentZoneCubeRotation.Value;

                if (_usingDefaultCube)
                {
                    SetColor(Color.green);
                    SetTransparentColor(Settings.ZoneCubeTransparency.Value);
                    NotificationManagerClass.DisplayMessageNotification($"[WTT-ClientCommonLib] Default 'Zone Cube' created at {spawnPosition}");
                }
                else
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        $"[WTT-ClientCommonLib] 'Zone Cube' created from bundle '{Settings.ZoneCubePrefab.Value}' at {spawnPosition}");
                }
            }
            else
            {
                Vector3 movePosition = Settings.SpawnZoneCubeAtLookingPosition.Value
                    ? hitPoint + (Camera.transform.position - hitPoint).normalized * 0.01f
                    : Camera.transform.position;

                LookPositionGameObject.transform.position = movePosition;
                Settings.CurrentZoneCubePosition.Value = movePosition;

                float currentY = Camera.transform.localRotation.eulerAngles.y;
                Settings.CurrentZoneCubeRotation.Value = Quaternion.Euler(0, currentY, 0);
                LookPositionGameObject.transform.rotation = Settings.CurrentZoneCubeRotation.Value;

                NotificationManagerClass.DisplayMessageNotification($"[WTT-ClientCommonLib] Moved 'Zone Cube' to {movePosition}");
            }
        }

        if (!LookPositionGameObject) return;

        if (Settings.IsKeyPressed(Settings.PositionModeKey.Value)) ChangeMode(EInputMode.Position);
        if (Settings.IsKeyPressed(Settings.ScaleModeKey.Value)) ChangeMode(EInputMode.Scale);
        if (Settings.IsKeyPressed(Settings.RotateModeKey.Value)) ChangeMode(EInputMode.Rotate);

        float delta = Time.deltaTime;
        float speed = Settings.TransformSpeed.Value;

        switch (Mode)
        {
            case EInputMode.Position: HandlePosition(speed, delta); break;
            case EInputMode.Rotate: HandleRotation(speed, delta); break;
            case EInputMode.Scale: HandleScaling(speed, delta); break;
        }

        if (Settings.IsKeyPressed(Settings.AddMapLocationToListKey.Value)) AddCubeData();

        if (Settings.IsKeyPressed(Settings.RemoveMapLocationFromListKey.Value) && Settings.CubeDataList.Count > 0)
        {
            Settings.CubeDataList.RemoveAt(Settings.CubeDataList.Count - 1);
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] Removed last Map Position from the list.");
        }
    }

    private string GetPrefabPath() => ZoneHelper.GetPrefabPath(Settings.ZoneCubePrefab.Value);

    private GameObject LoadPrefabFromBundle(string bundlePath) => ZoneHelper.TryLoadFirstPrefab(bundlePath);

    private void TransformSpeedCheck()
    {
        if (Settings.IsKeyPressed(Settings.IncreaseTransformSpeed.Value, true) && LookPositionGameObject)
        {
            isIncreaseKeyHeld = true;
            Settings.TransformSpeed.Value = Mathf.Clamp(Mathf.Round((Settings.TransformSpeed.Value + 0.1f) * 100f) / 100f, 0.1f, 10f);
        }

        if (isIncreaseKeyHeld && Settings.IsKeyReleased(Settings.IncreaseTransformSpeed.Value))
        {
            isIncreaseKeyHeld = false;
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] Transform Speed Increased to " + Settings.TransformSpeed.Value);
        }

        if (Settings.IsKeyPressed(Settings.DecreaseTransformSpeed.Value, true) && LookPositionGameObject)
        {
            isDecreaseKeyHeld = true;
            Settings.TransformSpeed.Value = Mathf.Clamp(Mathf.Round((Settings.TransformSpeed.Value - 0.1f) * 100f) / 100f, 0.1f, 10f);
        }

        if (isDecreaseKeyHeld && Settings.IsKeyReleased(Settings.DecreaseTransformSpeed.Value))
        {
            isDecreaseKeyHeld = false;
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] Transform Speed Decreased to " + Settings.TransformSpeed.Value);
        }
    }

    public void ChangeMode(EInputMode _mode)
    {
        if (Settings.PositionModeKey.Value.IsDown())
        {
            Mode = EInputMode.Position;
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuInstallModFunc);
            if (_usingDefaultCube) SetColor(Color.green);
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] Translation Mode Activated.");
        }

        if (Settings.ScaleModeKey.Value.IsDown())
        {
            Mode = EInputMode.Scale;
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuInstallModGear);
            if (_usingDefaultCube) SetColor(Color.blue);
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] Scaling Mode Activated.");
        }

        if (Settings.RotateModeKey.Value.IsDown())
        {
            Mode = EInputMode.Rotate;
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.MenuInstallModVital);
            if (_usingDefaultCube) SetColor(Color.red);
            NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] Rotation Mode Activated.");
        }
    }

    public void HandlePosition(float speed, float delta)
    {
        if (Settings.NegativeXKey.Value.IsPressed()) MoveLP("x", -(speed * delta));
        else if (Settings.PositiveXKey.Value.IsPressed()) MoveLP("x", speed * delta);

        if (Settings.NegativeYKey.Value.IsPressed()) MoveLP("y", -(speed * delta));
        else if (Settings.PositiveYKey.Value.IsPressed()) MoveLP("y", speed * delta);

        if (Settings.NegativeZKey.Value.IsPressed()) MoveLP("z", -(speed * delta));
        else if (Settings.PositiveZKey.Value.IsPressed()) MoveLP("z", speed * delta);

        if (Settings.CurrentZoneCubePosition.Value != LookPositionGameObject.transform.position)
            Settings.CurrentZoneCubePosition.Value = LookPositionGameObject.transform.position;
    }

    public void HandleRotation(float speed, float delta)
    {
        float rotSpeed = speed * 25;

        if (Settings.PositiveXKey.Value.IsPressed()) RotateLP("x", -rotSpeed * delta);
        else if (Settings.NegativeXKey.Value.IsPressed()) RotateLP("x", rotSpeed * delta);

        if (Settings.PositiveYKey.Value.IsPressed()) RotateLP("y", -rotSpeed * delta);
        else if (Settings.NegativeYKey.Value.IsPressed()) RotateLP("y", rotSpeed * delta);

        if (Settings.PositiveZKey.Value.IsPressed()) RotateLP("z", -rotSpeed * delta);
        else if (Settings.NegativeZKey.Value.IsPressed()) RotateLP("z", rotSpeed * delta);

        if (Settings.CurrentZoneCubeRotation.Value != LookPositionGameObject.transform.rotation)
            LookPositionGameObject.transform.rotation = Settings.CurrentZoneCubeRotation.Value;
    }

    public void HandleScaling(float speed, float delta)
    {
        if (Settings.NegativeXKey.Value.IsPressed()) ScaleLP("x", -(speed * delta));
        else if (Settings.PositiveXKey.Value.IsPressed()) ScaleLP("x", speed * delta);

        if (Settings.NegativeYKey.Value.IsPressed()) ScaleLP("y", -(speed * delta));
        else if (Settings.PositiveYKey.Value.IsPressed()) ScaleLP("y", speed * delta);

        if (Settings.NegativeZKey.Value.IsPressed()) ScaleLP("z", -(speed * delta));
        else if (Settings.PositiveZKey.Value.IsPressed()) ScaleLP("z", speed * delta);

        if (Settings.CurrentZoneCubeScale.Value != LookPositionGameObject.transform.localScale)
            Settings.CurrentZoneCubeScale.Value = LookPositionGameObject.transform.localScale;
    }

    public void MoveLP(string axis, float amount)
    {
        Vector3 translation = axis switch
        {
            "x" => new Vector3(amount, 0, 0),
            "y" => new Vector3(0, amount, 0),
            "z" => new Vector3(0, 0, amount),
            _ => Vector3.zero
        };
        LookPositionGameObject.transform.Translate(translation, Space.Self);
    }

    public void ScaleLP(string axis, float amount)
    {
        Vector3 scaleAmount = axis switch
        {
            "x" => new Vector3(amount, 0, 0),
            "y" => new Vector3(0, amount, 0),
            "z" => new Vector3(0, 0, amount),
            _ => Vector3.zero
        };

        Vector3 newScale = LookPositionGameObject.gameObject.transform.localScale + scaleAmount;
        LookPositionGameObject.gameObject.transform.localScale = new Vector3(
            Mathf.Max(newScale.x, 0.01f),
            Mathf.Max(newScale.y, 0.01f),
            Mathf.Max(newScale.z, 0.01f)
        );
    }

    public void RotateLP(string axis, float amount)
    {
        Vector3 rotation = axis switch
        {
            "x" => new Vector3(0, amount, 0),
            "y" => new Vector3(0, 0, amount),
            "z" => new Vector3(amount, 0, 0),
            _ => Vector3.zero
        };

        if (axis == "x")
        {
            Settings.CurrentZoneCubeRotation.Value *= Quaternion.Euler(rotation);
            LookPositionGameObject.transform.localRotation = Settings.CurrentZoneCubeRotation.Value;
        }
        else if (!Settings.LockXAndZRotation.Value)
        {
            Settings.CurrentZoneCubeRotation.Value *= Quaternion.Euler(rotation);
            LookPositionGameObject.transform.localRotation = Settings.CurrentZoneCubeRotation.Value;
        }
    }

    public void SetColor(Color color)
    {
        if (!LookPositionGameObject) return;
        LookPositionGameObject.GetComponent<Renderer>().material.color = new Color(color.r, color.g, color.b);
        SetTransparentColor(Settings.ZoneCubeTransparency.Value);
    }

    public void SetTransparentColor(float transparency)
    {
        if (!LookPositionGameObject) return;
        var r = LookPositionGameObject.GetComponent<Renderer>();
        ZoneHelper.SetAlpha(r, transparency);
    }

    public static void AddCubeData()
    {
        var position = Settings.CurrentZoneCubePosition.Value;
        var rotation = Settings.CurrentZoneCubeRotation.Value.eulerAngles;

        var location = new Settings.Location
        {
            Position = position,
            Rotation = rotation
        };

        Settings.CubeDataList.Add(location);
        NotificationManagerClass.DisplayMessageNotification("[WTT-ClientCommonLib] Zone Cube Location added to the list.");
    }

    private GameObject CreateZoneCubeInstance(bool useCustom, out bool usedDefault)
    {
        usedDefault = !useCustom || string.IsNullOrWhiteSpace(Settings.ZoneCubePrefab.Value);
        if (usedDefault)
            return ZoneHelper.CreateTransparentCube(Settings.ZoneName.Value);

        var path = GetPrefabPath();
        var prefab = LoadPrefabFromBundle(path);
        if (prefab != null)
        {
            var go = Instantiate(prefab);
            ZoneHelper.DestroyAllColliders(go);
            return go;
        }

        WTTClientCommonLib.Logger.LogWarning("[WTT-ClientCommonLib] Bundle load failed, using default cube.");
        usedDefault = true;
        return ZoneHelper.CreateTransparentCube(Settings.ZoneName.Value);
    }
}