using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimationTest : MonoBehaviour
{
    Animator animator;
    SpriteRenderer spriteRenderer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetAnimation(RuntimeAnimatorController animatorController, Color spriteColor)
    {
        animator.runtimeAnimatorController = animatorController;
        spriteRenderer.color = spriteColor;
    }
}
