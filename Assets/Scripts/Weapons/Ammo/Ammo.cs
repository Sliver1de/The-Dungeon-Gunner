using UnityEngine;

[DisallowMultipleComponent]
public class Ammo : MonoBehaviour, IFireable
{
    #region Tooltip

    [Tooltip("Populate with child TrailRenderer component")]

    #endregion

    [SerializeField]
    private TrailRenderer trailRenderer;

    private float ammoRange = 0f;   //the range of each ammo
    private float ammoSpeed;
    private Vector3 fireDirectionVector;
    private float fireDirectionAngle;
    private SpriteRenderer spriteRenderer;
    private AmmoDetailsSO ammoDetails;
    private float ammoChargeTimer;
    private bool isAmmoMaterialSet = false;
    private bool overrideAmmoMovement;
    private bool isColliding = false;

    private void Awake()
    {
        //cache sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        //Ammo charge effect
        if (ammoChargeTimer > 0f)
        {
            ammoChargeTimer -= Time.deltaTime;
            return;
        }
        else if (!isAmmoMaterialSet)
        {
            SetAmmoMaterial(ammoDetails.ammoMaterial);
            isAmmoMaterialSet = true;
        }

        //Don't move ammo if movement has been overriden - e.g. this ammo is part of an ammo pattern
        //如果弹药的移动已被重写（例如该弹药是弹药模式的一部分），则不要移动弹药
        if (!overrideAmmoMovement)
        {
            //Calculate distance vector to move ammo    计算移动弹药的距离向量
            Vector3 distanceVector = fireDirectionVector * ammoSpeed * Time.deltaTime;
            
            transform.position += distanceVector;
        
            //Disable after max range reached   达到最大范围后禁用
            ammoRange -= distanceVector.magnitude;

            if (ammoRange < 0f)
            {
                if (ammoDetails.isPlayerAmmo)
                {
                    //no multiplier
                    StaticEventHandler.CallMultiplierEvent(false);
                }

                DisableAmmo();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //If already colliding with something return    如果已经与某物碰撞则返回
        if (isColliding) return;
        
        //Deal Damage To Collision Object
        DealDamage(collision);
        
        //Show ammo hit effect
        AmmoHitEffect();
        
        DisableAmmo();
    }

    private void DealDamage(Collider2D collision)
    {
        Health health = collision.GetComponent<Health>();

        bool enemyHit = false;

        if (health != null)
        {
            //Set isColliding to prevent ammo dealing damage multiple times 设置 isColliding 以防止弹药多次造成伤害
            isColliding = true;
            
            health.TakeDamage(ammoDetails.ammoDamage);
            
            //Enemy hit
            if (health.enemy != null)
            {
                enemyHit = true;
            }
        }
        
        //If player ammo then update multiplier     如果玩家有弹药，则更新倍率
        if (ammoDetails.isPlayerAmmo)
        {
            if (enemyHit)
            {
                //multiplier
                StaticEventHandler.CallMultiplierEvent(true);
            }
            else
            {
                //no multiplier
                StaticEventHandler.CallMultiplierEvent(false);
            }
        }
    }

    /// <summary>
    /// Initialise the ammo being fired - using the ammodetails, the aimangle,weaponAngle,and weaponAimDirectionVector.
    /// if this ammo is part of a pattern the ammo movement can be overriden by setting overrideAmmoMovement to true
    /// 初始化正在射击的弹药 —— 使用 ammoDetails、aimAngle、weaponAngle、weaponAimDirectionVector。
    /// 如果该弹药是模式的一部分，弹药的移动可以通过将 overrideAmmoMovement 设置为 true 来覆盖。”
    /// </summary>
    /// <param name="ammoDetails"></param>
    /// <param name="aimAngle"></param>
    /// <param name="weaponAimAngle"></param>
    /// <param name="ammoSpeed"></param>
    /// <param name="weaponAimDirectionVector"></param>
    /// <param name="overrideAmmoMovement"></param>
    public void InitialiseAmmo(AmmoDetailsSO ammoDetails, float aimAngle, float weaponAimAngle, float ammoSpeed,
        Vector3 weaponAimDirectionVector, bool overrideAmmoMovement = false)
    {
        #region Ammo

        this.ammoDetails = ammoDetails;
        
        //Initialise isColliding
        isColliding = false;
        
        //Set fire Direction
        SetFireDirection(ammoDetails, aimAngle, weaponAimAngle, weaponAimDirectionVector);
        
        //Set ammo sprite
        spriteRenderer.sprite = ammoDetails.ammoSprite;
        
        //set initial ammo material depending on whether there is an ammo charge period 根据是否有弹药充能周期设置初始弹药材质
        if (ammoDetails.ammoChargeTime > 0f)
        {
            //Set ammo charge timer
            ammoChargeTimer = ammoDetails.ammoChargeTime;
            SetAmmoMaterial(ammoDetails.ammoChargeMaterial);
            isAmmoMaterialSet = false;
        }
        else
        {
            ammoChargeTimer = 0f;
            SetAmmoMaterial(ammoDetails.ammoMaterial);
            isAmmoMaterialSet = true;
        }
        
        //Set ammo range
        ammoRange = ammoDetails.ammoRange;
        
        //Set ammoSpeed
        this.ammoSpeed = ammoSpeed;
        
        //Override ammo movement
        this.overrideAmmoMovement = overrideAmmoMovement;
        
        //Active ammo gameobject
        gameObject.SetActive(true);

        #endregion

        #region Trail

        if (ammoDetails.isAmmoTrail)
        {
            trailRenderer.gameObject.SetActive(true);
            trailRenderer.emitting = true;   //拖尾粒子效果
            trailRenderer.material = ammoDetails.ammoTrailMaterial;
            trailRenderer.startWidth = ammoDetails.ammoTrailStartWidth;
            trailRenderer.endWidth = ammoDetails.ammoTrailEndWidth;
            trailRenderer.time = ammoDetails.ammoTrailTime;
        }
        else
        {
            trailRenderer.emitting = false;
            trailRenderer.gameObject.SetActive(false);
        }

        #endregion
    }

    /// <summary>
    /// Set ammo fire direction and angle based on the input angle and direction adjusted by the random spread
    /// 根据输入角度和方向，调整随机散布后的弹药射击方向和角度
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
        
        //Set ammo rotation
        transform.eulerAngles = new Vector3(0f, 0f, fireDirectionAngle);
        
        //Set ammo fire direction
        fireDirectionVector = HelperUtilities.GetDirectionVectorFromAngle(fireDirectionAngle);
    }

    /// <summary>
    /// Disable the ammo - thus returning it to the object pool
    /// </summary>
    private void DisableAmmo()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Display the ammo hit effect 显示弹药命中效果
    /// </summary>
    private void AmmoHitEffect()
    {
        //Process if a hit effect has been specified    处理是否已指定命中效果
        if (ammoDetails.ammoHitEffect != null && ammoDetails.ammoHitEffect.ammoHitEffectPrefab != null)
        {
            //Get ammo hit effect gameobject from the pool (with particle system component)
            //从对象池中获取弹药命中效果游戏对象（包含粒子系统组件）
            AmmoHitEffect ammoHitEffect =
                (AmmoHitEffect)PoolManager.Instance.ReuseComponent(ammoDetails.ammoHitEffect.ammoHitEffectPrefab,
                    transform.position, Quaternion.identity);
            
            //Set Hit Effect    设置命中效果
            ammoHitEffect.SetHitEffect(ammoDetails.ammoHitEffect);
            
            //Set gameobject active (the particle system is set to automatically disable the gameobject once finished)
            //设置游戏对象为激活状态（粒子系统完成后会自动禁用该游戏对象
            ammoHitEffect.gameObject.SetActive(true);
        }
    }

    public void SetAmmoMaterial(Material material)
    {
        spriteRenderer.material = material;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    #region Validation

#if UNITY_EDITOR
    private void OnValidate()
    {
        HelperUtilities.ValidateCheckNullValue(this,nameof(trailRenderer), trailRenderer);
    }
#endif

    #endregion
}
