using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;

namespace Content.Client.Stylesheets
{
    public sealed class StylesheetManager : IStylesheetManager
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public Stylesheet SheetNano { get; private set; } = default!;
        public Stylesheet SheetSpace { get; private set; } = default!;
        public Stylesheet SheetBase { get; private set; } = default!;

        public void Initialize()
        {
            SheetNano = new StyleNano(_resourceCache).Stylesheet;
            SheetSpace = new StyleSpace(_resourceCache).Stylesheet;
            SheetBase = new StyleBase(_resourceCache).Stylesheet;

            _userInterfaceManager.Stylesheet = SheetNano;
        }
    }
}
