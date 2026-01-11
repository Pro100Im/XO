using System;
using UnityEngine;
using UnityEngine.UIElements;

public class GameSettings : INotifyBindablePropertyChanged
{
    public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

    public static GameSettings Instance { get; private set; } = null!;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RuntimeInitializeOnLoad() => Instance = new GameSettings();


    private GlobalGameState m_GameState;

    public GlobalGameState GameState
    {
        get => m_GameState;
        set
        {
            if (m_GameState == value)
                return;

            m_GameState = value;

            //Notify(MainMenuStylePropertyName);

            //Notify(LoadingScreenStylePropertyName);

            //Notify(InGameUIPropertyName);
            //Notify(RespawnScreenStylePropertyName);
        }
    }

    MainMenuState m_MainMenuState;
    public MainMenuState MainMenuState
    {
        get => m_MainMenuState;
        set
        {
            if (m_MainMenuState == value)
                return;

            m_MainMenuState = value;
            //Notify(MainMenuStylePropertyName);
            //Notify(SessionCodeStylePropertyName);
            //Notify(DirectConnectStylePropertyName);
        }
    }

    bool m_MainMenuSceneLoaded;
    public bool MainMenuSceneLoaded
    {
        get => m_MainMenuSceneLoaded;
        set
        {
            if (m_MainMenuSceneLoaded == value)
                return;

            m_MainMenuSceneLoaded = value;
            //Notify(MainMenuSceneLoadedPropertyName);
        }
    }
}

public enum GlobalGameState
{
    MainMenu,
    InGame,
    Loading
}

public enum MainMenuState
{
    MainMenuScreen,
    DirectConnectPopUp,
}


