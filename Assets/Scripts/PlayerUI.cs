using Components;
using System;
using Unity.NetCode;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject crossArrow;
    [SerializeField] private GameObject circleArrow;
    [Space]
    [SerializeField] private GameObject crossYou;
    [SerializeField] private GameObject circleYou;

    private void Awake()
    {
        crossArrow.SetActive(false);
        circleArrow.SetActive(false);
        crossYou.SetActive(false);
        circleYou.SetActive(false);
    }

    private void Start()
    {
        DotsEventsMono.Instance.OnGameStartedEvent += DotsEventsMono_OnGameStartedEvent;
    }

    private void Update()
    {
        UpdateCurrentPlayableArrow();
    }

    private void DotsEventsMono_OnGameStartedEvent(object sender, EventArgs e)
    {
        var entityManager = ClientServerBootstrap.ClientWorld.EntityManager;
        var entityQuery = entityManager.CreateEntityQuery(typeof(GameClientDataComponent));
        var gameClientData = entityQuery.GetSingleton<GameClientDataComponent>();

        if(gameClientData.PlayerType == PlayerType.Cross)
        {
            crossYou.SetActive(true);
        }
        else
        {
            circleYou.SetActive(true); 
        }
    }

    private void UpdateCurrentPlayableArrow() 
    {
        var entityManager = ClientServerBootstrap.ClientWorld.EntityManager;
        var entityQuery = entityManager.CreateEntityQuery(typeof(GameServerDataCmponent));

        if(!entityQuery.HasSingleton<GameServerDataCmponent>())
                return;

        var gameServerData = entityQuery.GetSingleton<GameServerDataCmponent>();

        if (gameServerData.CurrentPlayablePlayerType == PlayerType.Cross)
        {
            circleArrow.SetActive(false);
            crossArrow.SetActive(true);
        }
        else
        {
            crossArrow.SetActive(false);
            circleArrow.SetActive(true);
        }
    }
}
