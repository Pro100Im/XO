using Global;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button _createLobbyBtn;
    [SerializeField] private Button _joinLobbyBtn;

    private void Awake()
    {
        _joinLobbyBtn.onClick.AddListener(QuickMatch);
    }

    private void QuickMatch()
    {
        GameManager.Instance.StartGameAsync();
    }

    private void OnDestroy()
    {
        _joinLobbyBtn.onClick.RemoveListener(QuickMatch);
    }
}       
