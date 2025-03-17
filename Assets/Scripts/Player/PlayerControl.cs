using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))]
[DisallowMultipleComponent]
public class PlayerControl : MonoBehaviour
{
    #region Tooltip
    //运动细节可脚本的对象包含运动细节，例如速度
    [Tooltip("MovementDetailsSO scriptable object containing movement details such as speed")]

    #endregion

    [SerializeField]
    private MovementDetailsSO movementDetails;
    
    #region Tooltip

    [Tooltip("The player WeaponShootPosition gameobject in the hierarchy")]

    #endregion

    //[SerializeField]
    //private Transform weaponShootPosition;

    private Player player;
    private bool leftMouseDownPreviousFrame = false;
    private int currentWeaponIndex = 1;
    private float moveSpeed;
    private Coroutine playerRollCoroutine;
    private WaitForFixedUpdate waitForFixedUpdate;
    private float playerRollCooldownTimer = 0f;
    private bool isPlayerMovementDisabled = false;
    
    [HideInInspector] public bool isPlayerRolling = false;

    private void Awake()
    {
        player = GetComponent<Player>();

        moveSpeed = movementDetails.GetMoveSpeed();
    }

    private void Start()
    {
        //Create waitforfixed update for use in coroutine   创建 等待固定更新 以便在协程中使用
        waitForFixedUpdate = new WaitForFixedUpdate();
        
        //Set Starting Weapon 设置初始武器
        SetStartingWeapon();
        
        //Set player animation speed 
        SetPlayerAnimationSpeed();
    }

    /// <summary>
    /// Set the player starting weapon  设置玩家初始武器
    /// </summary>
    private void SetStartingWeapon()
    {
        int index = 1;

        foreach (Weapon weapon in player.weaponList)
        {
            if (weapon.weaponDetails == player.playerDetails.startingWeapon)
            {
                SetWeaponByIndex(index);
                break;
            }

            index++;
        }
    }

    /// <summary>
    /// Set player animator speed to match movement speed   将玩家的动画器速度设置为与移动速度匹配
    /// </summary>
    private void SetPlayerAnimationSpeed()
    {
        //set animator speed to match movement speed
        player.animator.speed = moveSpeed / Settings.baseSpeedForPlayerAnimations;
    }

    private void Update()
    {
        //if player movement disabled then return   如果玩家移动被禁用则返回
        if (isPlayerMovementDisabled) return;
        
        //if player is rolling then return  如果玩家翻滚则返回
        if (isPlayerRolling) return;
        
        //Process the player movement input 处理玩家移动输入
        MovementInput();
        
        //Process the player weapon input 处理玩家武器输入
        WeaponInput();
        
        //Process player use item input  处理玩家使用物品的输入
        UseItemInput();
        
        //Player roll cooldown timer    玩家滚动冷却计时器
        PlayerRollCooldownTimer();
    }

    /// <summary>
    /// player movement input 玩家移动输入
    /// </summary>
    private void MovementInput()
    {
        //Get movement input
        float horizontalMovement = Input.GetAxisRaw("Horizontal");
        float verticalMovement = Input.GetAxisRaw("Vertical");
        bool rightMouseButtonDown = Input.GetMouseButtonDown(1);
        
        //Create a direction vector based on the input  根据输入创建方向向量
        Vector2 direction = new Vector2(horizontalMovement, verticalMovement);
        
        //Adjust distance diagonal movement (pythagoras approximation)  调整距离对角线运动（Pythagoras近似）
        if (horizontalMovement != 0f && verticalMovement != 0f)
        {
            direction *= 0.7f;
        }
        
        //if there is movement either move or roll  如果有移动或滚动
        if (direction != Vector2.zero)
        {
            if (!rightMouseButtonDown)
            {
                //trigger movement event 触发移动事件
                player.movementByVelocityEvent.CallMovementByVelocityEvent(direction, moveSpeed);
            }
            //else player roll if not cooling down  否则玩家滚动如果不冷却
            else if (playerRollCooldownTimer <= 0f)
            {
                PlayerRoll((Vector3)direction);
            }
        }
        //else trigger idle event 否则触发idle事件
        else
        {
            player.idleEvent.CallIdleEvent();
        }
    }

    private void PlayerRoll(Vector3 direction)
    {
        playerRollCoroutine = StartCoroutine(PlayerRollRoutine(direction));
    }

    private IEnumerator PlayerRollRoutine(Vector3 direction)
    {
        //minDistane used to decide when to exit coroutine loop 最小距离用于决定何时退出循环
        float minDistance = 0.2f;

        isPlayerRolling = true;

        Vector3 targetPosition = player.transform.position + (Vector3)direction * movementDetails.rollDistance;

        while (Vector3.Distance(player.transform.position, targetPosition) > minDistance)
        {
            player.movementToPositionEvent.CallMovementToPosition(targetPosition, player.transform.position,
                movementDetails.rollSpeed, direction, isPlayerRolling);
            
            //yield return wait for fixed update
            yield return waitForFixedUpdate;
        }
        
        isPlayerRolling = false;
        
        //Set cooldown timer  设置冷却时间
        playerRollCooldownTimer = movementDetails.rollCooldownTime;
        
        player.transform.position = targetPosition;
    }

    private void PlayerRollCooldownTimer()
    {
        if (playerRollCooldownTimer >= 0f)
        {
            playerRollCooldownTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Weapon input
    /// </summary>
    private void WeaponInput()
    {
        Vector3 weaponDirection;
        float weaponAngleDegrees;
        float playerAngleDegrees;
        AimDirection playerAimDirection;
        
        //Aim weapon input  瞄准武器输入
        AimWeaponInput(out weaponDirection, out weaponAngleDegrees, out playerAngleDegrees, out playerAimDirection);
        
        //Fire weapon input 开火武器输入
        FireWeaponInput(weaponDirection, weaponAngleDegrees, playerAngleDegrees, playerAimDirection);
        
        //Switch weapon input
        SwitchWeaponInput();
        
        //Reload weapon input 重新装填输入
        ReloadWeaponInput();
    }

    private void AimWeaponInput(out Vector3 weaponDirection, out float weaponAngleDegrees, out float playerAngleDegrees,
        out AimDirection playerAimDirection)
    {
        //Get mouse world position
        Vector3 mouseWorldPosition = HelperUtilities.GetMouseWorldPosition();
        
        //Calculate direction vector of mouse cursor from weapon shoot position 计算鼠标光标相对于武器射击位置的方向向量
        //weaponDirection = (mouseWorldPosition - weaponShootPosition.position);
        weaponDirection = (mouseWorldPosition - player.activeWeapon.GetShootPosition());
        
        //Calculate direction vector of mouse cursor from player transform position 计算鼠标光标相对于玩家 Transform 位置的方向向量
        Vector3 playerDirection = (mouseWorldPosition - transform.position);
        
        //Get weapon to cursor angle 获取武器到光标的角度
        weaponAngleDegrees = HelperUtilities.GetAngleFromVector(weaponDirection);
        
        //Get player to cursor angle 获取玩家到光标的角度
        playerAngleDegrees = HelperUtilities.GetAngleFromVector(playerDirection);
        
        //Set player aim direction 设置玩家瞄准方向
        playerAimDirection = HelperUtilities.GetAimDirection(playerAngleDegrees);
        
        //trigger weapon aim event 触发武器瞄准事件
        player.aimWeaponEvent.CallAimWeaponEvent(playerAimDirection, playerAngleDegrees, weaponAngleDegrees,
            weaponDirection);
    }

    private void FireWeaponInput(Vector3 weaponDirection, float weaponAngleDegrees, float playerAngleDegrees,
        AimDirection playerAimDirection)
    {
        //Fire when left mouse button is clicked    点击鼠标左键开火
        if (Input.GetMouseButton(0))
        {
            //Trigger fire weapon event
            player.fireWeaponEvent.CallFireWeaponEvent(true, leftMouseDownPreviousFrame, playerAimDirection,
                playerAngleDegrees, weaponAngleDegrees, weaponDirection);
            leftMouseDownPreviousFrame = true;
        }
        else
        {
            leftMouseDownPreviousFrame = false;
        }
    }

    private void SwitchWeaponInput()
    {
        //Switch weapon if mouse scroll wheel selected
        if (Input.mouseScrollDelta.y < 0f)
        {
            PreviousWeapon();
        }

        if (Input.mouseScrollDelta.y > 0f)
        {
            NextWeapon();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetWeaponByIndex(1);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetWeaponByIndex(1);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetWeaponByIndex(2);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetWeaponByIndex(3);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetWeaponByIndex(4);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SetWeaponByIndex(5);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SetWeaponByIndex(6);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SetWeaponByIndex(7);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            SetWeaponByIndex(8);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            SetWeaponByIndex(9);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            SetWeaponByIndex(10);
        }

        if (Input.GetKeyDown(KeyCode.Minus))
        {
            SetCurrentWeaponToFirstInTheList();
        }
    }
    
    private void SetWeaponByIndex(int weaponIndex)
    {
        if (weaponIndex - 1 < player.weaponList.Count)
        {
            currentWeaponIndex = weaponIndex;
            player.setActiveWeaponEvent.CallSetActiveWeaponEvent(player.weaponList[weaponIndex - 1]);
        }
    }

    private void NextWeapon()
    {
        currentWeaponIndex++;

        if (currentWeaponIndex > player.weaponList.Count)
        {
            currentWeaponIndex = 1;
        }
        
        SetWeaponByIndex(currentWeaponIndex);
    }

    private void PreviousWeapon()
    {
        currentWeaponIndex--;

        if (currentWeaponIndex < 1)
        {
            currentWeaponIndex = player.weaponList.Count;
        }
        
        SetWeaponByIndex(currentWeaponIndex);
    }

    private void ReloadWeaponInput()
    {
        Weapon currentWeapon = player.activeWeapon.GetCurrentWeapon();
        
        //if current weapon is reloading return
        if (currentWeapon.isWeaponReloading) return;
        
        //remaining ammo is less than clip capacity then return and not infinite ammo then return
        //如果剩余弹药少于弹夹容量且没有无限弹药，则返回。
        if (currentWeapon.weaponRemainingAmmo < currentWeapon.weaponDetails.weaponClipAmmoCapacity &&
            !currentWeapon.weaponDetails.hasInfiniteAmmo)
        {
            return;
        }
        
        //if ammo in clip equals clip capacity then return  如果弹夹中的弹药数量等于弹夹容量，则返回
        if (currentWeapon.weaponClipRemainingAmmo == currentWeapon.weaponDetails.weaponClipAmmoCapacity) return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            //Call the reload weapon event
            player.reloadWeaponEvent.CallReloadWeaponEvent(player.activeWeapon.GetCurrentWeapon(), 0);
        }
    }

    /// <summary>
    /// Use the nearest item within 2 unity units from the player   使用距离玩家最近且在 2 个 Unity 单位范围内的物品
    /// </summary>
    private void UseItemInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            float useItemRadius = 2f;
            
            //Get any 'Useable' item near the player    获取玩家附近的任何“可使用”物品
            Collider2D[] collider2DArray = Physics2D.OverlapCircleAll(player.GetPlayerPosition(), useItemRadius);
            
            //Loop through detected items to see if any are 'useable'   遍历检测到的物品，查看是否有任何物品是“可使用”的
            foreach (Collider2D collider2D in collider2DArray)
            {
                IUseable iUseable = collider2D.GetComponent<IUseable>();

                if (iUseable != null)
                {
                    iUseable.UseItem();
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //if collided with something stop player roll coroutine 如果发生碰撞则停止协程
        StopPlayerRollRoutine();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        //if in collision with something stop player roll coroutine 如果发生碰撞则停止协程
        StopPlayerRollRoutine();
    }

    private void StopPlayerRollRoutine()
    {
        if (playerRollCoroutine != null)
        {
            StopCoroutine(playerRollCoroutine);
            isPlayerRolling = false;
        }
    }

    /// <summary>
    /// Enable the player movement
    /// </summary>
    public void EnablePlayer()
    {
        isPlayerMovementDisabled = false;
    }

    /// <summary>
    /// Disable the player movement
    /// </summary>
    public void DisablePlayer()
    {
        isPlayerMovementDisabled = true;
        player.idleEvent.CallIdleEvent();
    }

    /// <summary>
    /// Set the current weapon to first in the player weapon list   将当前武器设置为玩家武器列表中的第一个
    /// </summary>
    private void SetCurrentWeaponToFirstInTheList()
    {
        //Create new temporary list 创建新的临时列表
        List<Weapon> tempWeaponList = new List<Weapon>();
        
        //Add the current weapon to first in the temp list  将当前武器添加到临时列表的首位
        Weapon currentWeapon = player.weaponList[currentWeaponIndex - 1];
        currentWeapon.weaponListPosition = 1;
        tempWeaponList.Add(currentWeapon);
        
        //Loop through existing weapon list and add - skipping current weapon   遍历现有武器列表并添加，跳过当前武器
        int index = 2;

        foreach (Weapon weapon in player.weaponList)
        {
            if (weapon == currentWeapon) continue;
            
            tempWeaponList.Add(weapon);
            weapon.weaponListPosition = index;
            index++;
        }
        
        //Assign new list   分配新列表
        player.weaponList = tempWeaponList;

        currentWeaponIndex = 1;
        
        //Set current weapon    设置当前武器
        SetWeaponByIndex(currentWeaponIndex);
    }

    #region Validation

#if UNITY_EDITOR
    private void OnValidate()
    {
        HelperUtilities.ValidateCheckNullValue(this, nameof(movementDetails), movementDetails);
    }
#endif

    #endregion
}
