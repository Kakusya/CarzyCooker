using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterUnitCreate_CreateUnitView: AEvent<Scene, AfterUnitCreate>
    {
        protected override async UniTask Run(Scene scene, AfterUnitCreate args)
        {
            Unit unit = args.Unit;
            var owner = unit.GetAsyncOwner();
            EntityRef<Unit> unitRef = unit;
            await owner.RunAsync(async context =>
            {
                GameObject prefab = await owner.GetOwnedResources().LoadAssetAsync<GameObject>(AssetUtility.GetPrefabAsset("Skeleton/Skeleton"));
                context.Apply(() =>
                {
                    Unit current = unitRef;
                    GameObject go = UnityEngine.Object.Instantiate(prefab);
                    go.transform.position = current.Position;
                    current.AddComponent<GameObjectComponent>().GameObject = go;
                    current.AddComponent<AnimatorComponent>();
                });
            });
        }
    }
}
