using Content.Client.Changelog;
using Content.Client.UserInterface.Systems.EscapeMenu;
using Content.Shared.Links;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Info;

public sealed class LinkBanner : BoxContainer
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

    public LinkBanner()
    {
        IoCManager.InjectDependencies(this);

        var buttons = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal
        };
        AddChild(buttons);

        var uriOpener = IoCManager.Resolve<IUriOpener>();

        var rulesButton = new Button { Text = Loc.GetString("server-info-rules-button") };
        rulesButton.OnPressed += _ => uriOpener.OpenUri(Links.Rules);

        var discordButton = new Button { Text = Loc.GetString("server-info-discord-button") };
        discordButton.OnPressed += _ => uriOpener.OpenUri(Links.Discord);

        var websiteButton = new Button { Text = Loc.GetString("server-info-website-button") };
        websiteButton.OnPressed += _ => uriOpener.OpenUri(Links.Website);

        var wikiButton = new Button { Text = Loc.GetString("server-info-wiki-button") };
        wikiButton.OnPressed += _ => uriOpener.OpenUri(Links.Wiki);

        var donateButton = new Button { Text = Loc.GetString("server-info-donate-button") };
        donateButton.OnPressed += _ => uriOpener.OpenUri(Links.Donate);

        var changelogButton = new ChangelogButton();
        changelogButton.OnPressed += _ => _userInterfaceManager.GetUIController<ChangelogUIController>().ToggleWindow();

        buttons.AddChild(changelogButton);
        buttons.AddChild(rulesButton);
        buttons.AddChild(discordButton);
        buttons.AddChild(websiteButton);
        buttons.AddChild(wikiButton);
        buttons.AddChild(donateButton);
    }
}
