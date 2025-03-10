using System.Linq;
using Content.Client.Humanoid;
using Content.Client.Lobby.UI;
using Content.Client.Message;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Client.AutoGenerated;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Client.Utility;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Preferences.UI;

public sealed class HighlightedContainer : PanelContainer
{
    public HighlightedContainer()
    {
        PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = new Color(47, 47, 53),
            ContentMarginTopOverride = 10,
            ContentMarginBottomOverride = 10,
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 10
        };
    }
}

[GenerateTypedNameReferences]
public sealed partial class HumanoidProfileEditor : Control
{
    private readonly List<AntagPreferenceSelector> _antagPreferences;
    private readonly IEntityManager _entMan;
    private readonly LineEdit? _flavorTextEdit;
    private readonly List<JobPrioritySelector> _jobPriorities;

    private readonly IClientPreferencesManager _preferencesManager;

    private readonly EntityUid? _previewDummy;

    private readonly ColorSelectorSliders _rgbSkinColorSelector;

    // Mildly hacky, as I don't trust prototype order to stay consistent and don't want the UI to break should a new one get added mid-edit. --moony
    private readonly List<SpeciesPrototype> _speciesList;
    private readonly List<TraitPreferenceSelector> _traitPreferences;
    private List<BodyTypePrototype> _bodyTypesList = new();

    private bool _isDirty;
    private bool _needsDummyRebuild;
    private bool _needUpdatePreview;
    public int CharacterSlot;
    public HumanoidCharacterProfile? Profile;

    public HumanoidProfileEditor(IClientPreferencesManager preferencesManager, IPrototypeManager prototypeManager,
        IEntityManager entityManager, IConfigurationManager configurationManager)
    {
        RobustXamlLoader.Load(this);
        _random = IoCManager.Resolve<IRobustRandom>();
        _prototypeManager = prototypeManager;
        _entMan = entityManager;
        _preferencesManager = preferencesManager;
        var markingManager = IoCManager.Resolve<MarkingManager>();

        #region Left

        #region Randomize

        #endregion Randomize

        #region Name

        NameEdit.OnTextChanged += args => { SetName(args.Text); };
        NameRandomButton.OnPressed += _ => RandomizeName();
        RandomizeEverythingButton.OnPressed += _ => { RandomizeEverything(); };

        #endregion Name

        #region Appearance

        TabContainer.SetTabTitle(0, Loc.GetString("humanoid-profile-editor-appearance-tab"));

        #region Sex

        var sexButtonGroup = new ButtonGroup();

        SexMaleButton.Group = sexButtonGroup;
        SexMaleButton.OnPressed += _ =>
        {
            SetSex(Sex.Male);
        };

        SexFemaleButton.Group = sexButtonGroup;
        SexFemaleButton.OnPressed += _ =>
        {
            SetSex(Sex.Female);
        };

        #endregion Sex

        #region Body Type

        CBodyTypesButton.OnItemSelected += OnBodyTypeSelected;

        UpdateBodyTypes();

        #endregion Body Type

        #region Age

        AgeEdit.OnTextChanged += args =>
        {
            if (!int.TryParse(args.Text, out var newAge))
                return;
            SetAge(newAge);
        };

        #endregion Age

        #region Species

        _speciesList = prototypeManager.EnumeratePrototypes<SpeciesPrototype>().Where(o => o.RoundStart).ToList();
        for (var i = 0; i < _speciesList.Count; i++)
        {
            var name = Loc.GetString(_speciesList[i].Name);
            CSpeciesButton.AddItem(name, i);
        }

        CSpeciesButton.OnItemSelected += args =>
        {
            CSpeciesButton.SelectId(args.Id);
            SetSpecies(_speciesList[args.Id].ID);
            UpdateHairPickers();
            OnSkinColorOnValueChanged();
        };

        #endregion Species

        #region Skin

        SkinColor.OnValueChanged += _ =>
        {
            OnSkinColorOnValueChanged();
        };

        RgbSkinColorContainer.AddChild(_rgbSkinColorSelector = new ColorSelectorSliders());
        _rgbSkinColorSelector.OnColorChanged += _ =>
        {
            OnSkinColorOnValueChanged();
        };

        #endregion

        #region Hair

        HairPicker.OnMarkingSelect += newStyle =>
        {
            if (Profile is null)
                return;
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithHairStyleName(newStyle.id));
            IsDirty = true;
        };

        HairPicker.OnColorChanged += newColor =>
        {
            if (Profile is null)
                return;
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithHairColor(newColor.marking.MarkingColors[0]));
            IsDirty = true;
        };

        FacialHairPicker.OnMarkingSelect += newStyle =>
        {
            if (Profile is null)
                return;
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithFacialHairStyleName(newStyle.id));
            IsDirty = true;
        };

        FacialHairPicker.OnColorChanged += newColor =>
        {
            if (Profile is null)
                return;
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithFacialHairColor(newColor.marking.MarkingColors[0]));
            IsDirty = true;
        };

        HairPicker.OnSlotRemove += _ =>
        {
            if (Profile is null)
                return;
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithHairStyleName(HairStyles.DefaultHairStyle)
            );
            UpdateHairPickers();
            IsDirty = true;
        };

        FacialHairPicker.OnSlotRemove += _ =>
        {
            if (Profile is null)
                return;
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithFacialHairStyleName(HairStyles.DefaultFacialHairStyle)
            );
            UpdateHairPickers();
            IsDirty = true;
        };

        HairPicker.OnSlotAdd += delegate
        {
            if (Profile is null)
                return;

            var hair = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, Profile.Species).Keys
                .FirstOrDefault();

            if (string.IsNullOrEmpty(hair))
                return;

            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithHairStyleName(hair)
            );

            UpdateHairPickers();

            IsDirty = true;
        };

        FacialHairPicker.OnSlotAdd += delegate
        {
            if (Profile is null)
                return;

            var hair = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.FacialHair, Profile.Species).Keys
                .FirstOrDefault();

            if (string.IsNullOrEmpty(hair))
                return;

            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithFacialHairStyleName(hair)
            );

            UpdateHairPickers();

            IsDirty = true;
        };

        #endregion Hair

        #region Clothing

        ClothingButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-jumpsuit"),
            (int) ClothingPreference.Jumpsuit);
        ClothingButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-jumpskirt"),
            (int) ClothingPreference.Jumpskirt);

        ClothingButton.OnItemSelected += args =>
        {
            ClothingButton.SelectId(args.Id);
            SetClothing((ClothingPreference) args.Id);
        };

        #endregion Clothing

        #region Backpack

        BackpackButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-backpack"),
            (int) BackpackPreference.Backpack);
        BackpackButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-satchel"),
            (int) BackpackPreference.Satchel);
        BackpackButton.AddItem(Loc.GetString("humanoid-profile-editor-preference-duffelbag"),
            (int) BackpackPreference.Duffelbag);

        BackpackButton.OnItemSelected += args =>
        {
            BackpackButton.SelectId(args.Id);
            SetBackpack((BackpackPreference) args.Id);
        };

        #endregion Backpack

        #region Eyes

        EyesPicker.OnEyeColorPicked += newColor =>
        {
            if (Profile is null)
                return;
            Profile = Profile.WithCharacterAppearance(
                Profile.Appearance.WithEyeColor(newColor));
            IsDirty = true;
        };

        #endregion Eyes

        #endregion Appearance

        #region Jobs

        TabContainer.SetTabTitle(1, Loc.GetString("humanoid-profile-editor-jobs-tab"));

        PreferenceUnavailableButton.AddItem(
            Loc.GetString("humanoid-profile-editor-preference-unavailable-stay-in-lobby-button"),
            (int) PreferenceUnavailableMode.StayInLobby);
        PreferenceUnavailableButton.AddItem(
            Loc.GetString("humanoid-profile-editor-preference-unavailable-spawn-as-overflow-button",
                ("overflowJob", Loc.GetString(SharedGameTicker.FallbackOverflowJobName))),
            (int) PreferenceUnavailableMode.SpawnAsOverflow);

        PreferenceUnavailableButton.OnItemSelected += args =>
        {
            PreferenceUnavailableButton.SelectId(args.Id);

            Profile = Profile?.WithPreferenceUnavailable((PreferenceUnavailableMode) args.Id);
            IsDirty = true;
        };

        _jobPriorities = new List<JobPrioritySelector>();
        var jobCategories = new Dictionary<string, BoxContainer>();

        var firstCategory = true;
        var playTime = IoCManager.Resolve<PlayTimeTrackingManager>();

        foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
        {
            var departmentName = Loc.GetString($"department-{department.ID}");

            if (!jobCategories.TryGetValue(department.ID, out var category))
            {
                category = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Name = department.ID,
                    ToolTip = Loc.GetString("humanoid-profile-editor-jobs-amount-in-department-tooltip",
                        ("departmentName", departmentName))
                };

                if (firstCategory)
                    firstCategory = false;
                else
                {
                    category.AddChild(new Control
                    {
                        MinSize = new Vector2(0, 23)
                    });
                }

                category.AddChild(new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#464966") },
                    Children =
                    {
                        new Label
                        {
                            Text = Loc.GetString("humanoid-profile-editor-department-jobs-label",
                                ("departmentName", departmentName))
                        }
                    }
                });

                jobCategories[department.ID] = category;
                JobList.AddChild(category);
            }

            var jobs = department.Roles.Select(o => _prototypeManager.Index<JobPrototype>(o))
                .Where(o => o.SetPreference).ToList();
            jobs.Sort((x, y) =>
                -string.Compare(x.LocalizedName, y.LocalizedName, StringComparison.CurrentCultureIgnoreCase));

            foreach (var job in jobs)
            {
                var selector = new JobPrioritySelector(job);

                if (!playTime.IsAllowed(job, out var reason))
                    selector.LockRequirements(reason);

                category.AddChild(selector);
                _jobPriorities.Add(selector);

                selector.PriorityChanged += priority =>
                {
                    Profile = Profile?.WithJobPriority(job.ID, priority);
                    IsDirty = true;

                    foreach (var jobSelector in _jobPriorities)
                    {
                        // Sync other selectors with the same job in case of multiple department jobs
                        if (jobSelector.Job == selector.Job)
                            jobSelector.Priority = priority;

                        // Lower any other high priorities to medium.
                        if (priority == JobPriority.High)
                        {
                            if (jobSelector.Job != selector.Job && jobSelector.Priority == JobPriority.High)
                            {
                                jobSelector.Priority = JobPriority.Medium;
                                Profile = Profile?.WithJobPriority(jobSelector.Job.ID, JobPriority.Medium);
                            }
                        }
                    }
                };
            }
        }

        #endregion Jobs

        #region Antags

        TabContainer.SetTabTitle(2, Loc.GetString("humanoid-profile-editor-antags-tab"));

        _antagPreferences = new List<AntagPreferenceSelector>();

        foreach (var antag in prototypeManager.EnumeratePrototypes<AntagPrototype>().OrderBy(a => a.Name))
        {
            if (!antag.SetPreference)
                continue;

            var selector = new AntagPreferenceSelector(antag);
            AntagList.AddChild(selector);
            _antagPreferences.Add(selector);

            selector.PreferenceChanged += preference =>
            {
                Profile = Profile?.WithAntagPreference(antag.ID, preference);
                IsDirty = true;
            };
        }

        #endregion Antags

        #region Traits

        var traits = prototypeManager.EnumeratePrototypes<TraitPrototype>().OrderBy(t => t.Name).ToList();
        _traitPreferences = new List<TraitPreferenceSelector>();
        TabContainer.SetTabTitle(3, Loc.GetString("humanoid-profile-editor-traits-tab"));

        if (traits.Count > 0)
        {
            foreach (var trait in traits)
            {
                var selector = new TraitPreferenceSelector(trait);
                TraitsList.AddChild(selector);
                _traitPreferences.Add(selector);

                selector.PreferenceChanged += preference =>
                {
                    Profile = Profile?.WithTraitPreference(trait.ID, preference);
                    IsDirty = true;
                };
            }
        }
        else
        {
            TraitsList.AddChild(new Label
            {
                Text = "No traits available :(",
                FontColorOverride = Color.Gray
            });
        }

        #endregion

        #region Save

        SaveButton.OnPressed += _ => { Save(); };

        #endregion Save

        #region Markings

        TabContainer.SetTabTitle(4, Loc.GetString("humanoid-profile-editor-markings-tab"));

        CMarkings.OnMarkingAdded += OnMarkingChange;
        CMarkings.OnMarkingRemoved += OnMarkingChange;
        CMarkings.OnMarkingColorChange += OnMarkingChange;
        CMarkings.OnMarkingRankChange += OnMarkingChange;

        #endregion Markings

        #region FlavorText

        if (configurationManager.GetCVar(CCVars.FlavorText))
        {
            var flavorText = new FlavorText.FlavorText();
            TabContainer.AddChild(flavorText);
            TabContainer.SetTabTitle(TabContainer.ChildCount - 1,
                Loc.GetString("humanoid-profile-editor-flavortext-tab"));
            _flavorTextEdit = flavorText.CFlavorTextInput;

            flavorText.OnFlavorTextChanged += OnFlavorTextChange;
        }

        #endregion FlavorText

        #region Dummy

        var species = Profile?.Species ?? SharedHumanoidSystem.DefaultSpecies;
        var dollProto = _prototypeManager.Index<SpeciesPrototype>(species).DollPrototype;

        if (_previewDummy != null)
            _entMan.DeleteEntity(_previewDummy!.Value);

        _previewDummy = _entMan.SpawnEntity(dollProto, MapCoordinates.Nullspace);
        var sprite = _entMan.GetComponent<SpriteComponent>(_previewDummy!.Value);

        var previewSprite = new SpriteView
        {
            Sprite = sprite,
            Scale = (6, 6),
            OverrideDirection = Direction.South,
            VerticalAlignment = VAlignment.Center,
            SizeFlagsStretchRatio = 1
        };
        PreviewSpriteControl.AddChild(previewSprite);

        var previewSpriteSide = new SpriteView
        {
            Sprite = sprite,
            Scale = (6, 6),
            OverrideDirection = Direction.East,
            VerticalAlignment = VAlignment.Center,
            SizeFlagsStretchRatio = 1
        };
        PreviewSpriteSideControl.AddChild(previewSpriteSide);

        #endregion Dummy

        #endregion Left

        if (preferencesManager.ServerDataLoaded)
            LoadServerData();

        preferencesManager.OnServerDataLoaded += LoadServerData;


        IsDirty = false;
    }

    private LineEdit AgeEdit => CAgeEdit;
    private LineEdit NameEdit => CNameEdit;
    private Button NameRandomButton => CNameRandomize;
    private Button RandomizeEverythingButton => CRandomizeEverything;
    private Button SaveButton => CSaveButton;
    private Button SexFemaleButton => CSexFemale;
    private Button SexMaleButton => CSexMale;
    private Slider SkinColor => CSkin;
    private OptionButton ClothingButton => CClothingButton;
    private OptionButton BackpackButton => CBackpackButton;
    private SingleMarkingPicker HairPicker => CHairStylePicker;
    private SingleMarkingPicker FacialHairPicker => CFacialHairPicker;
    private EyeColorPicker EyesPicker => CEyeColorPicker;

    private TabContainer TabContainer => CTabContainer;
    private BoxContainer JobList => CJobList;
    private BoxContainer AntagList => CAntagList;
    private BoxContainer TraitsList => CTraitsList;
    private OptionButton PreferenceUnavailableButton => CPreferenceUnavailableButton;

    private Control PreviewSpriteControl => CSpriteViewFront;
    private Control PreviewSpriteSideControl => CSpriteViewSide;

    private BoxContainer RgbSkinColorContainer => CRgbSkinColorContainer;

    private bool IsDirty
    {
        get => _isDirty;
        set
        {
            _isDirty = value;
            _needUpdatePreview = true;
            UpdateSaveButton();
        }
    }

    private bool NeedsDummyRebuild
    {
        get => _needsDummyRebuild;
        set
        {
            _needsDummyRebuild = value;
            _needUpdatePreview = true;
        }
    }

    public event Action<HumanoidCharacterProfile, int>? OnProfileChanged;

    private void OnFlavorTextChange(string content)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithFlavorText(content);
        IsDirty = true;
    }

    private void OnMarkingChange(MarkingSet markings)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithCharacterAppearance(
            Profile.Appearance.WithMarkings(markings.GetForwardEnumerator().ToList()));
        NeedsDummyRebuild = true;
        IsDirty = true;
    }

    private void OnSkinColorOnValueChanged()
    {
        if (Profile is null)
            return;

        var skin = _prototypeManager.Index<SpeciesPrototype>(Profile.Species).SkinColoration;

        switch (skin)
        {
            case HumanoidSkinColor.HumanToned:
            {
                if (!SkinColor.Visible)
                {
                    SkinColor.Visible = true;
                    RgbSkinColorContainer.Visible = false;
                }

                var color = Shared.Humanoid.SkinColor.HumanSkinTone((int) SkinColor.Value);

                CMarkings.CurrentSkinColor = color;
                Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));
                break;
            }
            case HumanoidSkinColor.Hues:
            {
                if (!RgbSkinColorContainer.Visible)
                {
                    SkinColor.Visible = false;
                    RgbSkinColorContainer.Visible = true;
                }

                CMarkings.CurrentSkinColor = _rgbSkinColorSelector.Color;
                Profile = Profile.WithCharacterAppearance(
                    Profile.Appearance.WithSkinColor(_rgbSkinColorSelector.Color));
                break;
            }
            case HumanoidSkinColor.TintedHues:
            {
                if (!RgbSkinColorContainer.Visible)
                {
                    SkinColor.Visible = false;
                    RgbSkinColorContainer.Visible = true;
                }

                var color = Shared.Humanoid.SkinColor.TintedHues(_rgbSkinColorSelector.Color);

                CMarkings.CurrentSkinColor = color;
                Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithSkinColor(color));
                break;
            }
        }

        IsDirty = true;
        NeedsDummyRebuild = true; // TODO: ugh - fix this asap
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_previewDummy != null)
            _entMan.DeleteEntity(_previewDummy.Value);

        _preferencesManager.OnServerDataLoaded -= LoadServerData;
    }

    private void LoadServerData()
    {
        Profile = (HumanoidCharacterProfile) _preferencesManager.Preferences!.SelectedCharacter;
        CharacterSlot = _preferencesManager.Preferences.SelectedCharacterIndex;

        NeedsDummyRebuild = true;
        UpdateControls();
    }

    private void SetAge(int newAge)
    {
        Profile = Profile?.WithAge(newAge);
        IsDirty = true;
    }

    private void SetSex(Sex newSex)
    {
        Profile = Profile?.WithSex(newSex);
        // Body type can have sex restriction.
        UpdateBodyTypes();
        IsDirty = true;
    }

    private void SetBodyType(string newBodyType)
    {
        Profile = Profile?.WithBodyType(newBodyType);
        IsDirty = true;
        NeedsDummyRebuild = true;
    }

    private void SetSpecies(string newSpecies)
    {
        Profile = Profile?.WithSpecies(newSpecies);
        // Species may have special color prefs, make sure to update it.
        OnSkinColorOnValueChanged();
        // Repopulate the markings tab as well.
        CMarkings.SetSpecies(newSpecies);
        UpdateBodyTypes();
        NeedsDummyRebuild = true;
        IsDirty = true;
    }

    private void SetName(string newName)
    {
        Profile = Profile?.WithName(newName);
        IsDirty = true;
    }

    private void SetClothing(ClothingPreference newClothing)
    {
        Profile = Profile?.WithClothingPreference(newClothing);
        IsDirty = true;
    }

    private void SetBackpack(BackpackPreference newBackpack)
    {
        Profile = Profile?.WithBackpackPreference(newBackpack);
        IsDirty = true;
    }

    public void Save()
    {
        IsDirty = false;

        if (Profile == null)
            return;

        _preferencesManager.UpdateCharacter(Profile, CharacterSlot);
        NeedsDummyRebuild = true;
        OnProfileChanged?.Invoke(Profile, CharacterSlot);
    }

    private void OnBodyTypeSelected(OptionButton.ItemSelectedEventArgs args)
    {
        args.Button.SelectId(args.Id);
        SetBodyType(_bodyTypesList[args.Id].ID);
    }

    private void UpdateBodyTypes()
    {
        if (Profile is null)
            return;

        CBodyTypesButton.Clear();
        var species = _prototypeManager.Index<SpeciesPrototype>(Profile.Species);
        var sex = Profile.Sex;
        _bodyTypesList = EntitySystem.Get<HumanoidSystem>().GetValidBodyTypes(species, sex);

        for (var i = 0; i < _bodyTypesList.Count; i++)
        {
            CBodyTypesButton.AddItem(Loc.GetString(_bodyTypesList[i].Name), i);
        }

        // If current body type is not valid.
        if (!_bodyTypesList.Select(proto => proto.ID).Contains(Profile.BodyType))
        {
            // Then replace it with a first valid body type.
            SetBodyType(_bodyTypesList.First().ID);
        }

        IsDirty = true;
    }

    private void UpdateNameEdit()
    {
        NameEdit.Text = Profile?.Name ?? "";
    }

    private void UpdateFlavorTextEdit()
    {
        if (_flavorTextEdit is not null)
            _flavorTextEdit.Text = Profile?.FlavorText ?? "";
    }

    private void UpdateAgeEdit()
    {
        AgeEdit.Text = Profile?.Age.ToString() ?? "";
    }

    private void UpdateSexControls()
    {
        if (Profile?.Sex == Sex.Male)
            SexMaleButton.Pressed = true;
        else
            SexFemaleButton.Pressed = true;
    }

    private void UpdateSkinColor()
    {
        if (Profile == null)
            return;

        var skin = _prototypeManager.Index<SpeciesPrototype>(Profile.Species).SkinColoration;

        switch (skin)
        {
            case HumanoidSkinColor.HumanToned:
            {
                if (!SkinColor.Visible)
                {
                    SkinColor.Visible = true;
                    RgbSkinColorContainer.Visible = false;
                }

                SkinColor.Value = Shared.Humanoid.SkinColor.HumanSkinToneFromColor(Profile.Appearance.SkinColor);

                break;
            }
            case HumanoidSkinColor.Hues:
            {
                if (!RgbSkinColorContainer.Visible)
                {
                    SkinColor.Visible = false;
                    RgbSkinColorContainer.Visible = true;
                }

                // set the RGB values to the direct values otherwise
                _rgbSkinColorSelector.Color = Profile.Appearance.SkinColor;
                break;
            }
            case HumanoidSkinColor.TintedHues:
            {
                if (!RgbSkinColorContainer.Visible)
                {
                    SkinColor.Visible = false;
                    RgbSkinColorContainer.Visible = true;
                }

                // set the RGB values to the direct values otherwise
                _rgbSkinColorSelector.Color = Profile.Appearance.SkinColor;
                break;
            }
        }
    }

    private void UpdateMarkings()
    {
        if (Profile == null)
            return;

        CMarkings.SetData(Profile.Appearance.Markings, Profile.Species, Profile.Appearance.SkinColor);
    }

    private void UpdateSpecies()
    {
        if (Profile == null)
            return;

        CSpeciesButton.Select(_speciesList.FindIndex(x => x.ID == Profile.Species));
    }

    private void UpdateClothingControls()
    {
        if (Profile == null)
            return;

        ClothingButton.SelectId((int) Profile.Clothing);
    }

    private void UpdateBackpackControls()
    {
        if (Profile == null)
            return;

        BackpackButton.SelectId((int) Profile.Backpack);
    }

    private void UpdateHairPickers()
    {
        if (Profile == null)
            return;

        var hairMarking = Profile.Appearance.HairStyleId switch
        {
            HairStyles.DefaultHairStyle => new List<Marking>(),
            _ => new List<Marking>
                { new(Profile.Appearance.HairStyleId, new List<Color> { Profile.Appearance.HairColor }) }
        };

        var facialHairMarking = Profile.Appearance.FacialHairStyleId switch
        {
            HairStyles.DefaultFacialHairStyle => new List<Marking>(),
            _ => new List<Marking>
            {
                new(Profile.Appearance.FacialHairStyleId,
                    new List<Color> { Profile.Appearance.FacialHairColor })
            }
        };

        HairPicker.UpdateData(
            hairMarking,
            Profile.Species,
            1);
        FacialHairPicker.UpdateData(
            facialHairMarking,
            Profile.Species,
            1);
    }

    private void UpdateEyePickers()
    {
        if (Profile == null)
            return;

        EyesPicker.SetData(Profile.Appearance.EyeColor);
    }

    private void UpdateSaveButton()
    {
        SaveButton.Disabled = Profile is null || !IsDirty;
    }

    private void UpdatePreview()
    {
        if (Profile is null)
            return;

        /* dear fuck this needs to not happen ever again
        if (_needsDummyRebuild)
        {
            RebuildSpriteView(); // Species change also requires sprite rebuild, so we'll do that now.
            _needsDummyRebuild = false;
        }
        */

        EntitySystem.Get<HumanoidSystem>().LoadProfile(_previewDummy!.Value, Profile);
        LobbyCharacterPreviewPanel.GiveDummyJobClothes(_previewDummy!.Value, Profile);
    }

    public void UpdateControls()
    {
        if (Profile is null)
            return;
        UpdateNameEdit();
        UpdateFlavorTextEdit();
        UpdateSexControls();
        UpdateSkinColor();
        UpdateSpecies();
        UpdateClothingControls();
        UpdateBackpackControls();
        UpdateAgeEdit();
        UpdateHairPickers();
        UpdateEyePickers();
        UpdateSaveButton();
        UpdateJobPriorities();
        UpdateAntagPreferences();
        UpdateTraitPreferences();
        UpdateMarkings();
        UpdateBodyTypes();

        NeedsDummyRebuild = true;

        PreferenceUnavailableButton.SelectId((int) Profile.PreferenceUnavailable);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_needUpdatePreview)
        {
            UpdatePreview();

            _needUpdatePreview = false;
        }
    }

    private void UpdateJobPriorities()
    {
        foreach (var prioritySelector in _jobPriorities)
        {
            var jobId = prioritySelector.Job.ID;

            var priority = Profile?.JobPriorities.GetValueOrDefault(jobId, JobPriority.Never) ?? JobPriority.Never;

            prioritySelector.Priority = priority;
        }
    }

    private void UpdateAntagPreferences()
    {
        foreach (var preferenceSelector in _antagPreferences)
        {
            var antagId = preferenceSelector.Antag.ID;
            var preference = Profile?.AntagPreferences.Contains(antagId) ?? false;

            preferenceSelector.Preference = preference;
        }
    }

    private void UpdateTraitPreferences()
    {
        foreach (var preferenceSelector in _traitPreferences)
        {
            var traitId = preferenceSelector.Trait.ID;
            var preference = Profile?.TraitPreferences.Contains(traitId) ?? false;

            preferenceSelector.Preference = preference;
        }
    }

    private sealed class JobPrioritySelector : Control
    {
        private readonly StripeBack _lockStripe;
        private readonly RadioOptions<int> _optionButton;
        private readonly Label _requirementsLabel;
        private Label _jobTitle;

        public JobPrioritySelector(JobPrototype job)
        {
            Job = job;

            _optionButton = new RadioOptions<int>(RadioOptionsLayout.Horizontal)
            {
                FirstButtonStyle = StyleBase.ButtonOpenRight,
                ButtonStyle = StyleBase.ButtonOpenBoth,
                LastButtonStyle = StyleBase.ButtonOpenLeft
            };

            // Text, Value
            _optionButton.AddItem(Loc.GetString("humanoid-profile-editor-job-priority-high-button"),
                (int) JobPriority.High);
            _optionButton.AddItem(Loc.GetString("humanoid-profile-editor-job-priority-medium-button"),
                (int) JobPriority.Medium);
            _optionButton.AddItem(Loc.GetString("humanoid-profile-editor-job-priority-low-button"),
                (int) JobPriority.Low);
            _optionButton.AddItem(Loc.GetString("humanoid-profile-editor-job-priority-never-button"),
                (int) JobPriority.Never);

            _optionButton.OnItemSelected += args =>
            {
                _optionButton.Select(args.Id);
                PriorityChanged?.Invoke(Priority);
            };

            var icon = new TextureRect
            {
                TextureScale = (2, 2),
                Stretch = TextureRect.StretchMode.KeepCentered
            };

            {
                var specifier = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/Interface/Misc/job_icons.rsi"),
                    job.Icon);
                icon.Texture = specifier.Frame0();
            }

            _requirementsLabel = new Label
            {
                Text = Loc.GetString("role-timer-locked"),
                Visible = true,
                HorizontalAlignment = HAlignment.Center,
                StyleClasses = { StyleBase.StyleClassLabelSubText }
            };

            _lockStripe = new StripeBack
            {
                Visible = false,
                HorizontalExpand = true,
                TooltipDelay = 0.2f,
                MouseFilter = MouseFilterMode.Stop,
                Children =
                {
                    _requirementsLabel
                }
            };

            _jobTitle = new Label()
            {
                Text = job.LocalizedName,
                MinSize = (175, 0),
                MouseFilter = MouseFilterMode.Stop
            };

            if (job.LocalizedDescription != null)
            {
                _jobTitle.ToolTip = job.LocalizedDescription;
                _jobTitle.TooltipDelay = 0.2f;
            }

            AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    icon,
                    _jobTitle,
                    _optionButton,
                    _lockStripe
                }
            });
        }

        public JobPrototype Job { get; }

        public JobPriority Priority
        {
            get => (JobPriority) _optionButton.SelectedValue;
            set => _optionButton.SelectByValue((int) value);
        }

        public event Action<JobPriority>? PriorityChanged;

        public void LockRequirements(string requirements)
        {
            _lockStripe.ToolTip = requirements;
            _lockStripe.Visible = true;
            _optionButton.Visible = false;
        }

        // TODO: Subscribe to roletimers event. I am too lazy to do this RN But I doubt most people will notice fn
        public void UnlockRequirements()
        {
            _requirementsLabel.Visible = false;
            _lockStripe.Visible = false;
            _optionButton.Visible = true;
        }
    }

    private sealed class AntagPreferenceSelector : Control
    {
        private readonly CheckBox _checkBox;

        public AntagPreferenceSelector(AntagPrototype antag)
        {
            Antag = antag;

            _checkBox = new CheckBox { Text = $"{antag.Name}" };
            _checkBox.OnToggled += OnCheckBoxToggled;

            if (antag.Description != null)
            {
                _checkBox.ToolTip = antag.Description;
                _checkBox.TooltipDelay = 0.2f;
            }

            AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children =
                {
                    _checkBox
                }
            });
        }

        public AntagPrototype Antag { get; }

        public bool Preference
        {
            get => _checkBox.Pressed;
            set => _checkBox.Pressed = value;
        }

        public event Action<bool>? PreferenceChanged;

        private void OnCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
        {
            PreferenceChanged?.Invoke(Preference);
        }
    }

    private sealed class TraitPreferenceSelector : Control
    {
        private readonly CheckBox _checkBox;

        public TraitPreferenceSelector(TraitPrototype trait)
        {
            Trait = trait;

            _checkBox = new CheckBox { Text = $"{trait.Name}" };
            _checkBox.OnToggled += OnCheckBoxToggled;

            if (trait.Description != null)
            {
                _checkBox.ToolTip = trait.Description;
                _checkBox.TooltipDelay = 0.2f;
            }

            AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                Children = { _checkBox }
            });
        }

        public TraitPrototype Trait { get; }

        public bool Preference
        {
            get => _checkBox.Pressed;
            set => _checkBox.Pressed = value;
        }

        public event Action<bool>? PreferenceChanged;

        private void OnCheckBoxToggled(BaseButton.ButtonToggledEventArgs args)
        {
            PreferenceChanged?.Invoke(Preference);
        }
    }
}
