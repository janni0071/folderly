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
public class FolderlyGui
{
    public UIToolView View =>
        new(Label().Style(UILabelStyle.BodyStrong).Text("Hello World!"));

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        // throw new NotImplementedException();
    }
}