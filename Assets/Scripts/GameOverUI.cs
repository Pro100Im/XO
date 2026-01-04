using Components;
using RPCs;
using System;
using TMPro;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private Color _winColor;
    [SerializeField] private Color _loseColor;
    [SerializeField] private Color _tieColor;
    [SerializeField] private Button _rematchButton;

    private void Awake()
    {
        _rematchButton.onClick.AddListener(Rematch);
    }

    private void Start()
    {
        DotsEventsMono.Instance.OnGameWinEvent += OnGameWinEvent; 
        DotsEventsMono.Instance.OnGameRematchEvent += OnGameRematchEvent;
        DotsEventsMono.Instance.OnGameTieEvent += OnGameTieEvent;

        Hide();
    }

    private void OnGameTieEvent(object sender, EventArgs e)
    {
        var entityManager = ClientServerBootstrap.ClientWorld.EntityManager;
        var entityQuery = entityManager.CreateEntityQuery(typeof(GameClientDataComponent));
        var gameClientData = entityQuery.GetSingleton<GameClientDataComponent>();

        _resultText.color = _tieColor;
        _resultText.text = "Tie!";

        Show();
    }

    private void OnGameRematchEvent(object sender, EventArgs e)
    {
        Hide();
    }

    private void OnGameWinEvent(object sender, DotsEventsMono.OnWinnerEventArgs e)
    {
        var entityManager = ClientServerBootstrap.ClientWorld.EntityManager;
        var entityQuery = entityManager.CreateEntityQuery(typeof(GameClientDataComponent));
        var gameClientData = entityQuery.GetSingleton<GameClientDataComponent>();

        _resultText.color = (e.Winner == gameClientData.PlayerType) ? _winColor : _loseColor;
        _resultText.text = (e.Winner == gameClientData.PlayerType) ? "You Win!" : "You Lose!";

        Show();
    }

    private void Rematch()
    {
        var entityManager = ClientServerBootstrap.ClientWorld.EntityManager;

        entityManager.CreateEntity(typeof(RematchRPC), typeof(SendRpcCommandRequest));
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
