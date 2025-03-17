using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ActiveWeapon))]
[RequireComponent(typeof(FireWeaponEvent))]
[RequireComponent(typeof(ReloadWeaponEvent))]
[RequireComponent(typeof(WeaponFireEvent))]

[DisallowMultipleComponent]
public class FireWeapon : MonoBehaviour
{
    private float firePreChargeTimer = 0f;
    private float fireRateCoolDownTimer = 0f;
    private ActiveWeapon activeWeapon;
    private FireWeaponEvent fireWeaponEvent;
    private ReloadWeaponEvent reloadWeaponEvent;
    private WeaponFireEvent weaponFireEvent;

    private void Awake()
    {
        activeWeapon = GetComponent<ActiveWeapon>();
        fireWeaponEvent = GetComponent<FireWeaponEvent>();
        reloadWeaponEvent = GetComponent<ReloadWeaponEvent>();
        weaponFireEvent = GetComponent<WeaponFireEvent>();
    }

    private void OnEnable()
    {
        fireWeaponEvent.OnFireWeapon += FireWeaponEvent_OnFireWeapon;
    }

    private void OnDisable()
    {
        fireWeaponEvent.OnFireWeapon -= FireWeaponEvent_OnFireWeapon;
    }

    private void Update()
    {
        //Decrease cooldown timer.
        fireRateCoolDownTimer -= Time.deltaTime;
        //Debug.Log(firePreChargeTimer);
    }

    /// <summary>
    /// Handle fire weapon event 处理开火武器事件
    /// </summary>
    /// <param name="fireWeaponEvent"></param>
    /// <param name="fireWeaponEventArgs"></param>
    private void FireWeaponEvent_OnFireWeapon(FireWeaponEvent fireWeaponEvent, FireWeaponEventArgs fireWeaponEventArgs)
    {
        WeaponFire(fireWeaponEventArgs);
    }

    /// <summary>
    /// Fire weapon
    /// </summary>
    /// <param name="fireWeaponEventArgs"></param>
    private void WeaponFire(FireWeaponEventArgs fireWeaponEventArgs)
    {
        //Handle weapon precharge timer
        WeaponPreCharge(fireWeaponEventArgs);
        
        //weapon fire
        if (fireWeaponEventArgs.fire)
        {
            //Test if weapon is ready to fire
            if (IsWeaponReadyToFire())
            {
                FireAmmo(fireWeaponEventArgs.aimAngle, fireWeaponEventArgs.weaponAimAngle,
                    fireWeaponEventArgs.weaponAimDirectionVector);

                ResetCoolDownTimer();

                ResetPreChargeTimer();
            }
        }
    }

    /// <summary>
    /// Handle weapon precharge
    /// </summary>
    /// <param name="fireWeaponEventArgs"></param>
    private void WeaponPreCharge(FireWeaponEventArgs fireWeaponEventArgs)
    {
        //Weapon precharge
        if (fireWeaponEventArgs.firePreviousFrame)
        {
            //Decrease precharge timer if fire button held previous frame   如果上一帧按住开火按钮，则减少预充能计时器
            firePreChargeTimer -= Time.deltaTime;
        }
        else
        {
            //else reset the precharge timer    否则重置预充能计时器
            ResetPreChargeTimer();
        }
    }

    /// <summary>
    /// Returns true if the weapon is ready to fire, else returns false 如果武器做好开火准备返回true，否则返回false
    /// </summary>
    /// <returns></returns>
    private bool IsWeaponReadyToFire()
    {
        //if there is no ammo and weapon doesn't have infinite ammo return false 如果没有弹药且武器没有无限弹药，则返回 false
        if (activeWeapon.GetCurrentWeapon().weaponRemainingAmmo <= 0 &&
            !activeWeapon.GetCurrentWeapon().weaponDetails.hasInfiniteAmmo)
        {
            return false;
        }
        
        //if the weapon is reloading then return false  如果武器正在换弹，则返回 false
        if (activeWeapon.GetCurrentWeapon().isWeaponReloading)
        {
            return false;
        }
        
        //if the weapon isn't precharge or is cooling down then return false   如果武器不处于预充能状态或正在冷却，则返回 false
        if (firePreChargeTimer > 0f || fireRateCoolDownTimer > 0f) 
        {
            return false;
        }
        
        //if no ammo in the clip and the weapon doesn't have infinite clip capacity then return false
        //如果弹夹中没有弹药且武器没有无限弹夹容量，则返回 false
        if (!activeWeapon.GetCurrentWeapon().weaponDetails.hasInfiniteClipCapacity &&
            activeWeapon.GetCurrentWeapon().weaponClipRemainingAmmo <= 0)
        {
            //trigger a reload weapon event
            reloadWeaponEvent.CallReloadWeaponEvent(activeWeapon.GetCurrentWeapon(), 0);
            
            return false;
        }
        
        //weapon is ready to fire - return true;    武器已准备好开火 —— 返回true
        return true;
    }

    /// <summary>
    /// Set up ammo using an ammo gameobject and component from the object pool 使用对象池中的弹药游戏对象和组件来设置弹药
    /// </summary>
    /// <param name="aimAngle"></param>
    /// <param name="weaponAimAngle"></param>
    /// <param name="weaponAimDirectionVector"></param>
    private void FireAmmo(float aimAngle, float weaponAimAngle, Vector3 weaponAimDirectionVector)
    {
        AmmoDetailsSO currentAmmo = activeWeapon.GetCurrentAmmo();

        if (currentAmmo != null)
        {
            //Fire ammo routine
            StartCoroutine(FireAmmoRoutine(currentAmmo, aimAngle, weaponAimAngle, weaponAimDirectionVector));
        }
    }

    private IEnumerator FireAmmoRoutine(AmmoDetailsSO currentAmmo, float aimAngle, float weaponAimAngle,
        Vector3 weaponAimDirectionVector)
    {
        int ammoCounter = 0;
        
        //Get random ammo per shot  获取每次射击的随机弹药消耗量
        int ammoPershot = Random.Range(currentAmmo.ammoSpawnAmountMin, currentAmmo.ammoSpawnAmountMax);
        
        //Get random interval between ammo  获取弹药之间的随机间隔
        float ammoSpawnInterval;

        if (ammoPershot > 1)
        {
            ammoSpawnInterval = Random.Range(currentAmmo.ammoSpawnIntervalMin, currentAmmo.ammoSpawnIntervalMax);
        }
        else
        {
            ammoSpawnInterval = 0f;
        }
        
        //Loop for number of ammo per shot
        while (ammoCounter < ammoPershot) 
        {
            ammoCounter++;
            
            //Get ammo prefab from array
            GameObject ammoPrefab = currentAmmo.ammoPrefabArray[Random.Range(0, currentAmmo.ammoPrefabArray.Length)];
            
            //Get random speed value
            float ammoSpeed = Random.Range(currentAmmo.ammoSpeedMin, currentAmmo.ammoSpeedMax);
            
            //Get GameObject with IFireable component   获取带有 IFireable 组件的游戏对象
            IFireable ammo = (IFireable)PoolManager.Instance.ReuseComponent(ammoPrefab, activeWeapon.GetShootPosition(),
                Quaternion.identity);
            
            //Initialise Ammo
            ammo.InitialiseAmmo(currentAmmo, aimAngle, weaponAimAngle, ammoSpeed, weaponAimDirectionVector);
            
            //wait for ammo per shot timegap    等待每次射击的弹药时间间隔
            yield return new WaitForSeconds(ammoSpawnInterval);
        }
            
        //Reduce ammo clip count if not infinite clip capacity  如果没有无限弹夹容量，则减少弹夹中的弹药数量
        if (!activeWeapon.GetCurrentWeapon().weaponDetails.hasInfiniteClipCapacity)
        {
            activeWeapon.GetCurrentWeapon().weaponClipRemainingAmmo--;
            activeWeapon.GetCurrentWeapon().weaponRemainingAmmo--;
        }
            
        //Call weapon fired event   调用武器开火事件
        weaponFireEvent.CallWeaponFiredEvent(activeWeapon.GetCurrentWeapon());
        
        //Display weapon shoot effect
        WeaponShootEffect(aimAngle);
        
        //Weapon fired sound effect 武器开火音效
        WeaponSoundEffect();
        
        //Debug.Log("11111");
    }

    /// <summary>
    /// Reset cooldown timer 重置冷却时间
    /// </summary>
    private void ResetCoolDownTimer()
    {
        //Reset cooldown timer
        fireRateCoolDownTimer = activeWeapon.GetCurrentWeapon().weaponDetails.weaponFireRate;
    }

    /// <summary>
    /// Reset precharge timers  重置预充能计时器
    /// </summary>
    private void ResetPreChargeTimer()
    {
        //Reset precharge timer
        firePreChargeTimer = activeWeapon.GetCurrentWeapon().weaponDetails.weaponPrechargeTime;
    }

    /// <summary>
    /// Display the weapon shoot effect 显示武器射击效果
    /// </summary>
    /// <param name="aimAngle"></param>
    private void WeaponShootEffect(float aimAngle)
    {
        //Process if there is a shooot effect & prefab
        if (activeWeapon.GetCurrentWeapon().weaponDetails.weaponShootEffect != null &&
            activeWeapon.GetCurrentWeapon().weaponDetails.weaponShootEffect.weaponShootEffectPrefab != null)
        {
            //Get weapon shoot effect gameobject from the pool with particle system component
            //从对象池中获取带有粒子系统组件的武器射击效果游戏对象
            WeaponShootEffect weaponShootEffect = (WeaponShootEffect)PoolManager.Instance.ReuseComponent(
                activeWeapon.GetCurrentWeapon().weaponDetails.weaponShootEffect.weaponShootEffectPrefab,
                activeWeapon.GetShootEffectPosition(), Quaternion.identity);
            
            //set shoot effect
            weaponShootEffect.SetShootEffect(activeWeapon.GetCurrentWeapon().weaponDetails.weaponShootEffect, aimAngle);
            
            //Set gameobject active (the particle system is set automatically disable the gameobject once finished)
            //设置游戏对象为激活状态（粒子系统完成后会自动禁用该游戏对象）
            weaponShootEffect.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Play weapon shooting sound effect   播放武器射击音效
    /// </summary>
    private void WeaponSoundEffect()
    {
        if (activeWeapon.GetCurrentWeapon().weaponDetails.weaponFiringSoundEffect != null)
        {
            SoundEffectManager.Instance.PlaySoundEffect(activeWeapon.GetCurrentWeapon().weaponDetails
                .weaponFiringSoundEffect);
        }
    }
}
