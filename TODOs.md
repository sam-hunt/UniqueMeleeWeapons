# TODOs

# Features

- Brainstorm incendiary traits etc (same shape as the tox coating: a Flame/Burn DamageDef via MeleeOnHitEffect_ExtraDamage)
- Ability traits (aoe shock stun? cleave attack?)
- Wielder-hediff trait (`equippedHediffs`) for an equip buff/curse
- Investigate VWE integration (akimbo etc?)
- Mod settings to add each weapon to rewards pools
- Mod setting to remove wood stuff from pools for generated weapons
- Mercenary camp quest?
- About.xml
- Test Blood-soaked moodlets and nullifications, PanicFlee-on-hit vs humanlikes
- Tweak blood-soaked color to be a little more washed?
- Test EMP trait
- Consider concussive balance vs paralytic arrows

- `pyrophoric` (Bladed, coating-family or colour-two): `MeleeOnHitEffect_ExtraDamage` w/ Core `Flame`/`Burn` (Odyssey `IncendiaryRounds` analog).

# Trait spread (vanilla-like coverage)

Rubric derived from Odyssey's 42-trait spread (see Docs/Odyssey-Ranged-Trait-Catalog.md). A lens for
spotting what's missing, not hard quotas:

- Target mix ≈ 60% stat / 15% on-hit effect / 15% active ability / 10% cosmetic. We are currently
  on-hit- and cosmetic-HEAVY (≈50% of traits proc on hit) with ZERO abilities — so priorities are
  (1) ability traits and (2) clean/standalone stat traits; hold off adding more on-hit status riders.
- Variety reads at the whole-mod level, not per category — single-trait categories are fine (Odyssey
  ships several: Rifle/Shotgun/BeamWeapon/LowStoppingPower). Grow a category only when a genuinely
  distinct, meaningful archetype earns it (e.g. Bladed is two bleeds — a non-bleed option would add
  real variety; redundant filler would not — Bladed now spans two AP traits (Razored staple + Monomolecular)
  plus the Serrated bleed, so a non-AP, non-bleed edge would be the variety add).
- ~1/3 of stat traits should carry a downside (tradeoff or drawback) for roll variance; today only
  the cosmetic Ugly/Cumbersome do.
- Flashy = rarer: abilities & inlays at commonality ~0.5, staples at 1. Keep every category's
  alone-able adjective trait (generation throws otherwise).
- Odyssey's most common roll is a reliable "+X" with no proc — `UMW_Razored` now fills this tier in Bladed
  (a clean +damage/AP staple); Pointed/Blunt/Heavy could still use one.

Net-new gap ideas (low-tech-plausible). Abilities (cleave/slam), incendiary edge, wielder-hediff,
spear pole-vault, and the component-EMP-pulser are already listed under Features above.

- Wielder buffs via equippedStatOffsets (untapped, no ranged precedent): "swift" +MeleeDodgeChance/
  +MoveSpeed; "worn grip" +MeleeHitChance (alone✗ modifier — the melee CustomGrip).
- Wielder curse (tradeoff — serves the drawback heuristic): "bloodthirsty/cursed" +melee, −mood/+pain.
- More on-hit effects, only if not over-saturating the 15% (each is the tox-coating shape — a
  DamageDef/hediff via MeleeOnHitEffect): vampiric (heal attacker), crippling/hamstring (move-speed
  debuff hediff).
- Clean / tradeoff stat traits to rebalance off the status-heavy middle: Bladed "balanced edge"
  (+MeleeHitChance, no rider); Bladed "brittle razor" (+damage, −value/faster deterioration). NB a
  Blunt "flanged" +AP trait is muddy — melee AP isn't separable from damage (both ride
  MeleeWeapon_DamageMultiplier, owned by UMW_Heavy). If Blunt ever wants a 2nd trait, a wielder-buff
  archetype is the cleanest route (stun is already taken).
- Non-offensive abilities beyond slam/cleave: rallying cry (heirloom relic, brief ally combat buff)
  and a guard/parry stance (temporary +dodge/+block).

# Art

- ModIcon.png
- Preview.png
