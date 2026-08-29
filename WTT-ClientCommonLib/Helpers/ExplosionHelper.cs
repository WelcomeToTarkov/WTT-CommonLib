using System.Diagnostics.CodeAnalysis;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using JetBrains.Annotations;
using Systems.Effects;
using UnityEngine;
using WTTClientCommonLib.Components;

namespace WTTClientCommonLib.Helpers;

/// <summary>
///     Helper methods to create explosions using a fake grenade source
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ExplosionHelper
{
    /// <summary>
    ///     Create an explosion at a given position
    /// </summary>
    /// <param name="position">The position to create the explosion at</param>
    /// <param name="originalItem">The item source of the explosion</param>
    /// <param name="owner">The player owner of the explosion</param>
    public static void DetonateAt(Vector3 position, Item originalItem, IPlayer owner)
    {
        DetonateGrenadeAt(position, new FakeGrenade(3, 7, 90, 80), originalItem, owner);
    }
    
    /// <summary>
    ///     Create an explosion at a given position
    /// </summary>
    /// <param name="position">The position to create the explosion at</param>
    /// <param name="originalItem">The item source of the explosion</param>
    /// <param name="owner">The player owner of the explosion</param>
    /// <param name="minDist">Minimum explosion distance in meters(?)</param>
    /// <param name="maxDist">Maximum explosion distance in meters(?)</param>
    /// <param name="fragCount">The number of fragments the explosion should create</param>
    /// <param name="strength">The strength of the explosion</param>
    public static void DetonateAt(Vector3 position, Item originalItem, IPlayer owner, float minDist, float maxDist, int fragCount, float strength)
    {
        DetonateGrenadeAt(position, new FakeGrenade(minDist, maxDist, fragCount, strength), originalItem, owner);
    }

    /// <summary>
    ///     Detonate a fake grenade at a given position.
    /// </summary>
    /// <param name="position">The position to create the explosion at</param>
    /// <param name="grenade">The fake grenade to detonate</param>
    /// <param name="originalItem">The item source of the explosion</param>
    /// <param name="owner">The player owner of the explosion</param>
    public static void DetonateGrenadeAt(Vector3 position, FakeGrenade grenade, Item originalItem, IPlayer owner)
    {
        var explosiveAmmoComponent = new ExplosiveAmmoComponent(grenade);
        var ballisticsCalculator = Singleton<GameWorld>.Instance.SharedBallisticsCalculator as BallisticsCalculator;
        var shift = Vector3.up * 0.08f;

        Singleton<Effects>.Instance.EmitGrenade(grenade.ExplosionType, position, Vector3.up);
        
        Grenade.Explosion(
            null,
            explosiveAmmoComponent,
            position,
            owner.ProfileId,
            ballisticsCalculator,
            originalItem,
            shift
        );
    }
}