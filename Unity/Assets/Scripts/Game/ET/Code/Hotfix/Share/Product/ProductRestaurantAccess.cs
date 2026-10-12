#if !DOTNET
namespace ET
{
    [FriendOf(typeof(Match))]
    [FriendOf(typeof(RestaurantRuntime))]
    [FriendOf(typeof(GridKitchenComponent))]
    public static class ProductRestaurantAccess
    {
        public static GridKitchenComponent ProductKitchen(this Match match)
        {
            if (match == null) return null;
            RestaurantRuntime restaurant = match.Restaurant;
            return restaurant == null ? match.GetComponent<GridKitchenComponent>() : restaurant.Kitchen;
        }
        public static Level ProductLevel(this GridKitchenComponent kitchen)
        {
            RestaurantRuntime restaurant = kitchen?.Restaurant ?? default;
            return restaurant?.CurrentLevel ?? default;
        }
    }
}
#endif
