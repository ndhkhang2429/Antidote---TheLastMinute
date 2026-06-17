using UnityEngine;
using StarterAssets;

public class FPSArmsController : MonoBehaviour
{
    private Animator _animator;
    private StarterAssetsInputs _input;

    // Parameters của GunAnimator
    private int _hashWalkSpeed = Animator.StringToHash("walkSpeed");
    private int _hashReloading = Animator.StringToHash("reloading");
    private int _hashChangingWeapon = Animator.StringToHash("changingWeapon");

    void Start()
    {
        _animator = GetComponent<Animator>();
        _input = GetComponentInParent<StarterAssetsInputs>();

        if (_input == null)
            _input = FindObjectOfType<StarterAssetsInputs>();
    }

    void Update()
    {
        if (_animator == null || _input == null) return;

        // Sync movement
        float speed = _input.move.magnitude;
        _animator.SetFloat(_hashWalkSpeed, speed);
    }

    public void TriggerReload(bool isReloading)
    {
        if (_animator != null)
            _animator.SetBool(_hashReloading, isReloading);
    }

    public void TriggerChangeWeapon()
    {
        if (_animator != null)
            StartCoroutine(ChangeWeaponAnim());
    }

    System.Collections.IEnumerator ChangeWeaponAnim()
    {
        _animator.SetBool(_hashChangingWeapon, true);
        yield return new WaitForSeconds(0.5f);
        _animator.SetBool(_hashChangingWeapon, false);
    }
}