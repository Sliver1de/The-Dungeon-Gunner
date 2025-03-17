using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HealthEvent))]
[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    #region Header References

    [Space(10)]
    [Header("References")]

    #endregion

    #region Tooltip
    //填充 HealthBar 组件到 HealthBar 游戏对象
    [Tooltip("Populate with the HealthBar component on the HealthBar gameobject")]

    #endregion

    [SerializeField]
    private HealthBar healthBar;
    
    private int startingHealth;
    private int currentHealth;
    private HealthEvent healthEvent;
    private Player player;
    private Coroutine immunityCoroutine;
    private bool isImmuneAfterHit = false;
    private float immunityTime = 0f;
    private SpriteRenderer spriteRenderer = null;
    private const float spriteFlashInterval = 0.2f;
    private WaitForSeconds waitForSecondsSpriteFlashInterval = new WaitForSeconds(spriteFlashInterval);
    
    [HideInInspector] public bool isDamageable = true;
    [HideInInspector] public Enemy enemy;

    private void Awake()
    {
        healthEvent = GetComponent<HealthEvent>();
    }

    private void Start()
    {
        //Trigger a health event for UI update      触发生命值事件以更新 UI
        CallHealthEvent(0);
        
        //Attempt to load enemy / player components     尝试加载敌人/玩家组件
        player = GetComponent<Player>();
        enemy = GetComponent<Enemy>();
        
        //Get player / enemy hit immunity details       获取玩家/敌人受击免疫详情
        if (player != null)
        {
            if (player.playerDetails.isImmuneAfterHit)
            {
                isImmuneAfterHit = true;
                immunityTime = player.playerDetails.hitImmunityTime;
                spriteRenderer = player.spriteRenderer;
            }
        }
        else if (enemy != null)
        {
            if (enemy.enemyDetails.isImmuneAfterHit)
            {
                isImmuneAfterHit = true;
                immunityTime = enemy.enemyDetails.hitImmunityTime;
                spriteRenderer = enemy.spriteRendererArray[0];
            }
        }
        
        //Enable the health bar if required     如果需要，启用血条。
        if (enemy != null && enemy.enemyDetails.isHealthBarDisplayed == true && healthBar != null)
        {
            healthBar.EnableHealthBar();
        }
        else if (healthBar != null)
        {
            healthBar.DisableHealthBar();
        }
    }

    /// <summary>
    /// Public Method called when damage is taken
    /// </summary>
    /// <param name="damageAmount"></param>
    public void TakeDamage(int damageAmount)
    {
        bool isRolling = false;

        if (player != null)
        {
            isRolling = player.playerControl.isPlayerRolling;
        }

        if (isDamageable && !isRolling)
        {
            currentHealth -= damageAmount;
            CallHealthEvent(damageAmount);

            PostHitImmunity();
            
            //Set health bar as the percentage of health remaining  将血条设置为剩余生命值的百分比
            if (healthBar != null)
            {
                healthBar.SetHealthBarValue((float)currentHealth / (float)startingHealth);
            }
        }

        // if (isDamageable && isRolling)
        // {
        //     Debug.Log("Dodged Bullet By Rolling");
        // }
        //
        // if (!isDamageable && !isRolling)
        // {
        //     Debug.Log("Avoid Damage Due To Immunity");
        // }
    }

    /// <summary>
    /// Indicate a hit and give some post hit immunity  指示一次击中并给予一些击后免疫。
    /// </summary>
    private void PostHitImmunity()
    {
        //Check if gameobject is active - if not return 检查游戏对象是否处于激活状态，如果不是，则返回
        if (gameObject.activeSelf == false)
        {
            return;
        }
        
        //If there is post hit immunity then        如果有击中后的免疫期，那么
        if (isImmuneAfterHit)
        {
            if (immunityCoroutine != null)
            {
                StopCoroutine(immunityCoroutine);
            }
            
            //flash red and give period of immunity     闪烁红色并给与一段免疫期
            immunityCoroutine = StartCoroutine(PostHitImmunityRoutine(immunityTime, spriteRenderer));
        }
    }

    /// <summary>
    /// Coroutine to indicate a hit and give some post hit immunity     击中并给予一定的后击免疫时间的协程
    /// </summary>
    /// <param name="immunityTime"></param>
    /// <param name="spriteRenderer"></param>
    /// <returns></returns>
    private IEnumerator PostHitImmunityRoutine(float immunityTime, SpriteRenderer spriteRenderer)
    {
        int iterations = Mathf.RoundToInt(immunityTime / spriteFlashInterval / 2f);

        isDamageable = false;

        while (iterations > 0)
        {
            spriteRenderer.color = Color.red;

            yield return waitForSecondsSpriteFlashInterval;
            
            spriteRenderer.color=Color.white;
            
            yield return waitForSecondsSpriteFlashInterval;

            iterations--;

            yield return null;
        }

        isDamageable = true;
    }

    private void CallHealthEvent(int damageAmount)
    {
        //Trigger health event
        healthEvent.CallHealthChangedEvent(((float)currentHealth / (float)startingHealth), currentHealth, damageAmount);
    }


    /// <summary>
    /// Set startingHealth
    /// </summary>
    /// <param name="startingHealth"></param>
    public void SetStartingHealth(int startingHealth)
    {
        this.startingHealth = startingHealth;
        currentHealth = startingHealth;
    }

    /// <summary>
    /// Get the starting health 获取初始血量
    /// </summary>
    /// <returns></returns>
    public int GetStartingHealth()
    {
        return startingHealth;
    }

    /// <summary>
    /// Increase health by specified percent    按指定百分比增加生命值
    /// </summary>
    /// <param name="healthPercent"></param>
    public void AddHealth(int healthPercent)
    {
        int healthIncrease = Mathf.RoundToInt((startingHealth * healthPercent) / 100f);

        int totalHealth = currentHealth + healthIncrease;

        if (totalHealth > startingHealth)
        {
            currentHealth = startingHealth;
        }
        else
        {
            currentHealth = totalHealth;
        }
        
        CallHealthEvent(0);
    }
}
