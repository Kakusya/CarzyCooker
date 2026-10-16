#if !DOTNET
namespace ET
{
    public static class ProductMenuCatalog
    {
        // 既有MenuVariant整数边界集中转换；共享值来自Luban源枚举，0仍是合法基础菜单。
        public static bool IsValid(int value) => IsValid((ProductMenuVariant)value);

        public static bool IsValid(ProductMenuVariant value) => value is ProductMenuVariant.Basic or ProductMenuVariant.Drink or ProductMenuVariant.Salad or ProductMenuVariant.Growth or ProductMenuVariant.Hot or ProductMenuVariant.Dessert or ProductMenuVariant.Cafe;

        public static bool Includes(ProductMenuVariant active, ProductMenuVariant resource) => IsValid(active) && IsValid(resource) &&
            (resource == ProductMenuVariant.Basic || active == resource || active == ProductMenuVariant.Cafe ||
                (ProductMenuVariant)active is ProductMenuVariant.Growth or ProductMenuVariant.Hot &&
                (ProductMenuVariant)resource is ProductMenuVariant.Drink or ProductMenuVariant.Salad);
    }
}
#endif
