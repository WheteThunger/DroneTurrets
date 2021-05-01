## Features

- Allows players with permission to deploy auto turrets onto RC drones
- Allows players with a separate permission to deploy NPC auto turrets
- Redirects damge from the turret to the drone

## Commands

- `droneturret` -- Deploys an auto turret onto the drone the player is looking at, consuming an auto turret item from their inventory unless they have permission for free turrets.
- `dronenpcturret` -- Deploys an NPC auto turret onto the drone the player is looking at. Does not consume any items.

## Permissions

- `droneturrets.deploy` -- Required to use the `droneturret` command.
- `droneturrets.deploynpc` -- Required to use the `dronenpcturret` command.
- `droneturrets.deploy.free` -- Allows using the `droneturret` command for free (no auto turret item required).
- `droneturrets.autodeploy` -- Deploying a drone while you have this permission will automatically deploy an auto turret to it, free of charge.
  - Not recommended if you want to allow players to deploy other attachments such as stashes since they are incompatible.

## Configuration

Default configuration:

```json
{
  "TipChance": 25
}
```

- `TipChance` (`0` - `100`) -- Chance that a tip message will be shown to a player when they deploy a drone, informing them that they can use the `/droneturret` command. Only applies to players with the `droneturrets.deploy` permission who do not have the `droneturrets.autodeploy` permission.

## Localization

```json
{
  "Tip.DeployCommand": "Tip: Look at the drone and run <color=yellow>/droneturret</color> to deploy a turret.",
  "Error.NoPermission": "You don't have permission to do that.",
  "Error.NoDroneFound": "Error: No drone found.",
  "Error.NoTurretItem": "Error: You need an auto turret to do that.",
  "Error.AlreadyHasTurret": "Error: That drone already has a turret.",
  "Error.IncompatibleAttachment": "Error: That drone has an incompatible attachment.",
  "Error.DeployFailed": "Error: Failed to deploy turret."
}
```

## FAQ

#### How do I get a drone?

As of this writing (March 2021), RC drones are a deployable item named `drone`, but they do not appear naturally in any loot table, nor are they craftable. However, since they are simply an item, you can use plugins to add them to loot tables, kits, GUI shops, etc. Admins can also get them with the command `inventory.give drone 1`, or spawn one in directly with `spawn drone.deployed`.

#### How do I remote-control a drone?

If a player has building privilege, they can pull out a hammer and set the ID of the drone. They can then enter that ID at a computer station and select it to start controlling the drone. Controls are `W`/`A`/`S`/`D` to move, `shift` (sprint) to go up, `ctrl` (duck) to go down, and mouse to steer.

Note: If you are unable to steer the drone, that is likely because you have a plugin drawing a UI that is grabbing the mouse cursor. The Movable CCTV was previously guilty of this and was patched in March 2021.

#### Is it possible to remove the black sphere?

Yes, by installing [Entity Scale Manager](https://umod.org/plugins/entity-scale-manager) and configuring it to hide spheres after resize. However, there is a significant performance cost to enabling that feature, so enable it cautiously, and disable it if it doesn't work out.

#### Why do the NPC auto turrets not have switches?

Switches don't spawn on NPC auto turrets because the player wouldn't be able to interact with them anyway, due to the collider being too large client-side. This cannot be addressed by a plugin without moving the switch out to an awkward position further away from the turret.

#### Can I use the NPC auto turrets that are seen in the Outpost monument?

No. These "scientist" turrets were tested on drones during development, but they have an issue where they sometimes disappear client-side when they move away from their spawn origin. That issue is difficult to work around, so this plugin supports only the "bandit" turrets.

## Recommended compatible plugins

- [Drone Hover](https://umod.org/plugins/drone-hover) -- Allows RC drones to hover in place while not being controlled.
- [Drone Lights](https://umod.org/plugins/drone-lights) -- Adds controllable search lights to RC drones.
- [Drone Storage](https://umod.org/plugins/drone-storage) -- Allows players to deploy a small stash to RC drones.
  - Note: A drone may only have a stash or a turret, not both at the same time.
- [Drone Effects](https://umod.org/plugins/drone-effects) -- Adds collision effects and propeller animations to RC drones.
- [Auto Flip Drones](https://umod.org/plugins/auto-flip-drones) -- Auto flips upside-down RC drones when a player takes control.
- [RC Identifier Fix](https://umod.org/plugins/rc-identifier-fix) -- Auto updates RC identifiers saved in computer stations to refer to the correct entity.
- [Better Turret Aim](https://umod.org/plugins/better-turret-aim) -- Allows mobile turrets to track targets more quickly.
- [Turret Loadouts](https://umod.org/plugins/turret-loadouts) -- Automatically fills turrets with weapons and ammo when deployed.
- [Turret Weapons](https://umod.org/plugins/turret-weapons) -- Allows turrets to accept more weapon types such as grenade launchers and rocket launchers.

## Developer API

#### API_DeployAutoTurret

Plugins can call this API to deploy an auto turret to a drone. The `BasePlayer` parameter is optional, but providing it is recommended because it will automatically authorize the player and allows for compatibility with plugins that automatically add weapons and ammo to the turret when deployed.

```csharp
AutoTurret API_DeployAutoTurret(Drone drone, BasePlayer player)
```

The return value will be the newly deployed auto turret, or `null` if the turret was not deployed for any of the following reasons.
- The drone already had an auto turret or other incompatible attachment
- Another plugin blocked it with the `OnDroneTurretDeploy` hook

#### API_DeployNpcAutoTurret

Similar to the `API_DeployAutoTurret` method, this deploys an NPC auto turret using the prefab that is typically seen at fishing villages. The `BasePlayer` parameter is optional, but providing it is recommended if initiated via a player action because it will allow plugins that are blocking it with a hook to provide feedback directly to that player.

```csharp
NPCAutoTurret API_DeployNpcAutoTurret(Drone drone, BasePlayer player)
```

The return value will be the newly deployed NPC auto turret, or `null` if the turret was not deployed for any of the following reasons.
- The drone already had an auto turret or other incompatible attachment
- Another plugin blocked it with the `OnDroneNpcTurretDeploy` hook

## Developer Hooks

#### OnDroneTurretDeploy

- Called when an auto turret is about to be deployed onto a drone
- Returning `false` will prevent the auto turret from being deployed
- Returning `null` will result in the default behavior
- The `BasePlayer` argument will be `null` if the turret is being deployed via the API without specifying a player.

```csharp
bool? OnDroneTurretDeploy(Drone drone, BasePlayer optionalDeployer)
```

#### OnDroneTurretDeployed

- Called after an auto turret has been deployed onto a drone
- No return behavior
- The `BasePlayer` argument will be `null` if the turret was deployed via the API without specifying a player.

```csharp
void OnDroneTurretDeployed(Drone drone, AutoTurret autoTurret, BasePlayer optionalDeployer)
```

#### OnDroneNpcTurretDeploy

- Called when an NPC auto turret is about to be deployed onto a drone
- Returning `false` will prevent the NPC auto turret from being deployed
- Returning `null` will result in the default behavior
- The `BasePlayer` argument will be `null` if the turret is being deployed via the API without specifying a player.

```csharp
bool? OnDroneNpcTurretDeploy(Drone drone, BasePlayer optionalDeployer)
```

#### OnDroneNpcTurretDeployed

- Called after an NPC auto turret has been deployed onto a drone
- No return behavior
- The `BasePlayer` argument will be `null` if the turret was deployed via the API without specifying a player.

```csharp
void OnDroneNpcTurretDeployed(Drone drone, NPCAutoTurret autoTurret, BasePlayer optionalDeployer)
```

#### OnEntityBuilt

This is an Oxide hook that is normally called when deploying an auto turret or other deployable. To allow for free compatibility with other plugins, this plugin calls this hook whenever an auto turret is deployed to a drone for a player.

- Not called when an auto turret is deployed via the API without specifying a player
- Not called when deploying NPC auto turrets
- The `Planner` can be used to get the player or the auto turret item, while the `GameObject` can be used to get the deployed auto turret.

```csharp
void OnEntityBuilt(Planner planner, GameObject go)
```
