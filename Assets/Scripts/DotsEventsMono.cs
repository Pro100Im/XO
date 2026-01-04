using System;
using UnityEngine;

public class DotsEventsMono : MonoBehaviour
{
    public static DotsEventsMono Instance { get; private set; }

    public event EventHandler<OnClientConnectedEventArgs> OnClientConnectedEvent;
    public event EventHandler OnGameStartedEvent;
    public event EventHandler<OnWinnerEventArgs> OnGameWinEvent;
    public event EventHandler OnGameRematchEvent;
    public event EventHandler OnGameTieEvent;

    private void Awake()
    {
        //if (Instance != null && Instance != this)
        //{
        //    Destroy(gameObject);

        //    return;
        //}

        Instance = this;

        //DontDestroyOnLoad(gameObject);

        OnClientConnectedEvent += DotsEventsMono_OnClientConnectedEvent;
    }

    private void DotsEventsMono_OnClientConnectedEvent(object sender, OnClientConnectedEventArgs e)
    {
        Debug.Log($"Client connected with ConnectionId: {e.ConnectionId}");
    }

    public class OnClientConnectedEventArgs : EventArgs
    {
        public int ConnectionId;
    }

    public class OnWinnerEventArgs : EventArgs
    {
        public PlayerType Winner;
    }

    public void RaiseOnClientConnectedEvent(int connectionId)
    {
        OnClientConnectedEvent?.Invoke(this, new OnClientConnectedEventArgs { ConnectionId = connectionId });
    }

    public void RaiseOnGameWinEvent(PlayerType winner)
    {
        OnGameWinEvent?.Invoke(this, new OnWinnerEventArgs { Winner = winner });
    }

    public void RaiseOnGameStartedEvent()
    {
        OnGameStartedEvent?.Invoke(this, EventArgs.Empty);
    }

    public void RaiseOnGameRematchEvent()
    {
        OnGameRematchEvent?.Invoke(this, EventArgs.Empty);
    }

    public void RaiseOnGameTieEvent()
    {
        OnGameTieEvent?.Invoke(this, EventArgs.Empty);
    }
}
