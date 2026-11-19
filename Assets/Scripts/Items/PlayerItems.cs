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
            // Chitin Plate: a shell that soaks up the first hit in every room.
            _health.ShouldBlock = direction =>
            {
                if (ShieldCharges <= 0) return false;
                ShieldCharges--;
                _health.SetInvulnerable(0.5f);
                Sfx.Play(SfxId.Blocked);
                Changed?.Invoke();
                return true;
            };
        }

        public int Count(ItemType type) => _counts.TryGetValue(type, out int count) ? count : 0;

        public PlayerStats Stats
        {
            get
            {
                int legs = Count(ItemType.ExtraLegs), wings = Count(ItemType.WingBuds), jelly = Count(ItemType.QueensJelly);
                int marble = Count(ItemType.LostMarble), sprinkles = Count(ItemType.Sprinkles), sock = Count(ItemType.StaticSock);
                int dose = Count(ItemType.FinchsDose);
                bool overcharged = _power != null && _power.IsOvercharged;

                return new PlayerStats
                {
                    moveSpeedMultiplier = 1f + 0.15f * legs + 0.08f * jelly + 0.12f * dose,
                    dodgeCooldownMultiplier = Mathf.Pow(0.75f, legs) * Mathf.Pow(0.9f, jelly) * Mathf.Pow(0.85f, dose),
                    dodgeCharges = 1 + (legs > 0 && wings > 0 ? 1 : 0),
                    hoverDodge = wings > 0,
                    damageMultiplier = overcharged ? 2 : 1,
                    // A marble is heavy: harder hits, further apart. Sprinkles are the opposite.
                    fireIntervalMultiplier = Mathf.Pow(0.9f, jelly) * Mathf.Pow(1.35f, marble) * Mathf.Pow(0.85f, sprinkles) * (overcharged ? 0.67f : 1f),
                    damageBonus = marble,
                    // The sock: every fifth shot, then every fourth, never more often than every second.
                    chargedEvery = sock > 0 ? Mathf.Max(2, 6 - sock) : 0,
                    fireDodge = Count(ItemType.BirthdayCandle),
                    bounce = Count(ItemType.RubberBand),
                    chill = Count(ItemType.IceCube),
                    magnetMultiplier = 1f + 1.5f * Count(ItemType.FridgeMagnet),
                    powerFillMultiplier = 1f + 0.35f * Count(ItemType.SpareBattery),
                    crumbMultiplier = 1f + Count(ItemType.Lunchbox),
                    pierce = 2 * Count(ItemType.NeedleTip) + (overcharged ? 99 : 0),
                    split = Count(ItemType.SplitterMandible),
                    acid = Count(ItemType.AcidGland),
                    burn = Count(ItemType.FireflyLantern),
                    critChance = 0.15f * Count(ItemType.CompoundEye) + 0.1f * jelly,
                    aimAssistBonusAngle = 10f * Count(ItemType.CompoundEye)
                };
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
                case ItemType.QueensJelly:
                    _health.SetMaxHealth(_health.maxHealth + 1, false);
                    _health.Heal(1);
                    break;
                case ItemType.ChitinPlate:
                    ShieldCharges = Count(ItemType.ChitinPlate);
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
            Changed?.Invoke();
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
