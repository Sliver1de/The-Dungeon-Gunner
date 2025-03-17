using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WeaponShootEffect : MonoBehaviour
{
    private ParticleSystem shootEffectParticleSystem;

    private void Awake()
    {
        shootEffectParticleSystem = GetComponent<ParticleSystem>();
    }

    /// <summary>
    /// Set the Shoot Effect from the passed in WeaponShootEffectSO and aimAngle
    /// 根据传入的 WeaponShootEffectSO 和 aimAngle 设置射击效果
    /// </summary>
    /// <param name="shootEffect"></param>
    /// <param name="aimAngle"></param>
    public void SetShootEffect(WeaponShootEffectSO shootEffect, float aimAngle)
    {
        //Set shoot effect color gradient   设置射击效果的颜色渐变
        SetShootEffectColorGradient(shootEffect.colorGradient);
        
        //Set shoot effect particle system starting values  设置射击效果粒子系统的起始值
        SetShootEffectParticleStartingValues(shootEffect.duration, shootEffect.startParticleSize,
            shootEffect.startParticleSpeed, shootEffect.startLifetime, shootEffect.effectGravity,
            shootEffect.maxParticleNumber);
        
        //Set shoot effect particle system particle burst particle number   设置射击效果粒子系统的粒子爆发数量
        SetShootEffectParticleEmission(shootEffect.emissionRate, shootEffect.burstParticleNumber);
        
        //Set emitter rotation  设置发射器旋转角度
        SetEmitterRotation(aimAngle);
        
        //Set shoot effect particle sprite  设置射击效果粒子的精灵
        SetShootEffectParticleSprite(shootEffect.sprite);
        
        //Set shoot effect lifetime min and max velocities  设置射击效果粒子的最小和最大生命周期速度
        SetShootEffectVelocityOverLifeTime(shootEffect.velocityOverLifetimeMin, shootEffect.velocityOverLifetimeMax);
    }

    /// <summary>
    /// Set the shoot effect particle system color gradient 设置射击效果颜色渐变
    /// </summary>
    /// <param name="gradient"></param>
    private void SetShootEffectColorGradient(Gradient gradient)
    {
        //Set color gradient
        ParticleSystem.ColorOverLifetimeModule colorOverLifetimeModule = shootEffectParticleSystem.colorOverLifetime;
        colorOverLifetimeModule.color = gradient;
    }

    /// <summary>
    /// Set shoot effect particle system starting value
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="startParticleSize"></param>
    /// <param name="startParticleSpeed"></param>
    /// <param name="startLifetime"></param>
    /// <param name="effectGravity"></param>
    /// <param name="maxParticles"></param>
    private void SetShootEffectParticleStartingValues(float duration, float startParticleSize, float startParticleSpeed,
        float startLifetime, float effectGravity, int maxParticles)
    {
        ParticleSystem.MainModule mainModule = shootEffectParticleSystem.main;
        
        //set particle system duration
        mainModule.duration = duration;
        
        //set particle start size
        mainModule.startSize = startParticleSize;
        
        //set particle start speed
        mainModule.startSpeed = startParticleSpeed;
        
        //set particle start lifetime
        mainModule.startLifetime = startLifetime;
        
        //set particle starting gravity
        mainModule.gravityModifier = effectGravity;
        
        //set max particles
        mainModule.maxParticles = maxParticles;
    }

    /// <summary>
    /// Set shoot effect particle system particle burst particle number
    /// </summary>
    /// <param name="emissionRate"></param>
    /// <param name="burstParticleNumber"></param>
    private void SetShootEffectParticleEmission(int emissionRate, float burstParticleNumber)
    {
        ParticleSystem.EmissionModule emissionModule = shootEffectParticleSystem.emission;
        
        //set particle burst number 设置粒子爆发数量
        ParticleSystem.Burst burst = new ParticleSystem.Burst(0f, burstParticleNumber);
        emissionModule.SetBurst(0,burst);
        
        //Set particle emission rate    设置粒子的发射速率
        emissionModule.rateOverTime = emissionRate;
    }

    /// <summary>
    /// Set shoot effect particle system sprite
    /// </summary>
    /// <param name="sprite"></param>
    private void SetShootEffectParticleSprite(Sprite sprite)
    {
        ParticleSystem.TextureSheetAnimationModule textureSheetAnimationModule =
            shootEffectParticleSystem.textureSheetAnimation;

        textureSheetAnimationModule.SetSprite(0, sprite);
    }

    /// <summary>
    /// Set the rotation of the emitter to match the aim angle
    /// </summary>
    /// <param name="angle"></param>
    private void SetEmitterRotation(float aimAngle)
    {
        transform.eulerAngles = new Vector3(0f, 0f, aimAngle);
    }

    /// <summary>
    /// Set the shoot effect velocity over lifetime
    /// </summary>
    /// <param name="minVelocity"></param>
    /// <param name="maxVelocity"></param>
    private void SetShootEffectVelocityOverLifeTime(Vector3 minVelocity, Vector3 maxVelocity)
    {
        ParticleSystem.VelocityOverLifetimeModule velocityOverLifetimeModule =
            shootEffectParticleSystem.velocityOverLifetime;
        
        //Define min max X velocity
        ParticleSystem.MinMaxCurve minMaxCurveX = new ParticleSystem.MinMaxCurve();
        minMaxCurveX.mode = ParticleSystemCurveMode.TwoConstants;
        minMaxCurveX.constantMin = minVelocity.x;
        minMaxCurveX.constantMax = maxVelocity.x;
        velocityOverLifetimeModule.x = minMaxCurveX;
        
        //Define min max Y velocity
        ParticleSystem.MinMaxCurve minMaxCurveY = new ParticleSystem.MinMaxCurve();
        minMaxCurveY.mode = ParticleSystemCurveMode.TwoConstants;
        minMaxCurveY.constantMin = minVelocity.y;
        minMaxCurveY.constantMax = maxVelocity.y;
        velocityOverLifetimeModule.y = minMaxCurveY;
        
        //Define min max Z velocity
        ParticleSystem.MinMaxCurve minMaxCurveZ = new ParticleSystem.MinMaxCurve();
        minMaxCurveZ.mode = ParticleSystemCurveMode.TwoConstants;
        minMaxCurveZ.constantMin = minVelocity.z;
        minMaxCurveZ.constantMax = maxVelocity.z;
        velocityOverLifetimeModule.z = minMaxCurveZ;
    }
}
