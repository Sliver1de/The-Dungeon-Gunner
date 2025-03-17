using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class AmmoHitEffect : MonoBehaviour
{
        private ParticleSystem ammoHitEffectParticleSystem;

    private void Awake()
    {
        ammoHitEffectParticleSystem = GetComponent<ParticleSystem>();
    }

    /// <summary>
    /// Set the Ammo Hit Effect from the passed in AmmoHitEffectSO details
    /// 根据传入的 AmmoHitEffectSO 设置受击效果
    /// </summary>
    /// <param name="aimHitEffect"></param>
    public void SetHitEffect(AmmoHitEffectSO ammoHitEffect)
    {
        //Set shoot effect color gradient   设置受击效果的颜色渐变
        SetHitEffectColorGradient(ammoHitEffect.colorGradient);
        
        //Set shoot effect particle system starting values  设置受击效果粒子系统的起始值
        SetHitEffectParticleStartingValues(ammoHitEffect.duration, ammoHitEffect.startParticleSize,
            ammoHitEffect.startParticleSpeed, ammoHitEffect.startLifetime, ammoHitEffect.effectGravity,
            ammoHitEffect.maxParticleNumber);
        
        //Set shoot effect particle system particle burst particle number   设置受击效果粒子系统的粒子爆发数量
        SetHitEffectParticleEmission(ammoHitEffect.emissionRate, ammoHitEffect.burstParticleNumber);
        
        //Set shoot effect particle sprite  设置射击效果粒子的精灵
        SetHitEffectParticleSprite(ammoHitEffect.sprite);
        
        //Set shoot effect lifetime min and max velocities  设置射击效果粒子的最小和最大生命周期速度
        SetHitEffectVelocityOverLifeTime(ammoHitEffect.velocityOverLifetimeMin, ammoHitEffect.velocityOverLifetimeMax);
    }

    /// <summary>
    /// Set the shoot effect particle system color gradient 设置射击效果颜色渐变
    /// </summary>
    /// <param name="gradient"></param>
    private void SetHitEffectColorGradient(Gradient gradient)
    {
        //Set color gradient
        ParticleSystem.ColorOverLifetimeModule colorOverLifetimeModule = ammoHitEffectParticleSystem.colorOverLifetime;
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
    private void SetHitEffectParticleStartingValues(float duration, float startParticleSize, float startParticleSpeed,
        float startLifetime, float effectGravity, int maxParticles)
    {
        ParticleSystem.MainModule mainModule = ammoHitEffectParticleSystem.main;
        
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
    private void SetHitEffectParticleEmission(int emissionRate, float burstParticleNumber)
    {
        ParticleSystem.EmissionModule emissionModule = ammoHitEffectParticleSystem.emission;
        
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
    private void SetHitEffectParticleSprite(Sprite sprite)
    {
        ParticleSystem.TextureSheetAnimationModule textureSheetAnimationModule =
            ammoHitEffectParticleSystem.textureSheetAnimation;

        textureSheetAnimationModule.SetSprite(0, sprite);
    }

    /// <summary>
    /// Set the shoot effect velocity over lifetime
    /// </summary>
    /// <param name="minVelocity"></param>
    /// <param name="maxVelocity"></param>
    private void SetHitEffectVelocityOverLifeTime(Vector3 minVelocity, Vector3 maxVelocity)
    {
        ParticleSystem.VelocityOverLifetimeModule velocityOverLifetimeModule =
            ammoHitEffectParticleSystem.velocityOverLifetime;
        
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
