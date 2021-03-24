using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Drone Turrets", "WhiteThunder", "1.0.0")]
    [Description("Allows players to deploy auto turrets to RC drones.")]
    internal class DroneTurrets : CovalencePlugin
    {
        #region Fields

        private static DroneTurrets _pluginInstance;
        private static Configuration _pluginConfig;

        private const float TurretScale = 0.6f;

        private const string PermissionDeploy = "droneturrets.deploy";
        private const string PermissionDeployNpc = "droneturrets.deploynpc";
        private const string PermissionDeployFree = "droneturrets.deploy.free";
        private const string PermissionAutoDeploy = "droneturrets.autodeploy";

        private const string SpherePrefab = "assets/prefabs/visualization/sphere.prefab";
        private const string AutoTurretPrefab = "assets/prefabs/npc/autoturret/autoturret_deployed.prefab";
        private const string NpcAutoTurretPrefab = "assets/content/props/sentry_scientists/sentry.bandit.static.prefab";
        private const string ElectricSwitchPrefab = "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab";
        private const string DeployEffectPrefab = "assets/prefabs/npc/autoturret/effects/autoturret-deploy.prefab";

        private const int AutoTurretItemId = -2139580305;

        private const BaseEntity.Slot TurretSlot = BaseEntity.Slot.UpperModifier;

        private static readonly Vector3 TurretLocalPosition = new Vector3(0, -0.4f, 0);
        private static readonly Vector3 SphereEntityLocalPosition = new Vector3(0, 0.1f, 0);
        private static readonly Vector3 TurretSwitchLocalPosition = new Vector3(0, -0.64f, -0.32f);
        private static readonly Quaternion TurretSwitchLocalRotation = Quaternion.Euler(0, 180, 0);

        #endregion

        #region Hooks

        private void Init()
        {
            _pluginInstance = this;

            permission.RegisterPermission(PermissionDeploy, this);
            permission.RegisterPermission(PermissionDeployNpc, this);
            permission.RegisterPermission(PermissionDeployFree, this);
            permission.RegisterPermission(PermissionAutoDeploy, this);
        }

        private void Unload()
        {
            _pluginInstance = null;
            _pluginConfig = null;
        }

        private void OnServerInitialized()
        {
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                var drone = entity as Drone;
                if (drone == null || !IsDroneEligible(drone))
                    continue;

                var droneTurret = GetDroneTurret(drone);
                if (droneTurret == null)
                    continue;

                RefreshDroneTurret(droneTurret);
            }
        }

        private object CanPickupEntity(BasePlayer player, Drone drone)
        {
            if (!IsDroneEligible(drone))
                return null;

            var turret = GetDroneTurret(drone);
            if (turret == null)
                return null;

            // Prevent drone pickup while it has a turret.
            // A player must remove the turret first.
            // Ignores NPC turrets since they can't be picked up.
            if (turret != null && !(turret is NPCAutoTurret))
                return false;

            return null;
        }

        private void OnEntityBuilt(Planner planner, GameObject go)
        {
            if (planner == null || go == null)
                return;

            var drone = go.ToBaseEntity() as Drone;
            if (drone == null)
                return;

            var player = planner.GetOwnerPlayer();
            if (player == null)
                return;

            NextTick(() =>
            {
                // Delay this check to allow time for other plugins to deploy an entity to this slot.
                if (drone == null || player == null || drone.GetSlot(TurretSlot) != null)
                    return;

                if (permission.UserHasPermission(player.UserIDString, PermissionAutoDeploy))
                {
                    DeployAutoTurret(drone, player, 1);
                }
                else if (permission.UserHasPermission(player.UserIDString, PermissionDeploy)
                    && UnityEngine.Random.Range(0, 100) < _pluginConfig.TipChance)
                {
                    ChatMessage(player, "Tip.DeployCommand");
                }
            });
        }

        private object OnSwitchToggled(ElectricSwitch electricSwitch)
        {
            var autoTurret = GetParentTurret(electricSwitch);
            if (autoTurret == null)
                return null;

            var drone = GetParentDrone(autoTurret);
            if (drone == null)
                return null;

            if (electricSwitch.IsOn())
                autoTurret.InitiateStartup();
            else
                autoTurret.InitiateShutdown();

            return null;
        }

        private object OnTurretTarget(AutoTurret turret, BasePlayer basePlayer)
        {
            if (turret == null || basePlayer == null || GetParentDrone(turret) == null)
                return null;

            // Don't target human or NPC players in safe zones, unless they are hostile.
            if (basePlayer.InSafeZone() && (basePlayer.IsNpc || !basePlayer.IsHostile()))
                return false;

            return null;
        }

        // Redirect damage from the turret to the drone.
        private object OnEntityTakeDamage(AutoTurret turret, HitInfo info)
        {
            var drone = GetParentDrone(turret);
            if (drone == null)
                return null;

            drone.Hurt(info);
            HitNotify(drone, info);

            return true;
        }

        // Redirect damage from the turret switch to the drone.
        private object OnEntityTakeDamage(ElectricSwitch electricSwitch, HitInfo info)
        {
            var autoTurret = GetParentTurret(electricSwitch);
            if (autoTurret == null)
                return null;

            var drone = GetParentDrone(autoTurret);
            if (drone == null)
                return null;

            drone.Hurt(info);
            HitNotify(drone, info);

            return true;
        }

        private void OnEntityKill(AutoTurret turret)
        {
            var sphereEntity = turret.GetParentEntity() as SphereEntity;
            if (sphereEntity == null)
                return;

            var drone = sphereEntity.GetParentEntity() as Drone;
            if (drone == null)
                return;

            sphereEntity.Invoke(() => sphereEntity.Kill(), 0);
        }

        private void OnEntityDeath(Drone drone)
        {
            if (!IsDroneEligible(drone))
                return;

            var turret = GetDroneTurret(drone);
            if (turret != null)
            {
                // Causing the turret to die allows its inventory to potentially be dropped.
                // This approach is intentionally used, as opposed to dropping the inventory
                // directly, in order to respect vanilla behavior around `turret.dropChance`, and
                // to allow other plugins to intercept the OnEntityDeath(AutoTurret) hook to kill
                // the inventory before vanilla logic drops it.
                turret.Die();
            }
        }

        #endregion

        #region API

        private AutoTurret API_DeployAutoTurret(Drone drone, BasePlayer player)
        {
            if (GetDroneTurret(drone) != null
                || drone.GetSlot(TurretSlot) != null
                || DeployTurretWasBlocked(drone, player))
                return null;

            return DeployAutoTurret(drone, player);
        }

        private NPCAutoTurret API_DeployNpcAutoTurret(Drone drone, BasePlayer player)
        {
            if (GetDroneTurret(drone) != null
                || drone.GetSlot(TurretSlot) != null
                || DeployNpcTurretWasBlocked(drone))
                return null;

            return DeployNpcAutoTurret(drone, player);
        }

        #endregion

        #region Commands

        [Command("droneturret")]
        private void DroneTurretCommand(IPlayer player)
        {
            if (player.IsServer)
                return;

            Drone drone;
            if (!VerifyPermission(player, PermissionDeploy)
                || !VerifyDroneFound(player, out drone)
                || !VerifyDroneHasNoTurret(player, drone)
                || !VerifyDroneSlotVacant(player, drone))
                return;

            Item autoTurretItem = null;
            var conditionFraction = 1f;

            var basePlayer = player.Object as BasePlayer;
            var isFree = player.HasPermission(PermissionDeployFree);
            if (!isFree)
            {
                autoTurretItem = FindPlayerAutoTurretItem(basePlayer);
                if (autoTurretItem == null)
                {
                    ReplyToPlayer(player, "Error.NoTurretItem");
                    return;
                }
                conditionFraction = GetItemConditionFraction(autoTurretItem);
            }

            if (DeployTurretWasBlocked(drone, basePlayer))
                return;

            if (DeployAutoTurret(drone, basePlayer, conditionFraction) == null)
            {
                ReplyToPlayer(player, "Error.DeployFailed");
                return;
            }

            if (!isFree && autoTurretItem != null)
                UseItem(basePlayer, autoTurretItem);
        }

        [Command("dronenpcturret")]
        private void DroneNpcTurretCommand(IPlayer player, string cmd, string[] args)
        {
            if (player.IsServer)
                return;

            var basePlayer = player.Object as BasePlayer;
            Drone drone;
            if (!VerifyPermission(player, PermissionDeployNpc)
                || !VerifyDroneFound(player, out drone)
                || !VerifyDroneHasNoTurret(player, drone)
                || !VerifyDroneSlotVacant(player, drone)
                || DeployNpcTurretWasBlocked(drone, basePlayer))
                return;

            if (DeployNpcAutoTurret(drone, basePlayer) == null)
                ReplyToPlayer(player, "Error.DeployFailed");
        }

        #endregion

        #region Helper Methods - Command Checks

        private bool VerifyPermission(IPlayer player, string perm)
        {
            if (player.HasPermission(perm))
                return true;

            ReplyToPlayer(player, "Error.NoPermission");
            return false;
        }

        private bool VerifyDroneFound(IPlayer player, out Drone drone)
        {
            var basePlayer = player.Object as BasePlayer;
            drone = GetLookEntity(basePlayer, 3) as Drone;
            if (drone != null && IsDroneEligible(drone))
                return true;

            ReplyToPlayer(player, "Error.NoDroneFound");
            return false;
        }

        private bool VerifyDroneHasNoTurret(IPlayer player, Drone drone)
        {
            if (GetDroneTurret(drone) == null)
                return true;

            ReplyToPlayer(player, "Error.AlreadyHasTurret");
            return false;
        }

        private bool VerifyDroneSlotVacant(IPlayer player, Drone drone)
        {
            if (drone.GetSlot(TurretSlot) == null)
                return true;

            ReplyToPlayer(player, "Error.IncompatibleAttachment");
            return false;
        }

        #endregion

        #region Helper Methods

        private static bool DeployTurretWasBlocked(Drone drone, BasePlayer deployer)
        {
            object hookResult = Interface.CallHook("OnDroneTurretDeploy", drone, deployer);
            return hookResult is bool && (bool)hookResult == false;
        }

        private static bool DeployNpcTurretWasBlocked(Drone drone, BasePlayer deployer = null)
        {
            object hookResult = Interface.CallHook("OnDroneNpcTurretDeploy", drone, deployer);
            return hookResult is bool && (bool)hookResult == false;
        }

        private static bool IsDroneEligible(Drone drone) =>
            !(drone is DeliveryDrone);

        private static Drone GetParentDrone(BaseEntity entity)
        {
            var sphereEntity = entity.GetParentEntity() as SphereEntity;
            return sphereEntity != null ? sphereEntity.GetParentEntity() as Drone : null;
        }

        private static Drone GetControlledDrone(BasePlayer player)
        {
            var computerStation = player.GetMounted() as ComputerStation;
            if (computerStation == null)
                return null;

            return GetControlledDrone(computerStation);
        }

        private static Drone GetControlledDrone(ComputerStation computerStation) =>
            computerStation.currentlyControllingEnt.Get(serverside: true) as Drone;

        private static AutoTurret GetDroneTurret(Drone drone) =>
            GetGrandChildOfType<AutoTurret>(drone);

        private static T GetGrandChildOfType<T>(BaseEntity entity) where T : BaseEntity
        {
            foreach (var child in entity.children)
            {
                foreach (var grandChild in child.children)
                {
                    var grandChildOfType = grandChild as T;
                    if (grandChildOfType != null)
                        return grandChildOfType;
                }
            }
            return null;
        }

        private static void HitNotify(BaseEntity entity, HitInfo info)
        {
            var player = info.Initiator as BasePlayer;
            if (player == null)
                return;

            entity.ClientRPCPlayer(null, player, "HitNotify");
        }

        private static SphereEntity SpawnSphereEntity(Drone drone, float scale = TurretScale)
        {
            SphereEntity sphereEntity = GameManager.server.CreateEntity(SpherePrefab, SphereEntityLocalPosition) as SphereEntity;
            if (sphereEntity == null)
                return null;

            SetupSphereEntity(sphereEntity);
            sphereEntity.currentRadius = scale;
            sphereEntity.lerpRadius = scale;
            sphereEntity.SetParent(drone);
            sphereEntity.Spawn();

            return sphereEntity;
        }

        private static AutoTurret DeployTurret(Drone drone, BasePlayer deployer, float conditionFraction = 1)
        {
            SphereEntity sphereEntity = SpawnSphereEntity(drone);
            var turret = GameManager.server.CreateEntity(AutoTurretPrefab, TurretLocalPosition) as AutoTurret;
            if (turret == null)
            {
                sphereEntity.Kill();
                return null;
            }

            if (deployer != null)
                turret.OwnerID = deployer.userID;

            SetupDroneTurret(turret);
            turret.SetFlag(IOEntity.Flag_HasPower, true);
            turret.SetParent(sphereEntity);
            turret.Spawn();
            turret.SetHealth(turret.MaxHealth() * conditionFraction);
            AttachTurretSwitch(turret);
            drone.SetSlot(TurretSlot, turret);

            Effect.server.Run(DeployEffectPrefab, turret.transform.position);
            Interface.CallHook("OnDroneTurretDeployed", drone, turret, deployer);

            return turret;
        }

        private static NPCAutoTurret DeployNpcAutoTurret(Drone drone, BasePlayer deployer)
        {
            SphereEntity sphereEntity = SpawnSphereEntity(drone);
            var turret = GameManager.server.CreateEntity(NpcAutoTurretPrefab, TurretLocalPosition) as NPCAutoTurret;
            if (turret == null)
            {
                sphereEntity.Kill();
                return null;
            }

            SetupDroneTurret(turret);
            turret.SetParent(sphereEntity);
            turret.Spawn();
            drone.SetSlot(BaseEntity.Slot.UpperModifier, turret);

            Effect.server.Run(DeployEffectPrefab, turret.transform.position);
            Interface.CallHook("OnDroneNpcTurretDeployed", drone, turret, deployer);

            return turret;
        }

        private static void RemoveProblemComponents(BaseEntity ent)
        {
            foreach (var collider in ent.GetComponentsInChildren<Collider>())
            {
                if (!collider.isTrigger)
                    UnityEngine.Object.DestroyImmediate(collider);
            }

            UnityEngine.Object.DestroyImmediate(ent.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(ent.GetComponent<GroundWatch>());
        }

        private static void SetupDroneTurret(AutoTurret turret)
        {
            // Damage will be processed by the drone.
            turret.baseProtection = null;

            RemoveProblemComponents(turret);
            HideInputsAndOutputs(turret);
        }

        private static ElectricSwitch AttachTurretSwitch(AutoTurret autoTurret)
        {
            var electricSwitch = GameManager.server.CreateEntity(ElectricSwitchPrefab, autoTurret.transform.TransformPoint(TurretSwitchLocalPosition), autoTurret.transform.rotation * TurretSwitchLocalRotation) as ElectricSwitch;
            if (electricSwitch == null)
                return null;

            SetupTurretSwitch(electricSwitch);
            electricSwitch.Spawn();
            electricSwitch.SetParent(autoTurret, true);

            return electricSwitch;
        }

        private static void HideInputsAndOutputs(IOEntity ioEntity)
        {
            // Hide the inputs and outputs on the client.
            foreach (var input in ioEntity.inputs)
                input.type = IOEntity.IOType.Generic;

            foreach (var output in ioEntity.outputs)
                output.type = IOEntity.IOType.Generic;
        }

        private static void SetupTurretSwitch(ElectricSwitch electricSwitch)
        {
            // Damage will be processed by the drone.
            electricSwitch.baseProtection = null;

            electricSwitch.pickup.enabled = false;
            electricSwitch.SetFlag(IOEntity.Flag_HasPower, true);
            RemoveProblemComponents(electricSwitch);
            HideInputsAndOutputs(electricSwitch);
        }

        private static void SetupSphereEntity(SphereEntity sphereEntity)
        {
            sphereEntity.EnableSaving(true);

            // Fix the issue where leaving the area and returning would not recreate the sphere and its children on clients.
            sphereEntity.EnableGlobalBroadcast(false);
        }

        private static void RefreshDroneTurret(AutoTurret turret)
        {
            SetupDroneTurret(turret);

            var sphereEntity = turret.GetParentEntity() as SphereEntity;
            if (sphereEntity != null)
                SetupSphereEntity(sphereEntity);

            var electricSwitch = turret.GetComponentInChildren<ElectricSwitch>();
            if (electricSwitch != null)
                SetupTurretSwitch(electricSwitch);
        }

        private static BaseEntity GetLookEntity(BasePlayer basePlayer, float maxDistance = 3)
        {
            RaycastHit hit;
            return Physics.Raycast(basePlayer.eyes.HeadRay(), out hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                ? hit.GetEntity()
                : null;
        }

        private static AutoTurret GetParentTurret(BaseEntity entity) =>
            entity.GetParentEntity() as AutoTurret;

        private static void RunOnEntityBuilt(Item turretItem, AutoTurret autoTurret) =>
            Interface.CallHook("OnEntityBuilt", turretItem.GetHeldEntity(), autoTurret.gameObject);

        private static void UseItem(BasePlayer basePlayer, Item item, int amountToConsume = 1)
        {
            item.UseItem(amountToConsume);
            basePlayer.Command("note.inv", item.info.itemid, -amountToConsume);
        }

        private static float GetItemConditionFraction(Item item) =>
            item.hasCondition ? item.condition / item.info.condition.max : 1.0f;

        private static Item FindPlayerAutoTurretItem(BasePlayer basePlayer) =>
            basePlayer.inventory.FindItemID(AutoTurretItemId);

        private AutoTurret DeployAutoTurret(Drone drone, BasePlayer basePlayer, float conditionFraction = 1)
        {
            var autoTurret = DeployTurret(drone, basePlayer, conditionFraction);
            if (autoTurret == null)
                return null;

            if (basePlayer == null)
                return autoTurret;

            autoTurret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID
            {
                userid = basePlayer.userID,
                username = basePlayer.displayName
            });
            autoTurret.SendNetworkUpdate();

            // Allow other plugins to detect the auto turret being deployed (e.g., to add a weapon automatically).
            var turretItem = FindPlayerAutoTurretItem(basePlayer);
            if (turretItem != null)
            {
                RunOnEntityBuilt(turretItem, autoTurret);
            }
            else
            {
                // Temporarily increase the player inventory capacity to ensure there is enough space.
                basePlayer.inventory.containerMain.capacity++;
                var temporaryTurretItem = ItemManager.CreateByItemID(AutoTurretItemId);
                if (basePlayer.inventory.GiveItem(temporaryTurretItem))
                {
                    RunOnEntityBuilt(temporaryTurretItem, autoTurret);
                    temporaryTurretItem.RemoveFromContainer();
                }
                temporaryTurretItem.Remove();
                basePlayer.inventory.containerMain.capacity--;
            }

            return autoTurret;
        }

        #endregion

        #region Configuration

        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("TipChance")]
            public int TipChance = 25;
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #endregion

        #region Configuration Boilerplate

        private class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => _pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _pluginConfig = Config.ReadObject<Configuration>();
                if (_pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_pluginConfig))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_pluginConfig, true);
        }

        #endregion

        #region Localization

        private void ReplyToPlayer(IPlayer player, string messageName, params object[] args) =>
            player.Reply(string.Format(GetMessage(player, messageName), args));

        private void ChatMessage(BasePlayer player, string messageName, params object[] args) =>
            player.ChatMessage(string.Format(GetMessage(player.IPlayer, messageName), args));

        private string GetMessage(IPlayer player, string messageName, params object[] args) =>
            GetMessage(player.Id, messageName, args);

        private string GetMessage(string playerId, string messageName, params object[] args)
        {
            var message = lang.GetMessage(messageName, this, playerId);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Tip.DeployCommand"] = "Tip: Look at the drone and run <color=yellow>/droneturret</color> to deploy a turret.",
                ["Error.NoPermission"] = "You don't have permission to do that.",
                ["Error.NoDroneFound"] = "Error: No drone found.",
                ["Error.NoTurretItem"] = "Error: You need an auto turret to do that.",
                ["Error.AlreadyHasTurret"] = "Error: That drone already has a turret.",
                ["Error.IncompatibleAttachment"] = "Error: That drone has an incompatible attachment.",
                ["Error.DeployFailed"] = "Error: Failed to deploy turret.",
            }, this, "en");
        }

        #endregion
    }
}
