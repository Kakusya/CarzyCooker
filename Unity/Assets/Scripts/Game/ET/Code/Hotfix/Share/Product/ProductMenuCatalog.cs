#if !DOTNET
namespace ET
{
    public static class ProductMenuCatalog
    {
        // 既有MenuVariant整数边界集中转换；共享值来自Luban源枚举，0仍是合法基础菜单。
        public static bool IsValid(int value) => (ProductMenuVariant)value is ProductMenuVariant.Basic or ProductMenuVariant.Drink or ProductMenuVariant.Salad or ProductMenuVariant.Growth or ProductMenuVariant.Hot or ProductMenuVariant.Dessert or ProductMenuVariant.Cafe;

        public static bool Includes(int active, int resource) => IsValid(active) && IsValid(resource) &&
            (resource == (int)ProductMenuVariant.Basic || active == resource || active == (int)ProductMenuVariant.Cafe ||
                (ProductMenuVariant)active is ProductMenuVariant.Growth or ProductMenuVariant.Hot &&
                (ProductMenuVariant)resource is ProductMenuVariant.Drink or ProductMenuVariant.Salad);
    }
}
#endif
