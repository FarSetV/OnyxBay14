using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using NUnit.Framework;
using Robust.Shared.Enums;

namespace Content.Tests.Shared.Preferences.Humanoid;

[TestFixture]
public sealed class GenderTest : ContentUnitTest
{
    private static HumanoidCharacterProfile ProfileWithSex(Sex sex) => HumanoidCharacterProfile.Default().WithSex(sex);

    [Test]
    public void TestFemaleSexReturnsFemaleGender()
    {
        Assert.That(ProfileWithSex(Sex.Female).Gender == Gender.Female);
    }

    [Test]
    public void TestMaleSexReturnsMaleGender()
    {
        Assert.That(ProfileWithSex(Sex.Male).Gender == Gender.Male);
    }
}
