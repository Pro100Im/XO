using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Global
{
    public class GameManager : MonoBehaviour
    {
        public const string MainMenuSceneName = "MainMenu";
        public const string GameSceneName = "GameScene";
        public const string ResourcesSceneName = "GameResources";

        public ServicesSettings CurrentServicesSettings;

        private Task m_LoadingGame;
        private CancellationTokenSource m_LoadingGameCancel;
        private Task m_LoadingMainMenu;
        private CancellationTokenSource m_LoadingMainMenuCancel;

        GameConnection m_GameConnection;

        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);

                return;
            }

            Instance = this;
        }

        private async void Start()
        {
            GameSettings.Instance.MainMenuSceneLoaded = false;

            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                m_LoadingMainMenuCancel = new CancellationTokenSource();

                try
                {
                    m_LoadingMainMenu = StartMenuAsync(m_LoadingMainMenuCancel.Token);

                    await m_LoadingMainMenu;
                }
                catch (OperationCanceledException)
                {
                    // Nothing to do when the task is cancelled.
                }
                finally
                {
                    m_LoadingMainMenuCancel.Dispose();
                    m_LoadingMainMenuCancel = null;
                }
            }
        }

        private async Task StartMenuAsync(CancellationToken cancellationToken)
        {
            DestroyLocalSimulationWorld();

            var clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");

            await ScenesLoader.LoadGameplayAsync(null, clientWorld);

            GameSettings.Instance.MainMenuSceneLoaded = true;
            cancellationToken.ThrowIfCancellationRequested();
        }

        public static void DestroyLocalSimulationWorld()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    world.Dispose();
                    break;
                }
            }
        }

        public async void StartGameAsync()
        {
            if (GameSettings.Instance.GameState != GlobalGameState.MainMenu)
            {
                Debug.Log("[StartGameAsync] Called but in-game, cannot start while in-game!");
                return;
            }

            //GameSettings.Instance.CancellableUserInputPopUp = new AwaitableCompletionSource();
            GameSettings.Instance.MainMenuState = MainMenuState.DirectConnectPopUp;

            //try
            //{
            //    await GameSettings.Instance.CancellableUserInputPopUp.Awaitable;
            //}
            //catch (OperationCanceledException)
            //{
            //    return;
            //}
            //finally
            //{
            //    GameSettings.Instance.MainMenuState = MainMenuState.MainMenuScreen;
            //}

            BeginEnteringGame();

            m_LoadingGameCancel = new CancellationTokenSource();

            try
            {
                m_LoadingGame = StartGameAsync(m_LoadingGameCancel.Token);

                await m_LoadingGame;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[{nameof(StartGameAsync)}] Loading has been cancelled.");
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{nameof(StartGameAsync)}] Loading has failed, returning to main menu");
                Debug.LogException(e);
                // Disposing the token here because the error has been handled and ReturnToMainMenu should not check it.
                m_LoadingGameCancel.Dispose();
                m_LoadingGameCancel = null;

                ReturnToMainMenuAsync();

                return;
            }
            finally
            {
                m_LoadingGameCancel?.Dispose();
                m_LoadingGameCancel = null;
            }

            FinishLoadingGame();
        }

        private void BeginEnteringGame()
        {
            Debug.LogWarning($"[{nameof(BeginEnteringGame)}] Starting to enter game...");

            GameSettings.Instance.GameState = GlobalGameState.Loading;
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.StartLoading);
        }

        private async Task StartGameAsync(CancellationToken cancellationToken)
        {
            if (m_LoadingMainMenuCancel != null || GameSettings.Instance.MainMenuSceneLoaded)
            {
                if (m_LoadingMainMenuCancel != null)
                {
                    m_LoadingMainMenuCancel.Cancel();
                    try
                    {
                        await m_LoadingMainMenu;
                    }
                    catch (OperationCanceledException)
                    {
                        // We are ignoring the cancelled exception as it is expected.
                    }
                }

                if (GameSettings.Instance.MainMenuSceneLoaded)
                    await DisconnectAndUnloadWorlds();

                cancellationToken.ThrowIfCancellationRequested();
            }

            // Connecting to a Multiplayer Session.
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.InitializeConnection);

            m_GameConnection = await GameConnection.JoinOrCreateMatchmakerGameAsync(cancellationToken);
            m_GameConnection.Session.RemovedFromSession += OnSessionLeft;

            Debug.LogWarning($"[{nameof(StartGameAsync)}] Joined session {m_GameConnection.Session.Id}.");

            cancellationToken.ThrowIfCancellationRequested();

            ConnectionSettings.Instance.SessionCode = m_GameConnection.Session.Code;

            // Creating entity worlds.
            CreateEntityWorlds(m_GameConnection.Session, m_GameConnection.SessionConnectionType, out var server, out var client);

            // If we have a server, start listening.
            if (server != null)
            {
                using var drvQuery = server.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                var serverDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
                serverDriver.Listen(m_GameConnection.ListenEndpoint);

                Debug.LogWarning($"[{nameof(StartGameAsync)}] Server listening on {m_GameConnection.ListenEndpoint}.");
            }
            if (client != null)
            {
                ConnectionSettings.Instance.ConnectionEndpoint = m_GameConnection.ConnectEndpoint;
                await WaitForPlayerConnectionAsync(cancellationToken);

                Debug.LogWarning($"[{nameof(StartGameAsync)}] Client connected to {m_GameConnection.ConnectEndpoint}.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            Debug.LogWarning($"[{nameof(StartGameAsync)}] Connected to session {m_GameConnection.Session.Id}.");

            // Worlds are created and connected, game is ready to load and start.
            await ScenesLoader.LoadGameplayAsync(server, client);

            Debug.LogWarning($"[{nameof(StartGameAsync)}] Gameplay scenes loaded.");

            cancellationToken.ThrowIfCancellationRequested();

            if (client != null)
            {
                await WaitForGhostReplicationAsync(client, cancellationToken);
                await WaitForAttachedCameraAsync(client, cancellationToken);
            }
        }

        private async Task DisconnectAndUnloadWorlds()
        {
            ConnectionSettings.Instance.GameConnectionState = GameConnectionState.NotConnected;

            bool requestedDisconnect = false;

            foreach (var world in World.All)
            {
                if (world.IsClient())
                {
                    using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
                    if (query.TryGetSingletonEntity<NetworkId>(out var networkId))
                    {
                        requestedDisconnect = true;
                        world.EntityManager.AddComponentData(networkId, new NetworkStreamRequestDisconnect());
                    }
                }
            }

            if (requestedDisconnect)
                await Awaitable.NextFrameAsync();

            await LeaveSessionAsync();
            await DestroyGameSessionWorlds();
            await ScenesLoader.UnloadGameplayScenesAsync();
        }

        private async Task LeaveSessionAsync()
        {
            if (m_GameConnection != null)
            {
                m_GameConnection.Session.RemovedFromSession -= OnSessionLeft;

                if (m_GameConnection.Session.IsHost || m_GameConnection.Session.IsServer)
                    ConnectionSettings.Instance.SessionCode = null;

                if (m_GameConnection.Session.IsHost)
                    await m_GameConnection.Session.AsHost().DeleteAsync();
                else
                    await m_GameConnection.Session.LeaveAsync();

                m_GameConnection = null;
            }
        }

        public static async Task DestroyGameSessionWorlds()
        {
            // This prevents the "Cannot dispose world while updating it" error,
            // allowing us to call this from anywhere.
            await Awaitable.EndOfFrameAsync();

            // Destroy netcode worlds:
            for (var i = World.All.Count - 1; i >= 0; i--)
            {
                var world = World.All[i];

                if (world.IsServer() || world.IsClient())
                {
                    world.Dispose();
                }
            }
        }

        public static async Task WaitForPlayerConnectionAsync(CancellationToken cancellationToken = default)
        {
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WaitingConnection);

            // The GameManagerSystem is handling the connection/reconnection once the client world is created.
            ConnectionSettings.Instance.GameConnectionState = GameConnectionState.Connecting;

            while (ConnectionSettings.Instance.GameConnectionState == GameConnectionState.Connecting)
            {
                Debug.LogWarning($"[{nameof(WaitForPlayerConnectionAsync)}] Waiting for player connection...");

                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        private void OnSessionLeft()
        {
            m_GameConnection = null;
            ReturnToMainMenuAsync();
        }

        public async void ReturnToMainMenuAsync()
        {
            //Debug.Log($"[{nameof(ReturnToMainMenuAsync)}] Called.");

            //if (!CanUseMainMenu)
            //{
            //    QuitAsync();
            //    return;
            //}

            if (m_LoadingGameCancel != null)
            {
                Debug.Log($"[{nameof(ReturnToMainMenuAsync)}] Cancelling loading game.");
                m_LoadingGameCancel.Cancel();
                try
                {
                    await m_LoadingGame;
                }
                catch (OperationCanceledException)
                {
                    // Discarding this exception because we're the one asking for it.
                }
                Debug.Log($"[{nameof(ReturnToMainMenuAsync)}] Loading Cancelled, start returning to main menu.");
            }

            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.UnloadingGame);
            GameSettings.Instance.GameState = GlobalGameState.Loading;

            //GameSettings.Instance.IsPauseMenuOpen = false;

            await DisconnectAndUnloadWorlds();

            // Restart the main menu scene.
            Start();

            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.BackToMainMenu);
            GameSettings.Instance.GameState = GlobalGameState.MainMenu;
        }

        private static void CreateEntityWorlds(ISession session, NetworkType connectionType,
            out World serverWorld, out World clientWorld)
        {
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.CreateWorld);
            DestroyLocalSimulationWorld();

#if UNITY_EDITOR
            if (connectionType == NetworkType.Relay && MultiplayerPlayModePreferences.RequestedNumThinClients > 0)
            {
                Debug.Log($"[{nameof(CreateEntityWorlds)}] A number of Thin Clients was set while the connection mode is set to use Relay. Disabling Thin Clients.");
                MultiplayerPlayModePreferences.RequestedNumThinClients = 0;
            }
#endif

            serverWorld = null;
            clientWorld = null;

            if (session.IsHost)
            {
                serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            }
            if (!session.IsServer)
            {
                clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            }
        }

        private static async Task WaitForGhostReplicationAsync(World world, CancellationToken cancellationToken = default)
        {
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WorldReplication);

            using var ghostCountQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GhostCount>());
            var waitedForTicks = 0;

            while (true)
            {
                if (ghostCountQuery.TryGetSingleton<GhostCount>(out var ghostCount))
                {
                    var synchronizingPercentage = ghostCount.GhostCountOnServer == 0
                        ? math.saturate(ghostCount.GhostCountInstantiatedOnClient / (float)ghostCount.GhostCountOnServer)
                        : waitedForTicks > 60 ? 1f : 0f; // Apparently the server has no ghosts to send us, so ghost loading is complete.

                    //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WorldReplication, synchronizingPercentage);
                    if (synchronizingPercentage > 0.99f) // A bit of wiggle room, because in most games, ghosts are constantly created and destroyed.
                        return;
                }

                await Awaitable.NextFrameAsync(cancellationToken);

                waitedForTicks++;
            }
        }

        private static async Task WaitForAttachedCameraAsync(World world, CancellationToken cancellationToken = default)
        {
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.WaitingOnPlayer);

            //using var mainEntityCameraQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<MainCamera>());

            //while (!mainEntityCameraQuery.HasSingleton<MainCamera>())
            //{
            //    await Awaitable.NextFrameAsync(cancellationToken);
            //}

            //// Waiting an extra frame so that the player position is properly synced with the server.

            await Awaitable.NextFrameAsync(cancellationToken);
        }

        private void FinishLoadingGame()
        {
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.LoadingDone);
            GameSettings.Instance.GameState = GlobalGameState.InGame;
        }

        public async void StartFromBootstrapAsync(World server, World client)
        {
            if (GameSettings.Instance.GameState != GlobalGameState.MainMenu)
            {
                Debug.Log($"[{nameof(StartFromBootstrapAsync)}] Must not be in-game to join game!");

                return;
            }
            if (SceneManager.GetActiveScene().name == MainMenuSceneName)
            {
                Debug.Log($"Must not be in {MainMenuSceneName} to use [{nameof(StartFromBootstrapAsync)}]!");

                return;
            }

            //Debug.Log($"[{nameof(StartFromBootstrapAsync)}] Starting game");

            BeginEnteringGame();

            // The bootstrap is creating the worlds and start the connection for us,
            // let's make sure the client is connected before the next step.
            if (client != null)
            {
                await WaitForPlayerConnectionAsync();
            }

            // Load any additional scene that would be required by the Gameplay.
            await ScenesLoader.LoadGameplayAsync(server, client);

            if (client != null)
            {
                await WaitForGhostReplicationAsync(client);
                await WaitForAttachedCameraAsync(client);
            }

            FinishLoadingGame();
        }
    }
}