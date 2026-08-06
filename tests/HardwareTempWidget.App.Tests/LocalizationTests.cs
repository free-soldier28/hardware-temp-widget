using HardwareTempWidget.Core;

namespace HardwareTempWidget.App.Tests;

[Collection("Localization")]
public class LocalizationTests
{
    [Fact]
    public void T_UnknownKey_ReturnsKeyItself()
    {
        Assert.Equal("Does.Not.Exist", Localization.T("Does.Not.Exist"));
    }

    [Fact]
    public void T_English_ReturnsEnglishString()
    {
        Localization.Initialize(AppLanguage.English);

        Assert.Equal("Language", Localization.T("Settings.Language"));
        Assert.Equal("Exit", Localization.T("Menu.Exit"));
    }

    [Fact]
    public void T_Russian_ReturnsRussianString()
    {
        Localization.Initialize(AppLanguage.Russian);

        Assert.Equal("Язык", Localization.T("Settings.Language"));
        Assert.Equal("Выход", Localization.T("Menu.Exit"));
    }

    [Fact]
    public void SetLanguage_ToSame_DoesNotRaiseChanged()
    {
        Localization.Initialize(AppLanguage.English);
        var raised = false;
        Localization.LanguageChanged += Handler;
        try
        {
            Localization.SetLanguage(AppLanguage.English);
            Assert.False(raised);
        }
        finally
        {
            Localization.LanguageChanged -= Handler;
        }

        void Handler() => raised = true;
    }

    [Fact]
    public void SetLanguage_ToDifferent_RaisesChangedAndTraduces()
    {
        Localization.Initialize(AppLanguage.English);
        var raised = false;
        Localization.LanguageChanged += Handler;
        try
        {
            Localization.SetLanguage(AppLanguage.Russian);

            Assert.True(raised);
            Assert.Equal(AppLanguage.Russian, Localization.Current);
            Assert.Equal("Прозрачность", Localization.T("Settings.Opacity"));
        }
        finally
        {
            Localization.LanguageChanged -= Handler;
        }

        void Handler() => raised = true;
    }

    [Fact]
    public void Initialize_SetsCurrentLanguage()
    {
        Localization.Initialize(AppLanguage.Russian);

        Assert.Equal(AppLanguage.Russian, Localization.Current);
    }
}