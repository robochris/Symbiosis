using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>The player's numbers after every item is counted. Worked out fresh whenever it is asked for.</summary>
    public struct PlayerStats
    {
        public float moveSpeedMultiplier;
        public float dodgeCooldownMultiplier;
        public int dodgeCharges;
        /// <summary>Dodge is a hover that passes over ground hazards.</summary>
        public bool hoverDodge;
        public int damageMultiplier;
        public float fireIntervalMultiplier;
        public int pierce;
        public int split;
        public int acid;
        public int burn;
        public float critChance;
        public float aimAssistBonusAngle;
        /// <summary>Added to every shot's damage before anything multiplies it.</summary>
        public int damageBonus;
        /// <summary>Above 0: every this-many-th shot is charged (double damage, pierces two more).</summary>
        public int chargedEvery;
        /// <summary>Above 0: a dodge leaves a burning patch behind, bigger with more.</summary>
        public int fireDodge;
        /// <summary>How many times a shot comes back off a wall.</summary>
        public int bounce;
        /// <summary>Above 0: shots chill what they hit, slowing it. More lasts longer.</summary>
        public int chill;
        /// <summary>How much further coins and samples fly in from. 1 = the usual reach.</summary>
        public float magnetMultiplier;
        /// <summary>How much faster kills fill the hero power. 1 = the usual rate.</summary>
        public float powerFillMultiplier;
        /// <summary>How much more often enemies leave a crumb for a hurt player. 1 = the usual chance.</summary>
        public float crumbMultiplier;
        /// <summary>Extra coins from everything put down.</summary>
        public int coinBonus;
        /// <summary>Above 0: the player's puddles reach fliers too.</summary>
        public int splashUp;
        /// <summary>How much further a dodge carries. 1 = the usual distance.</summary>
        public float dodgeReachMultiplier;
        public int farBonus, returns, popDodge;
        /// <summary>Above 0: every this-many-th shot calls lightning down where it lands.</summary>
        public int lightningEvery;

        public static PlayerStats Default => new PlayerStats
        {
            moveSpeedMultiplier = 1f, dodgeCooldownMultiplier = 1f, dodgeCharges = 1,
            damageMultiplier = 1, fireIntervalMultiplier = 1f,
            magnetMultiplier = 1f, powerFillMultiplier = 1f, crumbMultiplier = 1f
        };
    }

    /// <summary>
    /// The items the player is carrying this run: how many of each, what they add up to, and how they look on the character.
    /// Picking up a duplicate makes that item's effect stronger.
    /// </summary>
    public class PlayerItems : MonoBehaviour
    {
        [Tooltip("Chance per check that an enemy near a Pheromone Cloud turns on its neighbours.")]
        [Range(0f, 1f)] public float pheromoneChance = 0.25f;
        public float pheromoneRadius = 6f;
        public float pheromoneInterval = 2f;

        /// <summary>How much of its size the Void keeps with each of Finch's doses.</summary>
        public const float DoseSize = 0.82f;

        public event Action Changed;
        /// <summary>Hits the Chitin Plate can still block in this room.</summary>
        public int ShieldCharges { get; private set; }
        public IReadOnlyList<ItemDefinition> Collected => _collected;
        public LarvaBuddy Buddy { get; private set; }

        readonly Dictionary<ItemType, int> _counts = new Dictionary<ItemType, int>();
        readonly List<ItemDefinition> _collected = new List<ItemDefinition>();
        readonly Dictionary<ItemSlot, GameObject> _looks = new Dictionary<ItemSlot, GameObject>();
        PlayerController _player;
        Health _health;
        HeroPower _power;
        RoomNode _lastRoom;
        float _nextPheromone;

        void Awake()
        {
            _player = GetComponent<PlayerController>();
            _health = GetComponent<Health>();
            _power = GetComponent<HeroPower>();
        }

        void OnEnable()
        {
            FloorManager.RoomChanged += OnRoomChanged;
        }

        void OnDisable()
        {
            FloorManager.RoomChanged -= OnRoomChanged;
        }

        void Start()
        {
            _health.Damaged += BreakGlass;
            // Chitin Plate: a shell that soaks up the first hit in every room.
            _health.ShouldBlock = direction =>
            {
                if (ShieldCharges > 0)
                {
                    ShieldCharges--;
                    _health.SetInvulnerable(0.5f);
                    Sfx.Play(SfxId.Blocked);
                    Changed?.Invoke();
                    return true;
                }
                // A first run's safety net: a lethal hit caught, a few times.
                if (_health.Current <= 1 && SafetyNet.Catch())
                {
                    _health.SetInvulnerable(1.5f);
                    Sfx.Play(SfxId.Blocked);
                    return true;
                }
                // Save State: the hit that would have ended the run, loaded back from just before it. Once a copy.
                if (_health.Current <= 1 && SaveStatesUsed < Count(ItemType.SaveState))
                {
                    SaveStatesUsed++;
                    _health.SetInvulnerable(1.5f);
                    Sfx.Play(SfxId.PowerOn);
                    var hud = FindAnyObjectByType<Hud>();
                    if (hud != null) hud.Toast("Save State: loaded from just before that.", 3f);
                    Changed?.Invoke();
                    return true;
                }
                return false;
            };
        }

        public int Count(ItemType type) => _counts.TryGetValue(type, out int count) ? count : 0;

        /// <summary>The Glass Heart has been broken by a hit, for the rest of the run.</summary>
        public bool GlassBroken { get; private set; }

        void BreakGlass(int amount, Vector3 direction)
        {
            if (amount <= 0 || GlassBroken || Count(ItemType.GlassHeart) == 0) return;
            GlassBroken = true;
            Sfx.Play(SfxId.Blast, 0.6f);
            Fx.Burst(transform.position + Vector3.up, new Color(0.8f, 0.95f, 1f), 20, 6f, 0.1f, 0.5f);
            var hud = FindAnyObjectByType<Hud>();
            if (hud != null) hud.Toast("The Glass Heart breaks. It was lovely while it lasted.", 3f);
            Changed?.Invoke();
        }

        public PlayerStats Stats
        {
            get
            {
                int legs = Count(ItemType.ExtraLegs), wings = Count(ItemType.WingBuds), jelly = Count(ItemType.QueensJelly);
                int marble = Count(ItemType.LostMarble), sprinkles = Count(ItemType.Sprinkles), sock = Count(ItemType.StaticSock);
                int dose = Count(ItemType.FinchsDose);
                int hat = Count(ItemType.PaperHat), coffee = Count(ItemType.ColdCoffee);
                int fish = Count(ItemType.Fishbowl), clippings = Count(ItemType.GrassClippings), notice = Count(ItemType.OverdueNotice);
                var rule = RunRules.Active;
                bool overcharged = _power != null && _power.IsOvercharged;

                var stats = new PlayerStats
                {
                    moveSpeedMultiplier = 1f + 0.15f * legs + 0.08f * jelly + 0.12f * dose + 0.18f * coffee + 0.1f * fish + 0.12f * clippings + (rule == RunRules.Rule.QuickFeet ? 0.2f : 0f),
                    // Coffee: everything quicker except getting your breath back.
                    dodgeCooldownMultiplier = Mathf.Pow(0.75f, legs) * Mathf.Pow(0.9f, jelly) * Mathf.Pow(0.85f, dose) * Mathf.Pow(1.3f, coffee),
                    dodgeCharges = 1 + (legs > 0 && wings > 0 ? 1 : 0) + Count(ItemType.SpringHeel),
                    hoverDodge = wings > 0,
                    damageMultiplier = overcharged ? 2 : 1,
                    // A marble is heavy: harder hits, further apart. Sprinkles are the opposite.
                    fireIntervalMultiplier = Mathf.Pow(0.9f, jelly) * Mathf.Pow(1.35f, marble) * Mathf.Pow(0.85f, sprinkles) * Mathf.Pow(0.85f, coffee) * Mathf.Pow(0.75f, clippings) * Mathf.Pow(0.8f, Count(ItemType.PlugChain)) * (overcharged ? 0.67f : 1f),
                    damageBonus = marble + hat + notice + (rule == RunRules.Rule.GlassCannon ? 1 : 0),
                    coinBonus = Count(ItemType.PiggyBank) + Count(ItemType.DustBag) + (rule == RunRules.Rule.GoldRush ? 1 : 0),
                    // The sock: every fifth shot, then every fourth, never more often than every second.
                    chargedEvery = sock > 0 ? Mathf.Max(2, 6 - sock) : 0,
                    fireDodge = Count(ItemType.BirthdayCandle) + Count(ItemType.EverlastingWick),
                    bounce = Count(ItemType.RubberBand) + (rule == RunRules.Rule.Bouncy ? 1 : 0),
                    chill = Count(ItemType.IceCube) + 2 * Count(ItemType.FrostCore),
                    splashUp = Count(ItemType.RainBoot),
                    dodgeReachMultiplier = 1f + 0.6f * Count(ItemType.KiteString) + 0.4f * Count(ItemType.SqueakyToy),
                    farBonus = Count(ItemType.MagnifyingGlass) + 2 * Count(ItemType.Footnote) + (rule == RunRules.Rule.LongShot ? 1 : 0),
                    returns = Count(ItemType.YoYo) + Count(ItemType.SilkThread),
                    popDodge = Count(ItemType.BubbleWrap) + 2 * Count(ItemType.SqueakyToy),
                    lightningEvery = Count(ItemType.BottledLightning) > 0 ? Mathf.Max(3, 7 - Count(ItemType.BottledLightning)) : 0,
                    magnetMultiplier = (1f + 1.5f * Count(ItemType.FridgeMagnet) + 3f * Count(ItemType.DustBag)) * (rule == RunRules.Rule.Sticky ? 2f : 1f),
                    powerFillMultiplier = (1f + 0.35f * Count(ItemType.SpareBattery)) * (1f + 0.5f * Count(ItemType.Croak)) * (1f + 0.3f * notice),
                    crumbMultiplier = 1f + Count(ItemType.Lunchbox),
                    pierce = 2 * Count(ItemType.NeedleTip) + 2 * Count(ItemType.QueensCommand) + Count(ItemType.SilkThread) + Count(ItemType.PlugChain) + (overcharged ? 99 : 0),
                    split = Count(ItemType.SplitterMandible) + Count(ItemType.QueensCommand),
                    acid = Count(ItemType.AcidGland),
                    burn = Count(ItemType.FireflyLantern) + 2 * Count(ItemType.EverlastingWick),
                    critChance = 0.15f * Count(ItemType.CompoundEye) + 0.1f * jelly + 0.12f * Count(ItemType.FrostCore) + 0.15f * Count(ItemType.Footnote) + (rule == RunRules.Rule.SharpEyes ? 0.1f : 0f),
                    aimAssistBonusAngle = 10f * Count(ItemType.CompoundEye)
                };

                // Tiny: much quicker, for a while.
                if (_power != null && _power.IsTiny)
                {
                    stats.moveSpeedMultiplier += 0.45f;
                    stats.dodgeCooldownMultiplier *= 0.5f;
                }

                // Glass Heart, until it breaks. Odd Sock: one is a little luck, a pair a lot. Adrenaline, at the last point.
                if (Count(ItemType.GlassHeart) > 0 && !GlassBroken) stats.damageBonus += 2;
                stats.critChance += 0.05f * Count(ItemType.GooglyEyes);
                int socks = Count(ItemType.OddSock);
                stats.critChance += socks >= 2 ? 0.25f : 0.06f * socks;
                if (Count(ItemType.AdrenalineGland) > 0 && _health != null && _health.Current == 1 && _health.maxHealth > 1)
                {
                    stats.fireIntervalMultiplier *= 0.6f;
                    stats.moveSpeedMultiplier += 0.2f;
                }

                // Synergies: pairs that are more together.
                if (Synergies.Has("Packed Lunch Money", this)) stats.coinBonus += 1;
                if (Synergies.Has("Steam", this)) stats.damageBonus += 1;
                if (Synergies.Has("Elastic", this)) stats.bounce += 1;
                if (Synergies.Has("Eagle Eye", this)) stats.critChance += 0.1f;
                if (Synergies.Has("Pogo", this)) stats.dodgeReachMultiplier += 0.4f;
                if (Synergies.Has("Party Popper", this)) stats.popDodge += 1;
                if (Synergies.Has("Acid Rain", this)) stats.acid += 1;
                if (Synergies.Has("Sugar Rush", this)) stats.moveSpeedMultiplier += 0.1f;
                if (Synergies.Has("Static Cling", this) && stats.chargedEvery > 2) stats.chargedEvery -= 1;
                if (Synergies.Has("Royal Guard", this)) stats.pierce += 1;
                return stats;
            }
        }

        public void Add(ItemDefinition item)
        {
            if (item == null) return;
            _counts[item.type] = Count(item.type) + 1;
            _collected.Add(item);
            // On file in the lab from now on. It is written to disk with the next save.
            FilingCabinet.File(item);

            switch (item.type)
            {
                case ItemType.SugarCube:
                    _health.SetMaxHealth(_health.maxHealth + 1, true);
                    break;
                case ItemType.ExtraLife:
                    _health.SetMaxHealth(_health.maxHealth + 2, true);
                    break;
                case ItemType.Fishbowl:
                    _health.SetMaxHealth(_health.maxHealth + 1, true);
                    break;
                case ItemType.QueensJelly:
                    _health.SetMaxHealth(_health.maxHealth + 1, false);
                    _health.Heal(1);
                    break;
                case ItemType.ChitinPlate:
                    ShieldCharges = Count(ItemType.ChitinPlate);
                    break;
                case ItemType.Umbrella:
                    UmbrellaOpen = true;
                    break;
                case ItemType.PaperHat:
                    // The price of the hat: a point of health, for good. Never the last one.
                    if (_health.maxHealth > 1) _health.SetMaxHealth(_health.maxHealth - 1, false);
                    break;
                case ItemType.FinchsDose:
                    // Smaller again: all of the Void, so less of it to hit. It stops at a little over half size.
                    transform.localScale = Vector3.one * Mathf.Max(0.55f, Mathf.Pow(DoseSize, Count(ItemType.FinchsDose)));
                    break;
                case ItemType.LarvaBuddy:
                    if (Buddy == null && GameAssets.Instance != null && GameAssets.Instance.larvaPrefab != null)
                    {
                        Buddy = Instantiate(GameAssets.Instance.larvaPrefab, transform.position - transform.forward, Quaternion.identity);
                        Buddy.owner = this;
                    }
                    break;
            }

            ShowLook(item);
            Synergies.Check(this, item.type);
            Changed?.Invoke();
        }

        // One look per slot: the newest item in a slot replaces the one before it.
        void ShowLook(ItemDefinition item)
        {
            if (item.visual == null || item.slot == ItemSlot.Companion || _player == null || _player.visual == null) return;
            if (_looks.TryGetValue(item.slot, out var previous) && previous != null) Destroy(previous);

            var look = Instantiate(item.visual, _player.visual);
            look.name = item.displayName;
            look.transform.localPosition = Vector3.zero;
            look.transform.localRotation = Quaternion.identity;
            _looks[item.slot] = look;

            if (CameraFollow.Instance != null) CameraFollow.Instance.RefreshTargetRenderers();
        }

        /// <summary>The look currently showing in a slot, or null.</summary>
        public GameObject LookIn(ItemSlot slot) => _looks.TryGetValue(slot, out var look) ? look : null;

        public void RechargeShield()
        {
            ShieldCharges = Count(ItemType.ChitinPlate);
            UmbrellaOpen = Count(ItemType.Umbrella) > 0;
            Changed?.Invoke();
        }

        /// <summary>Save States spent this run.</summary>
        public int SaveStatesUsed { get; private set; }
        /// <summary>True while the umbrella has not yet kept anything off in this room.</summary>
        public bool UmbrellaOpen { get; private set; }

        /// <summary>A blast or puddle is about to hurt the player: true if the umbrella takes it instead (once a room).</summary>
        public bool UseUmbrella()
        {
            if (!UmbrellaOpen || Count(ItemType.Umbrella) == 0) return false;
            UmbrellaOpen = false;
            _health.SetInvulnerable(0.5f);
            Sfx.Play(SfxId.Blocked);
            Changed?.Invoke();
            return true;
        }

        void OnRoomChanged(FloorManager floors)
        {
            if (floors.CurrentNode == _lastRoom) return;
            _lastRoom = floors.CurrentNode;
            RechargeShield();
        }

        void Update()
        {
            // Pheromone Cloud: every so often, enemies close to the player may turn on each other.
            int cloud = Count(ItemType.PheromoneCloud);
            if (cloud == 0 || Time.time < _nextPheromone) return;
            _nextPheromone = Time.time + pheromoneInterval;

            foreach (var enemy in Enemy.Active)
            {
                if (enemy.Health.IsDead || enemy.IsConfused) continue;
                if ((enemy.transform.position - transform.position).sqrMagnitude > pheromoneRadius * pheromoneRadius) continue;
                if (UnityEngine.Random.value < pheromoneChance * cloud) enemy.Confuse(3f);
            }
        }
    }
}
