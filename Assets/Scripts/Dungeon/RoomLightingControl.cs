using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(InstantiatedRoom))]
public class RoomLightingControl : MonoBehaviour
{
    private InstantiatedRoom instantiatedRoom;

    private void Awake()
    {
        instantiatedRoom = GetComponent<InstantiatedRoom>();
    }

    private void OnEnable()
    {
        StaticEventHandler.OnRoomChanged += StaticEventHandler_OnRoomChanged;
    }

    private void OnDisable()
    {
        StaticEventHandler.OnRoomChanged -= StaticEventHandler_OnRoomChanged;
    }

    /// <summary>
    /// Handle room changed event 处理房间变化事件
    /// </summary>
    /// <param name="roomChangedEventArgs"></param>
    private void StaticEventHandler_OnRoomChanged(RoomChangedEventArgs roomChangedEventArgs)
    {
        //if this is the room entered and the room isn't already lit, then fade in the room lighting
        //如果这是进入的房间且房间尚未被点亮，则淡入房间照明
        if (roomChangedEventArgs.room == instantiatedRoom.room && !instantiatedRoom.room.isLit)
        {
            //Fade in room 淡入房间照明
            FadeInRoomLighting();
            
            //Ensure room environment decoration game objects are activated     确保房间环境装饰的游戏对象被激活
            instantiatedRoom.ActivateEnvironmentGameObjects();
            
            //Fade in the environment decoration gameobjects lighting   淡入环境装饰游戏对象的灯光效果
            FadeInEnvironmentLighting();
            
            //Fade in the room doors lighting   淡入房间门的照明
            FadeInDoors();
            
            instantiatedRoom.room.isLit = true;
        }
    }

    private void FadeInRoomLighting()
    {
        //Fade in the lighting for the room tilemaps    淡入房间Tilemap的照明
        StartCoroutine(FadeInRoomLightingRoutine(instantiatedRoom));   
    }

    private IEnumerator FadeInRoomLightingRoutine(InstantiatedRoom instantiatedRoom)
    {
        //Create new material to fade in 创建用于淡入的新材质
        Material material = new Material(GameResources.Instance.variableLitShader);
        
        instantiatedRoom.groundTilemap.GetComponent<TilemapRenderer>().material = material;
        instantiatedRoom.decoration1Tilemap.GetComponent<TilemapRenderer>().material = material;
        instantiatedRoom.decoration2Tilemap.GetComponent<TilemapRenderer>().material = material;
        instantiatedRoom.frontTilemap.GetComponent<TilemapRenderer>().material = material;
        instantiatedRoom.minimapTilemap.GetComponent<TilemapRenderer>().material = material;

        for (float i = 0.05f; i <= 1f; i += Time.deltaTime / Settings.fadeInTime)
        {
            material.SetFloat("Alpha_Slider",i);
            yield return null;
        }
        
        //Set material back to lit material 将材质恢复为发光材质
        instantiatedRoom.groundTilemap.GetComponent<TilemapRenderer>().material = GameResources.Instance.litMaterial;
        instantiatedRoom.decoration1Tilemap.GetComponent<TilemapRenderer>().material = GameResources.Instance.litMaterial;
        instantiatedRoom.decoration2Tilemap.GetComponent<TilemapRenderer>().material = GameResources.Instance.litMaterial;
        instantiatedRoom.frontTilemap.GetComponent<TilemapRenderer>().material = GameResources.Instance.litMaterial;
        instantiatedRoom.minimapTilemap.GetComponent<TilemapRenderer>().material = GameResources.Instance.litMaterial;
        
        
    }

    /// <summary>
    /// Fade in the environment decoration game objects 淡入环境装饰游戏对象
    /// </summary>
    private void FadeInEnvironmentLighting()
    {
        //Create new material to fade in    创建一个新的材质以进行淡入效果
        Material material = new Material(GameResources.Instance.variableLitShader);
        
        //Get all environment components in room    获取房间中所有的环境组件
        Environment[] environmentComponents = GetComponentsInChildren<Environment>();
        
        //loop through
        foreach (Environment environmentComponent in environmentComponents)
        {
            if (environmentComponent.spriteRenderer != null)
            {
                environmentComponent.spriteRenderer.material = material;
            }
        }

        StartCoroutine(FadeInEnvironmentLightingRoutine(material, environmentComponents));
        
        //Debug.Log("Environment fade in");
    }

    /// <summary>
    /// Fade in the environmental decoration game objects coroutine 环境装饰游戏对象淡入协程
    /// </summary>
    /// <param name="material"></param>
    /// <param name="environmentComponents"></param>
    /// <returns></returns>
    private IEnumerator FadeInEnvironmentLightingRoutine(Material material, Environment[] environmentComponents)
    {
        //Gradually fade in the lighting    逐渐淡入光照
        for (float i = 0.05f; i <= 1f; i += Time.deltaTime / Settings.fadeInTime)
        {
            material.SetFloat("Alpha_Slider",i);
            yield return null;
        }
        
        //Set environment components material back to lit material  将环境组件的材质设置回到光照材质
        foreach (Environment environmentComponent in environmentComponents)
        {
            if (environmentComponent.spriteRenderer != null)
            {
                environmentComponent.spriteRenderer.material = GameResources.Instance.litMaterial;
            }
        }
    }

    /// <summary>
    /// Fade in the doors
    /// </summary>
    private void FadeInDoors()
    {
        Door[] doorArray = GetComponentsInChildren<Door>();

        foreach (Door door in doorArray)
        {
            DoorLightingControl doorLightingControl = door.GetComponentInChildren<DoorLightingControl>();
            
            doorLightingControl.FadeInDoor(door);
        }
    }
}
