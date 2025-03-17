using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Enemy))]
[DisallowMultipleComponent]
public class EnemyWeaponAI : MonoBehaviour
{
    #region Tooltip

    [Tooltip("Select the layers that the enemy bullets will hit")]

    #endregion

    [SerializeField]
    private LayerMask layerMask;

    #region Tooltip

    [Tooltip("Populate this with the WeaponShootPosition child gameobject transform")]

    #endregion

    [SerializeField]
    private Transform weaponShootPosition;

    private Enemy enemy;
    private EnemyDetailsSO enemyDetails;
    private float firingIntervalTimer;
    private float firingDurationTimer;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        enemyDetails = enemy.enemyDetails;

        firingIntervalTimer = WeaponShootInterval();
        firingDurationTimer = WeaponShootDuration();
    }

    private void Update()
    {
        //Update timer
        firingIntervalTimer -= Time.deltaTime;
        
        //Interval Timer
        if (firingIntervalTimer < 0)
        {
            if (firingDurationTimer >= 0)
            {
                firingDurationTimer -= Time.deltaTime;

                FireWeapon();
            }
            else
            {
                //Reset timers
                firingIntervalTimer = WeaponShootInterval();
                firingDurationTimer = WeaponShootDuration();
            }
        }
    }

    /// <summary>
    /// Calculate a random weapon shoot duration between the min and max values
    /// 从最小和最大值之间计算一个随机的武器开火持续时间
    /// </summary>
    /// <returns></returns>
    private float WeaponShootDuration()
    {
        return Random.Range(enemyDetails.firingDurationMin, enemyDetails.firingDurationMax);
    }

    /// <summary>
    /// Calculate a random weapon shoot interval between the min and max
    /// 从最小和最大值之间计算一个随机的武器开火间隔
    /// </summary>
    /// <returns></returns>
    private float WeaponShootInterval()
    {
        return Random.Range(enemyDetails.firingIntervalMin, enemyDetails.firingIntervalMax);
    }

    /// <summary>
    /// Fire the weapon     开火
    /// </summary>
    private void FireWeapon()
    {
        //Player distance 玩家距离
        Vector3 playerDirectionVector = GameManager.Instance.GetPlayer().GetPlayerPosition() - transform.position;
        
        //Calculate direction vector of player from weapon shoot position   计算从武器射击位置到玩家的方向向量
        Vector3 weaponDirection = GameManager.Instance.GetPlayer().GetPlayerPosition() - weaponShootPosition.position;
        
        //Get weapon to player angle    获取武器到玩家的角度
        float weaponAngleDegrees = HelperUtilities.GetAngleFromVector(weaponDirection);
        
        //Get enemy to player angle     获取敌人到玩家的角度
        float enemyAngleDegrees = HelperUtilities.GetAngleFromVector(playerDirectionVector);
        
        //Set enemy aim direction       设置敌人的瞄准方向
        AimDirection enemyAimDirection = HelperUtilities.GetAimDirection(enemyAngleDegrees);
        
        //Trigger weapon aim event      触发武器瞄准事件
        enemy.aimWeaponEvent.CallAimWeaponEvent(enemyAimDirection, enemyAngleDegrees, weaponAngleDegrees,
            weaponDirection);
        
        //Only fire if enemy has a weapon   只有在敌人有武器的情况下才开火
        if (enemyDetails.enemyWeapon != null)
        {
            //Get ammo range    获取弹药范围
            float enemyAmmoRange = enemyDetails.enemyWeapon.weaponCurrentAmmo.ammoDamage;
            
            //Is the player in range    玩家是否在射程内
            if (playerDirectionVector.magnitude <= enemyAmmoRange)
            {
                //Does this enemy require line of sight to the player before firing?
                //这个敌人是否需要在开火前与玩家保持视线？
                if (enemyDetails.firingLineOfSightRequired &&
                    !IsPlayerInLineOfSight(weaponDirection, enemyAmmoRange)) return;
                
                //Trigger fire weapon event     触发开火武器事件
                enemy.fireWeaponEvent.CallFireWeaponEvent(true, true, enemyAimDirection, enemyAngleDegrees,
                    weaponAngleDegrees, weaponDirection);
            }
        }
    }

    private bool IsPlayerInLineOfSight(Vector3 weaponDirection, float enemyAmmoRange)
    {
        RaycastHit2D raycastHit2D = Physics2D.Raycast(weaponShootPosition.position, (Vector2)weaponDirection,
            enemyAmmoRange, layerMask);

        if (raycastHit2D && raycastHit2D.transform.CompareTag(Settings.playerTag))
        {
            return true;
        }
        
        return false;
    }

    #region Validation

#if UNITY_EDITOR
    private void OnValidate()
    {
        HelperUtilities.ValidateCheckNullValue(this, nameof(weaponShootPosition), weaponShootPosition);
    }
#endif

    #endregion
}
