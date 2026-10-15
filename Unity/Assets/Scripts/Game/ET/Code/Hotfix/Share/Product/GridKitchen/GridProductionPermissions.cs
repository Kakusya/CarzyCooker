#if !DOTNET
namespace ET
{
    public static partial class GridKitchenSystem
    {
        private static string CheckNewRecipePermission(this GridKitchenComponent self, bool allowed, GridStation station = null)
        {
            ProductionPermit permit = self.ProductLevel()?.ProductionPermit;
            if (permit == null)
            {
                return null;
            }
            if (station != null && !permit.StationDefinitions.Contains(station.DefinitionId))
            {
                return ProductionPermissionRules.Rejection(ProductionPermissionDecision.EquipmentLocked);
            }
            return ProductionPermissionRules.Rejection(allowed ? ProductionPermissionDecision.Allowed : ProductionPermissionDecision.MenuRestricted);
        }

        private static string CheckProductionInput(this GridKitchenComponent self, int carrier, int input)
        {
            ProductionPermit permit = self.ProductLevel()?.ProductionPermit;
            return permit == null || ProductionPermissionRules.AllowsInput(permit, carrier, input) ? null :
                ProductionPermissionRules.Rejection(ProductionPermissionDecision.MenuRestricted);
        }
    }
}
#endif
