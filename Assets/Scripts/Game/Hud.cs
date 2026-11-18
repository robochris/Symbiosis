using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Shrunk
{
    /// <summary>Everything drawn over the game: health, coins and samples, the dodge and hero power meters, and item messages.</summary>
    public class Hud : MonoBehaviour
    {
        [Tooltip("One per point of health. More are added automatically if max health goes up.")]
        public Image[] healthPips;
        public Text killsText;
        [Tooltip("Optional; only shown in scenes with a FloorManager.")]
        public Text floorText;
        public Text resourcesText;
        [Tooltip("Describes the item you are standing next to, and announces items you pick up.")]
        public Text promptText;
        [Tooltip("Radial fill over the dodge button; full when the dodge is ready.")]
        public Image dodgeFill;
        [Tooltip("Radial fill over the hero power button; full when it is ready.")]
        public Image powerFill;
        public Text powerLabel;
        [Tooltip("Keyboard hint, shown only when touch controls are off.")]
        public GameObject desktopHint;
        [Tooltip("Dot in the middle of the screen; shown in shoulder and first-person views.")]
        public Image crosshair;
        [Tooltip("Text on the camera button.")]
        public Text cameraLabel;
        public TouchControls touch;
        [Tooltip("In the lab: show banked samples instead of this run's.")]
        public bool lab;
        [Tooltip("Shown only while a boss is in play.")]
        public GameObject bossBar;
        public Image bossFill;
        public Text bossName;

        readonly List<Image> _pips = new List<Image>();
        int _shownKills = -1;
        int _shownFloor = -1;
        int _shownMode = -1;
        int _shownCoins = -1, _shownSamples = -1, _shownDepth = -1;
        Text _hintText;
        string _toast;
        float _toastUntil;

        void OnEnable()
        {
            Pickup.ItemCollected += OnItemCollected;
        }

        void OnDisable()
        {
            Pickup.ItemCollected -= OnItemCollected;
        }

        void Start()
        {
            _pips.AddRange(healthPips);
            if (desktopHint != null)
            {
                desktopHint.SetActive(touch == null || !touch.Active);
                _hintText = desktopHint.GetComponent<Text>();
            }
        }

        void OnItemCollected(ItemDefinition item)
        {
            Toast($"Got {item.displayName}: {item.description}", 3.5f);
        }

        /// <summary>Shows a message in the middle of the screen for a few seconds.</summary>
        public void Toast(string message, float seconds)
        {
            _toast = message;
            _toastUntil = Time.time + seconds;
        }

        void Update()
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                UpdateHealth(player.Health);
                if (dodgeFill != null) dodgeFill.fillAmount = player.DodgeReady01;
                UpdatePower(player.Power);
                UpdatePrompt(player);
            }

            if (bossBar != null)
            {
                BossEnemy boss = BossEnemy.InPlay;
                bool show = boss != null && !boss.Health.IsDead;
                if (bossBar.activeSelf != show) bossBar.SetActive(show);
                if (show)
                {
                    if (bossFill != null) bossFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(boss.Health01), 1f, 1f);
                    if (bossName != null) bossName.text = boss.Phase > 1 ? $"{boss.displayName}  ·  phase {boss.Phase}" : boss.displayName;
                }
            }

            var follow = CameraFollow.Instance;
            if (follow != null && _shownMode != (int)follow.mode)
            {
                _shownMode = (int)follow.mode;
                bool onTouch = touch != null && touch.Active;
                string name = CameraFollow.NameOf(follow.mode);
                if (crosshair != null) crosshair.enabled = follow.IsLookMode;
                if (cameraLabel != null) cameraLabel.text = onTouch ? "CAM: " + name : "V  camera: " + name;
                if (_hintText != null)
                {
                    _hintText.text = follow.IsLookMode
                        ? "WASD move  ·  mouse look (click to grab, Esc to release)  ·  hold click to fire  ·  Space dodge  ·  Q power  ·  V camera"
                        : "WASD move  ·  mouse aim  ·  hold click to fire  ·  Space dodge  ·  Q power  ·  V camera";
                }
            }

            var floors = FloorManager.Instance;
            if (floorText != null && floors != null && _shownFloor != floors.FloorNumber)
            {
                _shownFloor = floors.FloorNumber;
                floorText.text = "Floor " + _shownFloor;
            }

            if (killsText != null && _shownKills != RunState.Kills)
            {
                _shownKills = RunState.Kills;
                killsText.text = "Ants: " + _shownKills;
            }

            if (resourcesText != null && lab)
            {
                MetaSave meta = SaveSystem.Data.meta;
                if (_shownSamples != meta.samples || _shownCoins != meta.rareMaterials)
                {
                    _shownSamples = meta.samples;
                    _shownCoins = meta.rareMaterials;
                    resourcesText.text = meta.rareMaterials > 0
                        ? $"Samples {meta.samples}   ·   Rare materials {meta.rareMaterials}"
                        : $"Samples {meta.samples}";
                }
            }
            else if (resourcesText != null && (_shownCoins != RunState.Coins || _shownSamples != RunState.Samples || (floors != null && _shownDepth != floors.depth)))
            {
                _shownCoins = RunState.Coins;
                _shownSamples = RunState.Samples;
                _shownDepth = floors != null ? floors.depth : 1;
                float multiplier = RunDirector.RewardMultiplier(_shownDepth);
                resourcesText.text = multiplier > 1f
                    ? $"Coins {_shownCoins}   ·   Samples {_shownSamples} (×{multiplier:0.#})"
                    : $"Coins {_shownCoins}   ·   Samples {_shownSamples}";
            }
        }

        void UpdateHealth(Health health)
        {
            // Max health can grow mid-run (Sugar Cube): copy the last pip along the row as needed.
            while (_pips.Count > 0 && _pips.Count < health.maxHealth)
            {
                Image last = _pips[_pips.Count - 1];
                Image extra = Instantiate(last, last.transform.parent);
                extra.name = "Health " + (_pips.Count + 1);
                float step = _pips.Count > 1 ? last.rectTransform.anchoredPosition.x - _pips[_pips.Count - 2].rectTransform.anchoredPosition.x : 70f;
                extra.rectTransform.anchoredPosition = last.rectTransform.anchoredPosition + new Vector2(step, 0f);
                _pips.Add(extra);
            }

            for (int i = 0; i < _pips.Count; i++)
            {
                if (_pips[i] == null) continue;
                _pips[i].gameObject.SetActive(i < health.maxHealth);
                _pips[i].enabled = i < health.Current;
            }
        }

        /// <summary>How many health pips are lit. For tests.</summary>
        public int LitPips
        {
            get
            {
                int lit = 0;
                foreach (var pip in _pips) if (pip != null && pip.gameObject.activeSelf && pip.enabled) lit++;
                return lit;
            }
        }

        void UpdatePower(HeroPower power)
        {
            if (powerFill == null) return;
            if (power == null)
            {
                powerFill.transform.parent.gameObject.SetActive(false);
                return;
            }

            // While it is running the ring drains; otherwise it fills with kills.
            powerFill.fillAmount = power.IsActive ? power.Remaining01 : power.Meter;
            if (powerLabel != null)
            {
                bool onTouch = touch != null && touch.Active;
                powerLabel.text = power.IsActive ? "ACTIVE" : power.IsReady ? (onTouch ? "READY" : "READY\nQ") : "POWER";
            }
        }

        void UpdatePrompt(PlayerController player)
        {
            if (promptText == null) return;

            if (Time.time < _toastUntil)
            {
                promptText.text = _toast;
                return;
            }

            Pickup near = Pickup.NearestItem(player.transform.position, 3.5f);
            if (near == null)
            {
                promptText.text = WorldPrompt.For(player.transform.position) ?? "";
                return;
            }

            string text = $"{near.item.displayName}: {near.item.description}";
            if (near.price > 0)
            {
                text += RunState.Coins >= near.price ? $"   ·   {near.price} coins (walk onto it to buy)" : $"   ·   {near.price} coins (you have {RunState.Coins})";
            }
            promptText.text = text;
        }
    }
}
