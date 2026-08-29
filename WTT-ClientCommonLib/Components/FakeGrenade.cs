using EFT.InventoryLogic;
using JetBrains.Annotations;
using UnityEngine;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace WTTClientCommonLib.Components;

[method: UsedImplicitly]
public class FakeGrenade(
    float fuzeArmTimeSec,
    float minDist,
    float maxDist,
    int fragmentsCount,
    string fragmentType,
    string explosionType,
    bool showHitEffectOnExplode,
    Vector3 armorDistanceDistanceDamage,
    Vector3 contusion,
    Vector3 blindness,
    bool grenadeComponentIsDummy)
    : IExplosiveAmmoTemplate
{
    public float FuzeArmTimeSec { get; } = fuzeArmTimeSec;
    public float MinExplosionDistance { get; } = minDist;
    public float MaxExplosionDistance { get; } = maxDist;
    public int FragmentsCount { get; } = fragmentsCount;
    public string FragmentType { get; } = fragmentType;
    public string ExplosionType { get; } = explosionType;
    public float ExplosionStrength { get; }
    public bool ShowHitEffectOnExplode { get; } = showHitEffectOnExplode;
    public Vector3 ArmorDistanceDistanceDamage { get; } = armorDistanceDistanceDamage;
    public Vector3 Contusion { get; } = contusion;
    public Vector3 Blindness { get; } = blindness;
    public bool GrenadeComponentIsDummy { get; } = grenadeComponentIsDummy;

    public FakeGrenade(float minDist, float maxDist, int fragmentsCount, float strength) : this(0.0f, minDist, maxDist,
        fragmentsCount, "63b35f281745dd52341e5da7", "Grenade_new", false, new Vector3(1.0f, 4.0f, 25.0f),
        new Vector3(1.5f, 4.0f, 15.0f), new Vector3(0.0f, 0.0f, 0.0f), false)
    {
        ExplosionStrength = strength;
    }

    public FakeGrenade(float minDist, float maxDist, int fragmentsCount, string fragmentType, string explosionType,
        float strength) : this(0.0f, minDist, maxDist, fragmentsCount, fragmentType, explosionType, false,
        new Vector3(1.0f, 4.0f, 25.0f), new Vector3(1.5f, 4.0f, 15.0f), new Vector3(0.0f, 0.0f, 0.0f), false)
    {
        ExplosionStrength = strength;
    }
}