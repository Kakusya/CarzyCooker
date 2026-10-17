#if !DOTNET
using System.Linq;

namespace ET
{
    public static class ProductMenuCatalog
    {
        // 既有MenuVariant整数边界集中转换；共享值来自Luban源枚举，0仍是合法基础菜单。
        public static bool IsValid(int value) => IsValid((ProductMenuVariant)value);

        public static bool IsValid(ProductMenuVariant value) => value is ProductMenuVariant.Basic or ProductMenuVariant.Drink or ProductMenuVariant.Salad or ProductMenuVariant.Growth or ProductMenuVariant.Hot or ProductMenuVariant.Dessert or ProductMenuVariant.Cafe or ProductMenuVariant.Steamed or ProductMenuVariant.Tea or ProductMenuVariant.SteamTea;

        public static bool SupportsDrinks(ProductMenuVariant menu) => Includes(menu, ProductMenuVariant.Drink) || Includes(menu, ProductMenuVariant.Tea);

        public static bool IncludesItem(ProductMenuVariant menu, DRGridItemConfig definition) => Includes(menu, definition.MenuVariant) ||
            Tables.Instance.DTInitialLayoutConfig.DataList.Any(row => Includes(menu, row.MenuVariant) && row.InitialItemDefinitionId == definition.Id) ||
            Tables.Instance.DTSupplyConfig.DataList.Any(row => Includes(menu, row.MenuVariant) &&
                (row.MaterialDefinitionId == definition.Id || row.PackageDefinitionId == definition.Id));

        public static bool Includes(ProductMenuVariant active, ProductMenuVariant resource) => IsValid(active) && IsValid(resource) &&
            (resource == ProductMenuVariant.Basic || active == resource || active == ProductMenuVariant.SteamTea &&
                resource is ProductMenuVariant.Steamed or ProductMenuVariant.Tea || active == ProductMenuVariant.Cafe &&
                resource is ProductMenuVariant.Drink or ProductMenuVariant.Salad or ProductMenuVariant.Growth or ProductMenuVariant.Hot or ProductMenuVariant.Dessert ||
                (ProductMenuVariant)active is ProductMenuVariant.Growth or ProductMenuVariant.Hot &&
                (ProductMenuVariant)resource is ProductMenuVariant.Drink or ProductMenuVariant.Salad);
    }
}
#endif
