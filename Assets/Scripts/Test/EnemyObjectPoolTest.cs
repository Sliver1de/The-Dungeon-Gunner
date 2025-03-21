using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyObjectPoolTest : MonoBehaviour
{
    [SerializeField] private EnemyAnimationDetails[] enemyAnimationDetailsArray;
    [SerializeField] private GameObject enemyExamplePrefab;
    private float timer = 1f;
    
    [System.Serializable]
    public struct EnemyAnimationDetails
    {
        public RuntimeAnimatorController AnimatorController;
        public Color spriteColor;
    }

    private void Update()
    {
        //Spawn random enemy sprite every second
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            GetEnemyExample();
            timer = 1f;
        }
    }

    private void GetEnemyExample()
    {
        Room currentRoom = GameManager.Instance.GetCurrentRoom();
        
        //random spawn position within room bounds
        Vector3 spawnPosition = new Vector3(Random.Range(currentRoom.lowerBounds.x, currentRoom.upperBounds.x),
            Random.Range(currentRoom.lowerBounds.y, currentRoom.upperBounds.y), 0f);

        EnemyAnimationTest enemyAnimation = (EnemyAnimationTest)PoolManager.Instance.ReuseComponent(enemyExamplePrefab,
            HelperUtilities.GetSpawnPositionNearestToPlayer(spawnPosition), Quaternion.identity);

        int randomIndex = Random.Range(0, enemyAnimationDetailsArray.Length);
        
        enemyAnimation.gameObject.SetActive(true);

        enemyAnimation.SetAnimation(enemyAnimationDetailsArray[randomIndex].AnimatorController,
            enemyAnimationDetailsArray[randomIndex].spriteColor);
    }
}
