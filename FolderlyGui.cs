using DevToys.Api;
using System.ComponentModel.Composition;
using static DevToys.Api.GUI;

namespace Folderly;

[Export(typeof(IGuiTool))]
[Name("Folderly")]
[ToolDisplayInformation(
    IconFontName = "FluentSystemIcons",
    IconGlyph = '\uE670',
    GroupName = PredefinedCommonToolGroupNames.Converters,
    ResourceManagerAssemblyIdentifier = nameof(FolderlyResourceAssemblyIdentifier),
    ResourceManagerBaseName = "Folderly.Folderly",
    ShortDisplayTitleResourceName = nameof(Folderly.ShortDisplayTitle),
    LongDisplayTitleResourceName = nameof(Folderly.LongDisplayTitle),
    DescriptionResourceName = nameof(Folderly.Description),
    AccessibleNameResourceName = nameof(Folderly.AccessibleName))]
internal sealed class FolderlyGui : IGuiTool
{
    public UIToolView View =>
        new UIToolView(
            Stack()
                .Vertical()
                .WithChildren(
                    Label().Style(UILabelStyle.BodyStrong).Text("Your own smart file organizer!"),
                    SingleLineTextInput()
                        .Title("Folder to clean up"),
                    SettingGroup()
                        .Title("Options")
                        .Icon("FluentSystemIcons", '\uE670')
                        .WithSettings(
                            Setting()
                                .Title("Duplicate files")
                                .Description("This will delete all duplicates in your folder")
                                .InteractiveElement(Switch()))
                ));

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        // throw new NotImplementedException();
    }
}