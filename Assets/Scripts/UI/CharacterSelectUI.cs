using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class CharacterSelectUI : MonoBehaviour
{
    #region Tooltip
    //用子对象 CharacterSelector 填充此项
    [Tooltip("Populate this with the child CharacterSelector gameobject")]

    #endregion
    
    [SerializeField] private Transform characterSelector;

    #region Tooltip
    //用 PlayerNameInput 游戏对象上的 TextMeshPro 组件填充此项
    [Tooltip("Populate with the TextMeshPro component on the PlayerNameInput gameobject")]

    #endregion
    
    [SerializeField] private TMP_InputField playerNameInput;

    private List<PlayerDetailsSO> playerDetailsList;
    private GameObject playerSelectionPrefab;
    private CurrentPlayerSO currentPlayer;
    private List<GameObject> playerCharacterGameObjectList = new List<GameObject>();
    private Coroutine coroutine;
    private int selectedPlayerIndex = 0;
    private float offset = 4f;

    private void Awake()
    {
        playerSelectionPrefab = GameResources.Instance.playerSelectionPrefab;
        playerDetailsList = GameResources.Instance.playerDetailsList;
        currentPlayer = GameResources.Instance.currentPlayer;
    }

    private void Start()
    {
        //Instantiate player characters     实例化玩家角色
        for (int i = 0; i < playerDetailsList.Count; i++)
        {
            GameObject playerSelectionObject = Instantiate(playerSelectionPrefab, characterSelector);
            playerCharacterGameObjectList.Add(playerSelectionObject);
            playerSelectionObject.transform.localPosition = new Vector3((offset * i), 0f, 0f);
            PopulatePlayerDetails(playerSelectionObject.GetComponent<PlayerSelectionUI>(), playerDetailsList[i]);
        }
        
        playerNameInput.text = currentPlayer.playerName;
        
        //Initialise the current player     初始化当前玩家
        currentPlayer.playerDetails = playerDetailsList[selectedPlayerIndex];
    }

    /// <summary>
    /// Populate player character details for display   填充玩家角色详情以供显示
    /// </summary>
    /// <param name="playerSelection"></param>
    /// <param name="playerDetails"></param>
    private void PopulatePlayerDetails(PlayerSelectionUI playerSelection, PlayerDetailsSO playerDetails)
    {
        playerSelection.PlayerHandSpriteRenderer.sprite = playerDetails.playerHandSprite;
        playerSelection.PlayerHandNoWeaponSpriteRenderer.sprite = playerDetails.playerHandSprite;
        playerSelection.playerWeaponSpriteRenderer.sprite = playerDetails.startingWeapon.weaponSprite;
        playerSelection.animator.runtimeAnimatorController = playerDetails.runtimeAnimatorController;
    }

    /// <summary>
    /// Select next character - this method is called from the onClick event set in the inspector
    /// 选择下一个角色——此方法通过在检查器中设置的 onClick 事件调用
    /// </summary>
    public void NextCharacter()
    {
        if (selectedPlayerIndex >= playerDetailsList.Count - 1)
        {
            return;
        }

        selectedPlayerIndex++;
        
        currentPlayer.playerDetails = playerDetailsList[selectedPlayerIndex];

        MoveToSelectedCharacter(selectedPlayerIndex);
    }

    /// <summary>
    /// Select previous character - this method is called from the onClick event set in the inspector
    /// 选择上一个角色——此方法通过在检查器中设置的 onClick 事件调用
    /// </summary>
    public void PreviousCharacter()
    {
        if (selectedPlayerIndex == 0)
        {
            return;
        }

        selectedPlayerIndex--;
        
        currentPlayer.playerDetails = playerDetailsList[selectedPlayerIndex];

        MoveToSelectedCharacter(selectedPlayerIndex);
    }

    private void MoveToSelectedCharacter(int index)
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        coroutine = StartCoroutine(MoveToSelectedCharacterRoutine(index));
    }

    private IEnumerator MoveToSelectedCharacterRoutine(int index)
    {
        float currentLocalXPosition = characterSelector.localPosition.x;
        float targetLocalXPosition = index * offset * characterSelector.localScale.x * -1f;

        while (Mathf.Abs(currentLocalXPosition - targetLocalXPosition) > 0.01f)
        {
            currentLocalXPosition = Mathf.Lerp(currentLocalXPosition, targetLocalXPosition, Time.deltaTime * 10f);

            characterSelector.localPosition = new Vector3(currentLocalXPosition, characterSelector.localPosition.y, 0f);
            
            yield return null;
        }

        characterSelector.localPosition = new Vector3(targetLocalXPosition, characterSelector.localPosition.y, 0f);
    }

    /// <summary>
    /// Update player name - this method is called from the field changed event set in the inspector
    /// 更新玩家名称——此方法通过在检查器中设置的字段更改事件调用
    /// </summary>
    public void UpdatePlayerName()
    {
        playerNameInput.text = playerNameInput.text;

        currentPlayer.playerName = playerNameInput.text;
    }
}
