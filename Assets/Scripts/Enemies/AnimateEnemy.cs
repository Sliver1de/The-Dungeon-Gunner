using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
[DisallowMultipleComponent]
public class AnimateEnemy : MonoBehaviour
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        enemy.movementToPositionEvent.OnMovementToPosition += MovementToPositionEvent_OnMovementToPosition;

        enemy.idleEvent.OnIdle += IdleEvent_OnIdle;

        enemy.aimWeaponEvent.OnWeaponAim += AimWeaponEvent_OnWeaponAim;
    }

    private void OnDisable()
    {
        enemy.movementToPositionEvent.OnMovementToPosition -= MovementToPositionEvent_OnMovementToPosition;

        enemy.idleEvent.OnIdle -= IdleEvent_OnIdle;

        enemy.aimWeaponEvent.OnWeaponAim -= AimWeaponEvent_OnWeaponAim;
    }

    /// <summary>
    /// On weapon aim event handler     在武器瞄准事件处理程序
    /// </summary>
    /// <param name="aimWeaponEvent"></param>
    /// <param name="aimWeaponEventArgs"></param>
    private void AimWeaponEvent_OnWeaponAim(AimWeaponEvent aimWeaponEvent, AimWeaponEventArgs aimWeaponEventArgs)
    {
        InitialiseAnimationParameters();
        SetAimWeaponAnimationParameters(aimWeaponEventArgs.aimDirection);
    }

    /// <summary>
    /// On movement event handler
    /// </summary>
    /// <param name="movementToPositionEvent"></param>
    /// <param name="args"></param>
    private void MovementToPositionEvent_OnMovementToPosition(MovementToPositionEvent movementToPositionEvent,
        MovementToPositionArgs args)
    {
        // if (enemy.transform.position.x < GameManager.Instance.GetPlayer().transform.position.x)
        // {
        //     SetAimWeaponAnimationParameters(AimDirection.Right);
        // }
        // else
        // {
        //     SetAimWeaponAnimationParameters(AimDirection.Left);
        // }

        SetMovementAnimationParameters();
    }

    /// <summary>
    /// On idle event handler
    /// </summary>
    /// <param name="idleEvent"></param>
    private void IdleEvent_OnIdle(IdleEvent idleEvent)
    {
        SetIdleAnimationParameters();
    }

    private void InitialiseAnimationParameters()
    {
        enemy.animator.SetBool(Settings.aimUp, false);
        enemy.animator.SetBool(Settings.aimDown, false);
        enemy.animator.SetBool(Settings.aimLeft, false);
        enemy.animator.SetBool(Settings.aimRight, false);
        enemy.animator.SetBool(Settings.aimUpLeft, false);
        enemy.animator.SetBool(Settings.aimUpRight, false);
    }

    /// <summary>
    /// Set movement animation parameters
    /// </summary>
    private void SetMovementAnimationParameters()
    {
        enemy.animator.SetBool(Settings.isIdle, false);
        enemy.animator.SetBool(Settings.isMoving, true);
    }

    /// <summary>
    /// Set idle animation parameters
    /// </summary>
    private void SetIdleAnimationParameters()
    {
        enemy.animator.SetBool(Settings.isMoving, false);
        enemy.animator.SetBool(Settings.isIdle, true);
    }
    
    /// <summary>
    /// Set aim animation parameters
    /// </summary>
    /// <param name="aimDirection"></param>
    private void SetAimWeaponAnimationParameters(AimDirection aimDirection)
    {
        // InitialiseAnimationParameters();
        
        //Set aim direction
        switch (aimDirection)
        {
            case AimDirection.Up:
                enemy.animator.SetBool(Settings.aimUp, true);
                break;
            case AimDirection.UpRight:
                enemy.animator.SetBool(Settings.aimUpRight, true);
                break;
            case AimDirection.UpLeft:
                enemy.animator.SetBool(Settings.aimUpLeft, true);
                break;
            case AimDirection.Right:
                enemy.animator.SetBool(Settings.aimRight, true);
                break;
            case AimDirection.Left:
                enemy.animator.SetBool(Settings.aimLeft, true);
                break;
            case AimDirection.Down:
                enemy.animator.SetBool(Settings.aimDown, true);
                break;
        }
    }
}
