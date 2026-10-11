using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(LSUnitViewComponent))]
    public static partial class LSUnitViewComponentSystem
    {
        [EntitySystem]
        private static void Awake(this LSUnitViewComponent self)
        {

        }
        
        [EntitySystem]
        private static void Destroy(this LSUnitViewComponent self)
        {

        }

        public static async UniTask InitAsync(this LSUnitViewComponent self)
        {
            Room room = self.Room();
            LSUnitComponent lsUnitComponent = room.LSWorld.GetComponent<LSUnitComponent>();
            Scene root = self.Root();
            foreach (long playerId in room.PlayerIds)
            {
                LSUnit lsUnit = lsUnitComponent.GetChild<LSUnit>(playerId);
                var owner = self.GetAsyncOwner();
                EntityRef<LSUnitViewComponent> selfRef = self;
                var position = lsUnit.Position.ToVector();
                long id = lsUnit.Id;
                EntityRef<LSUnit> unitRef = lsUnit;
                await owner.RunAsync(async context =>
                {
                    GameObject prefab = await owner.GetOwnedResources().LoadAssetAsync<GameObject>(AssetUtility.GetPrefabAsset("Skeleton/Skeleton"));
                    context.Apply(() =>
                    {
                        if ((LSUnit)unitRef == null) return;
                        LSUnitViewComponent current = selfRef;
                        GameObject unitGo = UnityEngine.Object.Instantiate(prefab);
                        unitGo.transform.position = position;
                        LSUnitView view = current.AddChildWithId<LSUnitView, GameObject>(id, unitGo);
                        view.AddComponent<LSAnimatorComponent>();
                    });
                });
            }
        }
    }
}
