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

    private void DotsEventsMono_OnGameStartedEvent(object sender, EventArgs e)
    {
        var entityManager = ClientServerBootstrap.ClientWorld.EntityManager;
        var entityQuery = entityManager.CreateEntityQuery(typeof(GameClientData));
        var gameClientData = entityQuery.GetSingleton<GameClientData>();

        if(gameClientData.PlayerType == PlayerType.Cross)
        {
            crossYou.SetActive(true);
        }
        else
        {
            circleYou.SetActive(true); 
        }
    }
}
