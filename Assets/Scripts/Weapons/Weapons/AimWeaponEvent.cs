using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Events;

[DisallowMultipleComponent]
public class AimWeaponEvent : MonoBehaviour
{
    public event Action<AimWeaponEvent, AimWeaponEventArgs> OnWeaponAim;

    public void CallAimWeaponEvent(AimDirection aimDirection, float aimAngle, float weaponAimAngle,
        Vector3 weaponAimDirectionVector)
    {
        OnWeaponAim?.Invoke(this,
            new AimWeaponEventArgs()
            {
                aimDirection = aimDirection, aimAngle = aimAngle, weaponAimAngle = weaponAimAngle,
                WeaponAinDirectionVector = weaponAimDirectionVector
            });
        
    }
}

public class AimWeaponEventArgs : EventArgs
{
    public AimDirection aimDirection;
    public float aimAngle;
    public float weaponAimAngle;
    public Vector3 WeaponAinDirectionVector;
}
