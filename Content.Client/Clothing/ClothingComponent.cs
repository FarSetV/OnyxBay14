using Content.Shared.Clothing.Components;

namespace Content.Client.Clothing
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedClothingComponent))]
    public sealed class ClothingComponent : SharedClothingComponent
    {
        public string? InSlot;
    }
}
