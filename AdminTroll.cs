using System;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Oxide.Core;
using Oxide.Core.Configuration;
using UnityEngine;
using VLB;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("AdminTroll", "Akaben", "1.1.0")]
    [Description("Allows admins to troll abusive players such as cheaters")]
    public class AdminTroll : RustPlugin
    {
        private void Init()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("AdminTroll");
            permission.RegisterPermission("admintroll.use", this);

            foreach (string playerID in storedData.Players)
            {
                trolledPlayers.Add(playerID);
            }
        }

        public void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("AdminTroll", storedData);
        }

        private ArrayList trolledPlayers = new ArrayList();
        private Dictionary<BasePlayer, BaseMountable> sitChairs = new Dictionary<BasePlayer, BaseMountable>();
        
        private class StoredData
        {
            public HashSet<string> Players = new HashSet<string>();

            public StoredData()
            {
            }
        }
        private StoredData storedData;

        private ItemDefinition FindItem(string itemNameOrId)
        {
            ItemDefinition itemDef = ItemManager.FindItemDefinition(itemNameOrId.ToLower());
            if (itemDef == null)
            {
                int itemId;
                if (int.TryParse(itemNameOrId, out itemId))
                {
                    itemDef = ItemManager.FindItemDefinition(itemId);
                }
            }
            return itemDef;
        }
        string noPermission = "You do not have permission to use that command!";

        BasePlayer FindPlayer(string name)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList) {
                if (player.displayName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return player;
                }
                if (player.UserIDString.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return player;
                }
            }
			
            return null;
        }

        public void dropAllItems(BasePlayer player)
        {
            foreach (Item item in player.inventory.containerMain.itemList.ToList())
            {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity);
            }
            foreach (Item item in player.inventory.containerBelt.itemList.ToList())
            {
                item.Drop(player.inventory.containerBelt.dropPosition, player.inventory.containerBelt.dropVelocity);
            }
            foreach (Item item in player.inventory.containerWear.itemList.ToList())
            {
                if (item.info.shortname != "attire.nesthat")
                {
                    item.Drop(player.inventory.containerWear.dropPosition, player.inventory.containerWear.dropVelocity);
                }
            }
        }

        public bool isTrolled(BasePlayer player)
        {
            if (trolledPlayers.Contains(player.UserIDString))
            {
                return true;
            }

            return false;
        }
        
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (isTrolled(player))
            {
                if (config.preventLooting)
                {
                    player.EndLooting();
                    player.ChatMessage(config.cannotLootMsg);
                    NextTick(player.EndLooting);
                    if (config.landMineLoot)
                    {
                        Vector3 position = player.transform.position;
                        Quaternion rot = new Quaternion(0, 0, 0, 1);
            
                        var landmine = GameManager.server.CreateEntity("assets/prefabs/deployable/landmine/landmine.prefab", position, rot) as Landmine;
                        landmine.OwnerID = player.userID;
                        landmine.Spawn();
                        landmine.Trigger();
                        timer.Once(0.8f, () =>
                        {
                            landmine.explosionRadius = 20f;
                            landmine.Explode();
                        });
                    }
                }
            }
        }

        private Random random = new System.Random();
        
        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            if (isTrolled(player))
            {
                var per = random.Next(1,100);
                if (config.dropChance != 0)
                {
                    if (per <= config.dropChance)
                    {
                        if (config.dropMessage != "")
                        {
                            player.ChatMessage(config.dropMessage);
                        }
                        projectile.GetItem().Drop(player.inventory.containerMain.dropPosition,
                            player.inventory.containerMain.dropVelocity);
                    }
                }
            }
        }
        
        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (isTrolled(player) && config.freezeRockets)
            {
                var rocket = entity.gameObject;
                rocket.SetActive(false);
            }
        }
        
        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
            {
                return null;
            }

            if (info.WeaponPrefab != null)
            {
                if (info.WeaponPrefab.name == "landmine")
                {
                    if (info.Initiator.OwnerID != 0)
                    {
                        if (entity is BasePlayer)
                        {
                            if (entity.GetComponent<BasePlayer>().UserIDString == info.Initiator.OwnerID.ToString())
                            {
                                entity.GetComponent<BasePlayer>().Hurt(100f);
                                return true;
                            }
                        }

                        return false;
                    }
                }
            }

            if (!info.InitiatorPlayer)
            {
                return null;
            }
            
            if (isTrolled(info.InitiatorPlayer) && config.cannotDamage)
            {
                if (config.damageKarma)
                {
                    var damageToDeal = config.damageKarmaAmt;
                    if (info.WeaponPrefab is TimedExplosive || info.Weapon.name == "rocket" || info.Weapon.name == "explosive.satchel")
                    {
                        damageToDeal = damageToDeal * 10;
                    }
                    info.InitiatorPlayer.Hurt(damageToDeal);
                }
                return false;
            }
            
            return null;
        }
        
        object OnExplosiveFuseSet(TimedExplosive explosive, float fuseLength)
        {
            if (explosive.PrefabName == "assets/prefabs/weapons/satchelcharge/explosive.satchel.deployed.prefab")
            {
                BasePlayer thrownBy;
                if (explosive.OwnerID != 0)
                {
                    thrownBy = BasePlayer.Find(explosive.OwnerID.ToString());
                    if (isTrolled(thrownBy))
                    {
                        var dud = explosive as DudTimedExplosive;
                            timer.Once(0.5f, () => { 
                                dud.BecomeDud(); 
                                dud.explosionRadius = 0; 
                            });
                    }
                }
            }

            return null;
        }
        
        void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            // get explosive
            if (entity.PrefabName == "assets/prefabs/weapons/satchelcharge/explosive.satchel.deployed.prefab")
            {
                if (isTrolled(player))
                {
                    if (config.nonStickySatchels)
                    {
                        var explosive = entity.GetComponent<TimedExplosive>();
                        explosive.canStick = false;
                    }
                    if (config.DudSatchels)
                    {
                        var explosive = entity.GetComponent<TimedExplosive>();
                        var dud = explosive as DudTimedExplosive;
                        explosive.OwnerID = player.userID;
                        timer.Once(0.5f, () => { 
                            dud.BecomeDud();
                            dud.explosionRadius = 0;
                        });
                    }
                }
            }
        }
        
        object OnReloadMagazine(BasePlayer player, BaseProjectile projectile, int useless)
        {
            if (isTrolled(player))
            {
                var per = random.Next(1,100);
                if (config.reloadFail != 0)
                {
                    if (per <= config.reloadFail)
                    {
                        if (config.reloadDrop)
                        {
                            projectile.GetItem().Drop(player.inventory.containerMain.dropPosition,
                                player.inventory.containerMain.dropVelocity);
                            if (!string.IsNullOrEmpty(config.reloadDropMsg))
                            {
                                player.ChatMessage(config.reloadDropMsg);
                            }

                            return false;
                        }

                        return false;
                    }
                }
            }
            return null;
        }
        
        [ChatCommand("trolls")]
        private object trollsCmd(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, config.permissionName))
            {
                player.ChatMessage(noPermission);

                return false;
            }

            player.ChatMessage("Troll list ("+trolledPlayers.Count+")");
            if (trolledPlayers.Count == 0)
            {
                player.ChatMessage("There are currently no trolled players.");
            }
            foreach (string playerObj in trolledPlayers)
            {
                var Base = BasePlayer.Find(playerObj);
                var onlineShow = "true";
                var color = "green";
                if (Base == null)
                {
                    onlineShow = "false";
                    color = "red";
                }

                var name = "Unknown Name (Unable to fetch)";
                if (onlineShow == "true")
                {
                    name = Base.displayName;
                }
                player.ChatMessage("ID: "+playerObj+" (Online: <color="+color+">"+onlineShow+"</color>) | "+name);
            }
            
            
            return null;
        }
        
        [ChatCommand("untroll")]
        private object untrollCmd(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, config.permissionName))
            {
                player.ChatMessage(noPermission);

                return false;
            }
            
            if (args.Length < 1) 
            {
                player.ChatMessage("You must specify a user's name or ID to troll list!");
                return false;
            }

            var NameOID = string.Join(" ", args.Skip(0).ToArray());
            var TrollPlayer = FindPlayer(NameOID);
            
            if (TrollPlayer == null)
            {
                player.ChatMessage("Unable to find player!");
                return false;
            }
            
            if (!trolledPlayers.Contains(TrollPlayer.UserIDString))
            {
                player.ChatMessage("This player is not in the troll list!");
                return false;
            }
            
            player.ChatMessage("Troll was removed from "+TrollPlayer.displayName+"!\nTroll List: /trolls");
            trolledPlayers.Remove(TrollPlayer.UserIDString);
            storedData.Players.Remove(TrollPlayer.UserIDString);
            if (config.persist)
            {
                SaveData();
            }
            return null;
        }
        
        [ChatCommand("troll")]
        private object trollCmd(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, config.permissionName))
            {
                player.ChatMessage(noPermission);

                return false;
            }
            
            if (args.Length < 1) 
            {
                player.ChatMessage("You must specify a user's name or ID to troll list!");
                return false;
            }

            var NameOID = string.Join(" ", args.Skip(0).ToArray());
            var TrollPlayer = FindPlayer(NameOID);

            if (TrollPlayer == null)
            {
                player.ChatMessage("Unable to find player!");
                return false;
            }
            
            if (trolledPlayers.Contains(TrollPlayer.UserIDString))
            {
                player.ChatMessage("This player is already in the troll list!\nYou can remove them via: /untroll");
                return false;
            }
            
            player.ChatMessage("Troll was applied to "+TrollPlayer.displayName+"!\nUntroll: /untroll\nTroll List: /trolls");
            trolledPlayers.Add(TrollPlayer.UserIDString);
            storedData.Players.Add(TrollPlayer.UserIDString);
            
            if (config.Teammates)
            {
                if (TrollPlayer.Team.members.Count == 0)
                {
                    player.ChatMessage("Attempted to add their team to troll list, but they have no team.");
                }
                
                foreach (ulong memberID in TrollPlayer.Team.members)
                {
                    var online = BasePlayer.Find(memberID.ToString());
                    if (online != null && online is BasePlayer)
                    {
                        if (!trolledPlayers.Contains(online.UserIDString))
                        {
                            var memberString = memberID.ToString();
                            trolledPlayers.Add(memberString);
                            storedData.Players.Add(memberString);
                            player.ChatMessage("Added " + online.displayName + " (" + online.UserIDString +
                                               ") to the list from " + TrollPlayer.displayName + "'s team");
                        }
                        else
                        {
                            player.ChatMessage("Failed to add "+online.displayName + " to the troll list because they already exist in it");
                        }
                    }
                }
            }
            
            if (config.persist)
            {
                SaveData();
            }
            return null;
        }
        
        
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (sitChairs.ContainsKey(player))
            {
                Puts("is in obj");
                //player.EndSleeping();
                var mountable = sitChairs[player].transform.position;
                var assignedSeat = sitChairs[player];
                timer.Once(0.5f, () =>
                {
                    Puts("Tped");
                    player.Teleport(mountable);
                    assignedSeat.DismountAllPlayers();
                    player.MountObject(assignedSeat);
                });
            }
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (sitChairs.ContainsKey(player))
            {
                //player.EndSleeping();
                var Chair = sitChairs[player];
                sitChairs.Remove(player);
                Chair.DismountAllPlayers();
                Chair.AdminKill();
            }
        }
        
        [ChatCommand("sit")]
        private object sofaSit(BasePlayer player, string command, string[] args)
        {
            var sofaDeployed = "assets/bundled/prefabs/static/chair.invisible.static.prefab";
            if (args.Length < 1) 
            {
                player.ChatMessage("You must specify a user's name or ID to sit!");
                return false;
            }
            
            var NameOID = string.Join(" ", args.Skip(0).ToArray());
            var TrollPlayer = FindPlayer(NameOID);
            
            if (TrollPlayer.isMounted)
            {
                player.ChatMessage("Sit cannot work when a player is mounted.");
                return false;
            }
            if (TrollPlayer.IsFlying)
            {
                player.ChatMessage("Sit cannot work when a player is flying.");
                return false;
            }
            
            Vector3 position = player.transform.position;
            Quaternion rot = new Quaternion(0, 0, 0, 1);
            var gameServer =
                GameManager.server.CreateEntity(sofaDeployed, position, rot) as
                    BaseMountable;
            gameServer.Spawn();
            sitChairs.Add(TrollPlayer,gameServer);
            
            TrollPlayer.Teleport(gameServer.transform.position);
            TrollPlayer.MountObject(gameServer,1);

            Vector3 ToTp = new Vector3(TrollPlayer.transform.position.x, TrollPlayer.transform.position.y+3,
                TrollPlayer.transform.position.z);

            player.Teleport(ToTp);
            
            return null;
        }

        [ChatCommand("unsit")]
        private object unsofaSit(BasePlayer player, string command, string[] args)
        {
            var sofaDeployed = "assets/bundled/prefabs/static/chair.invisible.static.prefab";
            if (args.Length < 1) 
            {
                player.ChatMessage("You must specify a user's name or ID to unsit!");
                return false;
            }
            
            var NameOID = string.Join(" ", args.Skip(0).ToArray());
            var TrollPlayer = FindPlayer(NameOID);

            if (!sitChairs.ContainsKey(TrollPlayer))
            {
                player.ChatMessage("This user does not have a seat!");
                return false;
            }

            var sitChair = sitChairs[TrollPlayer];
            sitChair.DismountAllPlayers();
            sitChair.AdminKill();
            sitChairs.Remove(TrollPlayer);
            
            player.ChatMessage("Seat was removed.");
            return null;
        }
        
        [ChatCommand("landmine")]
        private object landmineCc(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, config.permissionName))
            {
                player.ChatMessage(noPermission);

                return false;
            }
            
            if (args.Length < 1) 
            {
                player.ChatMessage("You must specify a user's name or ID to landmine kill!");
                return false;
            }
            
            var NameOID = string.Join(" ", args.Skip(0).ToArray());
            var TrollPlayer = FindPlayer(NameOID);

            if (TrollPlayer.isMounted)
            {
                player.ChatMessage("Landmine cannot work when a player is mounted.");
                return false;
            }
            if (TrollPlayer.IsFlying)
            {
                player.ChatMessage("Landmine cannot work when a player is flying.");
                return false;
            }

            Vector3 position = TrollPlayer.transform.position;
            Quaternion rot = new Quaternion(0, 0, 0, 1);
            
            var landmine = GameManager.server.CreateEntity("assets/prefabs/deployable/landmine/landmine.prefab", position, rot) as Landmine;
            landmine.Spawn();
            landmine.Trigger();
            timer.Once(0.8f, () =>
            {
                landmine.explosionRadius = 20f;
                landmine.Explode();
            });
            player.ChatMessage("Landmine spawned at "+position.ToString() + " (under "+TrollPlayer.displayName+")");
            return null;
        }
        
        
        [ChatCommand("freefall")]
        private object freefallCc(BasePlayer player, string command, string[] args)
        {
            //if (player.HasPlayerFlag("IsAdmin"))
            //{
            //    noPermission = noPermission + "\nPermission: "+config.permissionName;
            //}
            if (!permission.UserHasPermission(player.UserIDString, config.permissionName))
            {
                player.ChatMessage(noPermission);

                return false;
            }

            if (args.Length < 1) 
            {
                player.ChatMessage("You must specify a user's name or ID to chicken!");
                return false;
            }

            var NameOID = string.Join(" ", args.Skip(0).ToArray());
            var TrollPlayer = FindPlayer(NameOID);

            if (TrollPlayer.IsFlying)
            {
                player.ChatMessage(TrollPlayer.displayName + " is flying and cannot be freefalled.");
                return false;
            }
            
            if (TrollPlayer.isMounted)
            {
                player.ChatMessage(TrollPlayer.displayName + " is mounted and cannot be freefalled.");
                return false;
            }
            
            var newPosition = new Vector3(TrollPlayer.transform.position.x,TrollPlayer.transform.position.y+100,TrollPlayer.transform.position.z);
            TrollPlayer.PauseFlyHackDetection(7f);
            TrollPlayer.Teleport(newPosition);
            player.ChatMessage(TrollPlayer.displayName + " is now in free falling state!");
            return null;
        }
        
        [ChatCommand("chicken")]
        private object chickenCc(BasePlayer player, string command, string[] args)
        {
            //if (player.HasPlayerFlag("IsAdmin"))
            //{
            //    noPermission = noPermission + "\nPermission: "+config.permissionName;
            //}
            if (!permission.UserHasPermission(player.UserIDString, config.permissionName))
            {
                player.ChatMessage(noPermission);

                return false;
            }

            if (args.Length < 1) 
            {
                player.ChatMessage("You must specify a user's name or ID to chicken!");
                return false;
            }
                
            var NameOID = string.Join(" ", args.Skip(0).ToArray());
            var TrollPlayer = FindPlayer(NameOID);
            TrollPlayer.inventory.containerBelt.SetLocked(true);
            TrollPlayer.inventory.containerMain.SetLocked(true);

            var chickenHead = FindItem("1081315464");
            Item nest = ItemManager.Create(chickenHead);
            nest.MoveToContainer(TrollPlayer.inventory.containerWear);
            TrollPlayer.inventory.containerWear.SetLocked(true);
            dropAllItems(TrollPlayer);
            
            player.ChatMessage("Chicken enabled for "+TrollPlayer.displayName+" | Auto-deactivation: 30 seconds");
            var chickenStopW = new Stopwatch();
            Timer thisTimer = new Timer(null);
            chickenStopW.Start();
            thisTimer = timer.Every(1f, () =>
            {
                TrollPlayer.SendConsoleCommand("gesture chicken");
                dropAllItems(TrollPlayer);
                Effect.server.Run("assets/rust.ai/agents/chicken/model/chicken@attack.fbx",
                    TrollPlayer.ServerPosition);
            });
                timer.Once(30f, () =>
            {
                thisTimer.Destroy();
                nest.Remove();
                TrollPlayer.inventory.containerBelt.SetLocked(false);
                TrollPlayer.inventory.containerMain.SetLocked(false);
                TrollPlayer.inventory.containerWear.SetLocked(false);
            });
            return null;
        }
        
        object OnCollectiblePickup(Item item, BasePlayer player, CollectibleEntity entity)
        {
            if (isTrolled(player))
            {
                if (config.canTakeCloth == false)
                {
                    if (config.canTakeClothMsg != "")
                    {
                        player.ChatMessage(config.canTakeClothMsg);
                    }
                    return false;
                }
            }
            return null;
        }
        
        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (isTrolled(player))
            {
                if (config.canFarm == false)
                {
                    if (config.cannotFarm != "")
                    {
                        player.ChatMessage(config.cannotFarm);
                    }

                    dispenser.finishBonus = new List<ItemAmount>();
                    return false;
                }
            }
            return null;
        }
        
        object OnItemCraft(ItemCraftTask task, BasePlayer player, Item item)
        {
            if (isTrolled(player) && config.cannotCraft)
            {
                task.cancelled = true;
                if (config.craftHorseDung)
                {
                    var dungItem = FindItem("-1579932985");
                    Item horsePoop = ItemManager.Create(dungItem);
                    player.GiveItem(horsePoop,BasePlayer.GiveItemReason.Crafted);
                }

                if (config.cannotCMsg != "")
                {
                    player.ChatMessage(config.cannotCMsg);
                }
            }
            return null;
        }
        
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            var player = entity.GetComponent<BasePlayer>();
            if (isTrolled(player))
            {
                if (config.canFarm == false)
                {
                    if (config.cannotFarm != "")
                    {
                        player.ChatMessage(config.cannotFarm);
                    }

                    if (config.cannotGather)
                    {
                        player.ShowToast(2,"You cannot gather anything here.");
                    }
                    return false;
                }
            }
            return null;
        }

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Permission")]
            public string permissionName = "admintroll.use";
            
            [JsonProperty(PropertyName = "Prevent Looting")]
            public bool preventLooting = true;
            
            [JsonProperty(PropertyName = "Spawn Landmine On Loot")]
            public bool landMineLoot = false;
            
            [JsonProperty(PropertyName = "Drop Player's Active Item On Attack Chance")]
            public int dropChance = 50;
            
            [JsonProperty(PropertyName = "Drop Gun On Shoot Message")]
            public string dropMessage = "";
            
            [JsonProperty(PropertyName = "Fail Reload Chance")]
            public int reloadFail = 50;
            
            [JsonProperty(PropertyName = "Drop Weapon On Fail Reload")]
            public bool reloadDrop = true;
            
            [JsonProperty(PropertyName = "Fail Reload Message")]
            public string reloadDropMsg = "";

            [JsonProperty(PropertyName = "Cannot Loot Message")]
            public string cannotLootMsg = "Cannot open container as it does not exist on the server.";
            
            [JsonProperty(PropertyName = "Troll Teammates")]
            public bool Teammates = false;
            
            [JsonProperty(PropertyName = "Instantly Dud Satchels")]
            public bool DudSatchels = true;
            
            [JsonProperty(PropertyName = "Freeze Rockets")]
            public bool freezeRockets = true;

            [JsonProperty(PropertyName = "Non Sticky Satchels")]
            public bool nonStickySatchels = true;
            
            [JsonProperty(PropertyName = "Trolled Players Cannot Damage")]
            public bool cannotDamage = true;
            
            [JsonProperty(PropertyName = "Trolled Players Damage Karma")]
            public bool damageKarma = true;

            [JsonProperty(PropertyName = "Damage Karma Amount")]
            public int damageKarmaAmt = 10;
            
            [JsonProperty(PropertyName = "Can Farm Nodes/Trees")]
            public bool canFarm = false;
            
            [JsonProperty(PropertyName = "Cannot Gather Toast On Fail")]
            public bool cannotGather = true;
            
            [JsonProperty(PropertyName = "Cannot Farm Message")]
            public string cannotFarm = "";

            [JsonProperty(PropertyName = "Can Collect Cloth")]
            public bool canTakeCloth = false;
            
            [JsonProperty(PropertyName = "Cannot Take Cloth Message")]
            public string canTakeClothMsg = "";
            
            [JsonProperty(PropertyName = "Cannot Craft Items")]
            public bool cannotCraft = true;
            
            [JsonProperty(PropertyName = "Replace Crafted Item With Horse Dung")]
            public bool craftHorseDung = true;
            
            [JsonProperty(PropertyName = "Cannot Craft Message")]
            public string cannotCMsg = "";
            
            [JsonProperty(PropertyName = "Cannot Heal")]
            public bool cannotHeal = true;
            
            [JsonProperty(PropertyName = "Cannot Heal Message")]
            public string cannotHealMsg = "";
            
            [JsonProperty(PropertyName = "Cannot Build")]
            public bool cannotBuild = true;
            
            [JsonProperty(PropertyName = "Cannot Build Message")]
            public string cannotBuildMsg = "";
            
            [JsonProperty(PropertyName = "Cannot Upgrade")]
            public bool cannotUpgrade = true;
            
            [JsonProperty(PropertyName = "Cannot Upgrade Message")]
            public string cannotUpgradeMsg = "";
            
            [JsonProperty(PropertyName = "Persist Trolled Player List On Restart")]
            public bool persist = true;
        }
        
        private object OnEntityBuilt(Planner planner, GameObject gObject)
        {
            BasePlayer player = planner?.GetOwnerPlayer();
            if (player == null)
                return false;

            BaseEntity entity = gObject?.ToBaseEntity();
            if (entity == null)
                return false;

            if (isTrolled(player))
            {
                if (config.cannotBuild)
                {
                    entity.Invoke(() => entity.Kill(BaseNetworkable.DestroyMode.Gib), 0.1f);
                    if (config.cannotBuildMsg != "")
                    {
                        player.ChatMessage(config.cannotBuildMsg);
                    }
                }
            }
            
            return false;
        }

        object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (isTrolled(player))
            {
                if (config.cannotUpgrade)
                {
                    if (config.cannotUpgradeMsg != "")
                    {
                        player.ChatMessage(config.cannotUpgradeMsg);
                    }
                    
                    return false;
                }
            }
            return null;
        }
        
        object OnHealingItemUse(MedicalTool tool, BasePlayer player)
        {
            if (isTrolled(player))
            {
                if (config.cannotHeal == true)
                {
                    if (config.cannotHealMsg != "")
                    {
                        player.ChatMessage(config.cannotHealMsg);
                    }
                    return false;
                }
            }
            return null;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintError("Configuration file is corrupt! Unloading plugin...");
                Interface.Oxide.RootPluginManager.RemovePlugin(this);
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
    }
}
