## Video tutorial

[![Video Tutorial](https://img.youtube.com/vi/6RQUhdjJpgA/maxresdefault.jpg)](https://www.youtube.com/watch?v=6RQUhdjJpgA)

## Features

- Allows players with permission to deploy auto turrets onto RC drones
- Allows quickly switching between the drone and turret perspectives by pressing the "swap seats" key (default: X)
- Allows moving the drone while controlling the turret
- (Balance) Configurable turret range
- (Balance) Optionally disable targeting of players, NPCs and animals
- (Balance) Optionally prevent players from remote-controlling drone turrets (can control only the drone)
- (Balance) Optional flashing light and audio alarm to alert players of nearby drone turrets
- (Balance) Integrates with Drone Settings to allow configuring speed and toughness

## Known issues

Since the March 2023 Rust update, drones now sway in the wind, but attached entities do not sway. This causes undesirable visuals to players observing a drone that is being controlled. There is no known fix at this time.

## Permissions

- `droneturrets.deploy` -- Required to use the `droneturret` command.
- `droneturrets.deploynpc` -- Required to use the `dronenpcturret` command.
- `droneturrets.deploy.free` -- Allows using the `droneturret` command for free (no auto turret item required).
- `droneturrets.autodeploy` -- Deploying a drone while you have this permission will automatically deploy an auto turret to it, free of charge.
  - Not recommended if you want to allow players to deploy other attachments such as stashes since they are incompatible.
- `droneturrets.control` -- Allows remotely controlling drone turrets. Only necessary while the `RequirePermissionToControlDroneTurrets` config option is set to `true`.

## Commands

- `droneturret` -- Deploys an auto turret onto the drone the player is looking at, consuming an auto turret item from their inventory unless they have permission for free turrets.
- `dronenpcturret` -- Deploys an NPC auto turret onto the drone the player is looking at. Does not consume any items. NPC turrets are the ones found at safe zone monuments such as fishing villages.

## Configuration

Default configuration:

```json
{
  "RequirePermissionToControlDroneTurrets": false,
  "TargetPlayers": true,
  "TargetNPCs": true,
  "TargetAnimals": true,
  "EnableAudioAlarm": false,
  "EnableSirenLight": false,
  "TurretRange": 30.0,
  "TipChance": 25
}
```

- `RequirePermissionToControlDroneTurrets` (`true` or `false`) -- Determines whether players require the `droneturrets.control` permission to remotely control turrets attached to drones. While `true`, anybody can control drone turrets. While a player is prohibited from controlling drone turrets, they can still view the turrets perspective. Note: NPC auto turrets and peacekeeper turrets cannot be viewed under any circumstances.
- `TargetPlayers` (`true` or `false`) -- Determines whether drone-mounted turrets should target real players.
- `TargetNPCs` (`true` or `false`) -- Determines whether drone-mounted turrets should target NPCs.
- `TargetAnimals` (`true` or `false`) -- Determines whether drone-mounted turrets should target NPC animals such as bears.
- `EnableAudioAlarm` (`true` or `false`) -- Determines whether drone-mounted turrets should play an audio alarm for nearby players to hear, while the turret is powered, and while the drone is being controlled or hovering.
- `EnableSirenLight` (`true` or `false`) -- Determines whether drone-mounted turrets should have a flashing siren light to warn nearby players, while the turret is powered, and while the drone is being controlled or hovering.
- `TurretRange` (`true` or `false`) -- The range of drone-mounted turrets.
- `TipChance` (`0` - `100`) -- Chance that a tip message will be shown to a player when they deploy a drone, informing them that they can use the `/droneturret` command. Only applies to players with the `droneturrets.deploy` permission who do not have the `droneturrets.autodeploy` permission.

## Localization

```json
{
  "Tip.DeployCommand": "Tip: Look at the drone and run <color=yellow>/droneturret</color> to deploy a turret.",
  "Error.NoPermission": "You don't have permission to do that.",
  "Error.NoDroneFound": "Error: No drone found.",
  "Error.BuildingBlocked": "Error: Cannot do that while building blocked.",
  "Error.NoTurretItem": "Error: You need an auto turret to do that.",
  "Error.AlreadyHasTurret": "Error: That drone already has a turret.",
  "Error.IncompatibleAttachment": "Error: That drone has an incompatible attachment.",
  "Error.DeployFailed": "Error: Failed to deploy turret.",
  "Error.CannotPickupWithTurret": "Cannot pick up that drone while it has a turret."
}
```

## FAQ

#### Is it possible to remove the black sphere?

Yes, by installing [Entity Scale Manager](https://umod.org/plugins/entity-scale-manager) and configuring it to hide spheres after resize. However, there is a significant performance cost to enabling that feature, so enable it cautiously, and disable it if it doesn't work out.

#### Why do the NPC auto turrets not have switches?

Switches don't spawn on NPC auto turrets because the player wouldn't be able to interact with them anyway, due to the collider being too large client-side. This cannot be addressed by a plugin without moving the switch out to an awkward position further away from the turret.

#### Can I use the NPC auto turrets that are seen in the Outpost monument?

No. These "scientist" turrets were tested on drones during development, but they have an issue where they sometimes disappear client-side when they move away from their spawn origin. That issue is difficult to work around, so this plugin supports only the "bandit" turrets.

## Recommended compatible plugins

Drone balance:
- [Drone Settings](https://umod.org/plugins/drone-settings) -- Allows changing speed, toughness and other properties of RC drones.
- [Targetable Drones](https://umod.org/plugins/targetable-drones) -- Allows RC drones to be targeted by Auto Turrets and SAM Sites.
- [Limited Drone Range](https://umod.org/plugins/limited-drone-range) -- Limits how far RC drones can be controlled from computer stations.

Drone fixes and improvements:
- [Better Drone Collision](https://umod.org/plugins/better-drone-collision) -- Overhauls RC drone collision damage so it's more intuitive.
- [Auto Flip Drones](https://umod.org/plugins/auto-flip-drones) -- Auto flips upside-down RC drones when a player takes control.
- [Drone Hover](https://umod.org/plugins/drone-hover) -- Allows RC drones to hover in place while not being controlled.

Drone attachments:
- [Drone Lights](https://umod.org/plugins/drone-lights) -- Adds controllable search lights to RC drones.
- [Drone Turrets](https://umod.org/plugins/drone-turrets) (This plugin) -- Allows players to deploy auto turrets to RC drones.
- [Drone Storage](https://umod.org/plugins/drone-storage) -- Allows players to deploy a small stash to RC drones.
- [Ridable Drones](https://umod.org/plugins/ridable-drones) -- Allows players to ride RC drones by standing on them or mounting a chair.

Turret plugins:
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

```csharp
object OnDroneTurretDeploy(Drone drone, BasePlayer optionalDeployer)
```

- Called when an auto turret is about to be deployed onto a drone
- Returning `false` will prevent the auto turret from being deployed
- Returning `null` will result in the default behavior
- The `BasePlayer` argument will be `null` if the turret is being deployed via the API without specifying a player.

#### OnDroneTurretDeployed

```csharp
void OnDroneTurretDeployed(Drone drone, AutoTurret autoTurret, BasePlayer optionalDeployer)
```

- Called after an auto turret has been deployed onto a drone
- No return behavior
- The `BasePlayer` argument will be `null` if the turret was deployed via the API without specifying a player.

#### OnDroneNpcTurretDeploy

```csharp
object OnDroneNpcTurretDeploy(Drone drone, BasePlayer optionalDeployer)
```

- Called when an NPC auto turret is about to be deployed onto a drone
- Returning `false` will prevent the NPC auto turret from being deployed
- Returning `null` will result in the default behavior
- The `BasePlayer` argument will be `null` if the turret is being deployed via the API without specifying a player.

#### OnDroneNpcTurretDeployed

```csharp
void OnDroneNpcTurretDeployed(Drone drone, NPCAutoTurret autoTurret, BasePlayer optionalDeployer)
```

- Called after an NPC auto turret has been deployed onto a drone
- No return behavior
- The `BasePlayer` argument will be `null` if the turret was deployed via the API without specifying a player.

#### OnEntityBuilt

```csharp
void OnEntityBuilt(Planner planner, GameObject go)
```

This is an Oxide hook that is normally called when deploying an auto turret or other deployable. To allow for free compatibility with other plugins, this plugin calls this hook whenever an auto turret is deployed to a drone for a player.

- Not called when an auto turret is deployed via the API without specifying a player
- Not called when deploying NPC auto turrets
- The `Planner` can be used to get the player or the auto turret item, while the `GameObject` can be used to get the deployed auto turret.
