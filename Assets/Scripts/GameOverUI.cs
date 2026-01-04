using Components;
using TMPro;
using Unity.NetCode;
using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private Color _winColor;
    [SerializeField] private Color _loseColor;

    private void Start()
    {
        DotsEventsMono.Instance.OnGameWinEvent += OnGameWinEvent; 

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

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
