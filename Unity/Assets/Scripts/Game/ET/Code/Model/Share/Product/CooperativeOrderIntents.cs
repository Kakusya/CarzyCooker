#if !DOTNET
using System.Collections.Generic;

namespace ET
{
    public enum OrderIntentAction { Set = 1, Clear = 2 }

    public readonly struct OrderIntentValue
    {
        public readonly int MemberId;
        public readonly long BindingGeneration, OrderId, CustomerId;
        public OrderIntentValue(int member, long binding, long order, long customer)
        {
            MemberId = member; BindingGeneration = binding; OrderId = order; CustomerId = customer;
        }
    }

    public readonly struct OrderIntentCommand : System.IEquatable<OrderIntentCommand>
    {
        public readonly OrderIntentAction Action;
        public readonly long OrderId, CustomerId;
        public OrderIntentCommand(OrderIntentAction action, long order, long customer)
        {
            Action = action; OrderId = order; CustomerId = customer;
        }
        public bool Equals(OrderIntentCommand other) => Action == other.Action && OrderId == other.OrderId && CustomerId == other.CustomerId;
    }

    [ComponentOf(typeof(Level))]
    public sealed class CooperativeOrderIntents : Entity, IAwake
    {
        public readonly Dictionary<int, OrderIntentValue> Members = new();
    }
}
#endif
