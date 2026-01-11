using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Global
{
    public class ScenesLoader : MonoBehaviour
    {
        public static async Task LoadGameplayAsync(World server, World client)
        {
            await LoadGameplayScenesAsync();

            if (server != null)
                await WaitForAllSubScenesToLoadAsync(server);
            if (client != null)
                await WaitForAllSubScenesToLoadAsync(client);
        }

        public static async Task WaitForAllSubScenesToLoadAsync(World world)
        {
            if (world == null)
                return;

            using var scenesQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<SceneReference>());
            using var scenesLeftToLoad = scenesQuery.ToEntityListAsync(Allocator.Persistent, out var handle);

            handle.Complete();

            var count = scenesLeftToLoad.Length;

            while (scenesLeftToLoad.Length > 0)
            {
                for (var i = 0; i < scenesLeftToLoad.Length; i++)
                {
                    var sceneEntity = scenesLeftToLoad[i];

                    if (SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
                    {
                        scenesLeftToLoad.RemoveAt(i);

                        var numLoaded = count - scenesLeftToLoad.Length;
                        var loadingProgress = numLoaded / count;

                        i--;
                    }
                }

                await Awaitable.NextFrameAsync();
            }
        }

        public static async Task LoadGameplayScenesAsync()
        {
            await LoadSceneAsync(GameManager.GameSceneName);
            await LoadSceneAsync(GameManager.ResourcesSceneName);
        }

        public static async Task LoadSceneAsync(string sceneName)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded)
                return;

            var sceneLoading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            UpdateLoadingStateAsync(sceneLoading);

            await sceneLoading;
        }

        public static async void UpdateLoadingStateAsync(AsyncOperation loadingTask)
        {
            while (loadingTask != null && !loadingTask.isDone)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        public static async Task UnloadGameplayScenesAsync()
        {
            //LoadingData.Instance.UpdateLoading(LoadingData.LoadingSteps.UnloadingWorld);

            var gameplay = SceneManager.GetSceneByName(GameManager.GameSceneName);

            if (gameplay.IsValid() && gameplay != SceneManager.GetActiveScene())
            {
                var unloadScene = SceneManager.UnloadSceneAsync(gameplay);

                UpdateLoadingStateAsync(unloadScene);

                await unloadScene;
            }

            var resource = SceneManager.GetSceneByName(GameManager.ResourcesSceneName);

            if (resource.IsValid())
            {
                var unloadScene = SceneManager.UnloadSceneAsync(resource);

                UpdateLoadingStateAsync(unloadScene);

                await unloadScene;
            }
        }
    }
}