using DevToys.Api;
using System.ComponentModel.Composition;

namespace Folderly;

[Export(typeof(IResourceAssemblyIdentifier))]
[Name(nameof(FolderlyResourceAssemblyIdentifier))]
internal sealed class FolderlyResourceAssemblyIdentifier : IResourceAssemblyIdentifier
{
    public ValueTask<FontDefinition[]> GetFontDefinitionsAsync()
    {
        throw new NotImplementedException();
    }
}