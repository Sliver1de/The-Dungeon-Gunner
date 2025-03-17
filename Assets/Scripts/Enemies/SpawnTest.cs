using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnTest : MonoBehaviour
{
    //public RoomTemplateSO roomTemplateSO;
    private List<SpawnableObjectsByLevel<EnemyDetailsSO>> testLveelSpawnList;
    private RandomSpawnableObject<EnemyDetailsSO> randomEnemyHelperClass;
    //private GameObject instantiatedEnemy;
    private List<GameObject> instantiatedEnemyList = new List<GameObject>();

    private void OnEnable()
    {
        StaticEventHandler.OnRoomChanged += StaticEventHandler_OnRoomChanged;
    }

    private void OnDisable()
    {
        StaticEventHandler.OnRoomChanged -= StaticEventHandler_OnRoomChanged;
    }

    private void StaticEventHandler_OnRoomChanged(RoomChangedEventArgs roomChangedEventArgs)
    {
        //Destroy any spawned enemies
        if (instantiatedEnemyList != null && instantiatedEnemyList.Count > 0)
        {
            foreach (GameObject enemy in instantiatedEnemyList)
            {
                Destroy(enemy);
            }
        }

        RoomTemplateSO roomTemplate = DungeonBuilder.Instance.GetRoomTemplate(roomChangedEventArgs.room.templateID);

        if (roomTemplate != null)
        {
            testLveelSpawnList = roomTemplate.enemiesByLevelList;
            
            //Create RandomSpawnableObject helper class 创建随机可生成对象辅助类
            randomEnemyHelperClass = new RandomSpawnableObject<EnemyDetailsSO>(testLveelSpawnList);
        }
    }

    // private void Start()
    // {
    //     //Get test level spawn list from dungeon room template
    //     testLveelSpawnList = roomTemplateSO.enemiesByLevelList;
    //     
    //     //Create RandomSpawnableObject helper class
    //     randomEnemyHelperClass = new RandomSpawnableObject<EnemyDetailsSO>(testLveelSpawnList);
    // }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            // if (instantiatedEnemy != null)
            // {
            //     Destroy(instantiatedEnemy);
            // }

            EnemyDetailsSO enemyDetails = randomEnemyHelperClass.GetItem();

            if (enemyDetails != null)
            {
                instantiatedEnemyList.Add(Instantiate(enemyDetails.enemyPrefab,
                    HelperUtilities.GetSpawnPositionNearestToPlayer(HelperUtilities.GetMouseWorldPosition()),
                    Quaternion.identity));
            }
        }
    }
}
