using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AmmoPattern : MonoBehaviour, IFireable
{
    #region Tooltip

    [Tooltip("Populate the array with the child ammo gameobjects")]

    #endregion

    [SerializeField]
    private Ammo[] ammoArray;

    private float ammoRange;
    private float ammoSpeed;
    private Vector3 fireDirectionVector;
    private float fireDirectionAngle;
    private AmmoDetailsSO ammoDetails;
    private float ammoChargeTimer;

    public GameObject GetGameObject()
    {
        return gameObject;
    }
    
    public void InitialiseAmmo(AmmoDetailsSO ammoDetails, float aimAngle, float weaponAimAngle, float ammoSpeed,
        Vector3 weaponAimDirectionVector, bool overrideAmmoMovement = false)
    {
        this.ammoDetails = ammoDetails;
        
        this.ammoSpeed = ammoSpeed;
        
        //Set fire direction    设置开火距离
        SetFireDirection(ammoDetails, aimAngle, weaponAimAngle, weaponAimDirectionVector);
        
        //Set ammo range    设置弹药范围
        ammoRange = ammoDetails.ammoRange;
        
        //Active ammo pattern gameobject    激活弹药模式游戏对象
        gameObject.SetActive(true);
        
        //loop through all child ammo and initialise it     遍历所有子弹药并初始化它们
        foreach (Ammo ammo in ammoArray)
        {
            ammo.InitialiseAmmo(ammoDetails, aimAngle, weaponAimAngle, ammoSpeed, weaponAimDirectionVector, true);
        }
        
        //Set ammo charge timer - this will hold the ammo briefly   设置弹药充能计时器——这将暂时保持弹药状态
        if (ammoDetails.ammoChargeTime > 0f)
        {
            ammoChargeTimer = ammoDetails.ammoChargeTime;
        }
        else
        {
            ammoChargeTimer = 0f;
        }
    }

    private void Update()
    {
        //Ammo charge effect
        if (ammoChargeTimer > 0f)
        {
            ammoChargeTimer -= Time.deltaTime;
            return;
        }
        
        //Calculate distance vector to move ammo    计算移动子弹的距离向量
        Vector3 distanceVector = fireDirectionVector * ammoSpeed * Time.deltaTime;

        transform.position += distanceVector;
        
        //Rotate ammo
        transform.Rotate(new Vector3(0f, 0f, ammoDetails.ammoRotationSpeed * Time.deltaTime));
        
        //Distance after max range reached  最大范围达到后的距离
        ammoRange -= distanceVector.magnitude;

        if (ammoRange < 0f)
        {
            DisableAmmo();
        }
    }
    
    /// <summary>
    /// Set ammo fire direction based on the input angle and direction adjusted by the random spread
    /// 根据输入方向，调整随机散布后的弹药射击方向和角度
    /// </summary>
    /// <param name="ammoDetails"></param>
    /// <param name="aimAngle"></param>
    /// <param name="weaponAimAngle"></param>
    /// <param name="weaponAimDirectionVector"></param>
    private void SetFireDirection(AmmoDetailsSO ammoDetails, float aimAngle, float weaponAimAngle,
        Vector3 weaponAimDirectionVector)
    {
        //Calculate random spread angle between min and max 计算最小值和最大值之间的随机散布角度
        float randomSpread = Random.Range(ammoDetails.ammoSpreadMin, ammoDetails.ammoSpreadMax);
        
        //Get a random spread toggle if 1 or -1 获取随机散布，切换为 1 或 -1
        int spreadToggle = Random.Range(0, 2) * 2 - 1;

        if (weaponAimDirectionVector.magnitude < Settings.useAimAngleDistance)
        {
            fireDirectionAngle = aimAngle;
        }
        else
        {
            fireDirectionAngle = weaponAimAngle;
        }
        
        //Adjust ammo fire angle by random spread   通过随机散布调整弹药射击角度
        fireDirectionAngle += spreadToggle * randomSpread;
        
        //Set ammo fire direction
        fireDirectionVector = HelperUtilities.GetDirectionVectorFromAngle(fireDirectionAngle);
    }

    /// <summary>
    /// Disable the ammo - thus returning it to the object pool     禁用弹药 —— 从而将其返回到对象池
    /// </summary>
    private void DisableAmmo()
    {
        //Disable the ammo pattern game object
        gameObject.SetActive(false);
    }

    #region Validation

#if UNITY_EDITOR
    private void OnValidate()
    {
        HelperUtilities.ValidateCheckEnumerableValues(this, nameof(ammoArray), ammoArray);
    }
#endif

    #endregion
}
