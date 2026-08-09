using UnityEngine;

public class OpeningTutorialHints : MonoBehaviour
{
    [Header("Tutorial Items")]
    [SerializeField] private ItemDataSO _flashlightData;
    [SerializeField] private WeaponDataSO _batData;
    [SerializeField] private WeaponDataSO _pistolData;

    [Header("Tutorial Zombie")]
    [SerializeField] private HealthSystem _tutorialZombieHealth;

    [Header("Zombie Zone Unlock")]
    [Tooltip("Zone 1 được bật sau khi player nhặt pistol.")]
    [SerializeField] private GameObject _firstZombieZone;

    private bool _firstZoneUnlocked;

    private bool _flashlightPickedUp;
    private bool _inventoryHintShown;
    private bool _flashlightAssigned;

    private bool _batPickedUp;
    private bool _batEquipped;

    private bool _tutorialZombieDead;

    private bool _pistolPickedUp;
    private bool _pistolEquipped;

    private void Start()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnHeldItemChanged += HandleHeldItemChanged;
        }

        if (_tutorialZombieHealth != null)
        {
            _tutorialZombieHealth.OnDeath += HandleTutorialZombieDeath;
        }
    }

    private void OnDestroy()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnHeldItemChanged -= HandleHeldItemChanged;
        }

        if (_tutorialZombieHealth != null)
        {
            _tutorialZombieHealth.OnDeath -= HandleTutorialZombieDeath;
        }
    }

    private void Update()
    {
        /*
         * Sau khi nhặt đèn pin, đợi player thật sự
         * mở inventory rồi mới chỉ cách kéo vào Slot 4.
         */
        if (_flashlightPickedUp &&
            !_inventoryHintShown &&
            InventoryUI.Instance != null &&
            InventoryUI.Instance.IsOpen)
        {
            _inventoryHintShown = true;

            ShowHint(
                "Drag the flashlight into Slot 4."
            );
        }
    }

    // Gọi từ WorldItem.onPickedUp của đèn pin.
    public void OnFlashlightPickedUp()
    {
        if (_flashlightPickedUp)
            return;

        _flashlightPickedUp = true;

        ShowHint(
            "Press [TAB] to open your inventory."
        );
    }

    // Gọi từ WorldItem.onPickedUp của cây gậy.
    public void OnBatPickedUp()
    {
        if (_batPickedUp)
            return;

        _batPickedUp = true;

        ShowHint(
            "Press [3] to equip the melee weapon."
        );
    }

    // Gọi từ WorldItem.onPickedUp của pistol.
    public void OnPistolPickedUp()
    {
        if (_pistolPickedUp)
            return;

        _pistolPickedUp = true;

        ShowHint(
            "Press [2] to equip the pistol."
        );

        UnlockFirstZombieZone();
    }

    private void UnlockFirstZombieZone()
    {
        if (_firstZoneUnlocked)
            return;

        _firstZoneUnlocked = true;

        if (_firstZombieZone != null)
        {
            _firstZombieZone.SetActive(true);

            Debug.Log(
                "[OpeningTutorial] Player đã nhặt pistol. Zone 1 được kích hoạt."
            );
        }
        else
        {
            Debug.LogWarning(
                "[OpeningTutorial] Chưa gán First Zombie Zone."
            );
        }
    }

    private void HandleHeldItemChanged(
        ItemDataSO heldItem)
    {
        if (heldItem == null)
            return;

        /*
         * MoveQuestItemToSlot4() cũng phát event này,
         * nên có thể phát hiện đèn pin đã được kéo vào slot.
         */
        if (!_flashlightAssigned &&
            heldItem == _flashlightData)
        {
            _flashlightAssigned = true;

            ShowHint(
                "Flashlight equipped. Press [4] to use it."
            );

            return;
        }

        if (!_batEquipped &&
            _batPickedUp &&
            heldItem == _batData)
        {
            _batEquipped = true;

            ShowHint(
                "Press [LMB] to attack. Keep your distance."
            );

            return;
        }

        if (!_pistolEquipped &&
            _pistolPickedUp &&
            heldItem == _pistolData)
        {
            _pistolEquipped = true;

            ShowHint(
                "Press [LMB] to fire. Press [R] to reload."
            );
        }
    }

    private void HandleTutorialZombieDeath()
    {
        if (_tutorialZombieDead)
            return;

        _tutorialZombieDead = true;

        ShowHint(
            "The zombie dropped a pistol and ammunition. Pick them up."
        );
    }

    private void ShowHint(string message)
    {
        if (NotificationUI.Instance != null)
        {
            NotificationUI.Instance.ShowNotification(
                message
            );
        }
        else
        {
            Debug.Log(
                $"[OpeningTutorial] {message}"
            );
        }
    }
    public void NotifyItemPickedUp(ItemDataSO item)
    {
        if (item == null)
            return;

        if (item == _flashlightData)
        {
            OnFlashlightPickedUp();
            return;
        }

        if (item == _batData)
        {
            OnBatPickedUp();
            return;
        }

        if (item == _pistolData)
        {
            OnPistolPickedUp();
        }
    }
}