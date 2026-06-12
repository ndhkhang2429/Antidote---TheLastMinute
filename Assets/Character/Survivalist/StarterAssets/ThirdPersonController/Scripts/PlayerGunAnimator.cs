using StarterAssets;
using UnityEngine;

public class PlayerGunAnimator : MonoBehaviour
{
    private Animator _animator;
    private StarterAssetsInputs _input;

    int _hashIsAim;
    int _hashShoot;
    int _hashReload;
    int _hashWeaponType;

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _input = GetComponent<StarterAssetsInputs>();

        _hashIsAim = Animator.StringToHash("IsAim");
        _hashShoot = Animator.StringToHash("Shoot");
        _hashReload = Animator.StringToHash("Reload");
        _hashWeaponType = Animator.StringToHash("WeaponType");
    }

    void Update()
    {
        _animator.SetBool(_hashIsAim, _input.aim);
        _animator.SetBool(_hashShoot, _input.shoot);

        if (_input.reload)
        {
            _animator.SetTrigger(_hashReload);
            _input.reload = false;
        }
    }
}