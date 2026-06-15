using DevToys.Api;
using static DevToys.Api.GUI;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MetadataExtractor;
using Directory = System.IO.Directory;

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
internal sealed class FolderlyGui : IGuiTool, IDisposable
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly IFileStorage _fileStorage;
    private bool _disposed;
    private bool _isCleaning; // Prevents concurrent cleanup

    // UI state properties
    private string _folderPath = string.Empty;
    private bool _deleteDuplicates;
    private bool _sortByFileType;
    private bool _renameImagesByDate;

    // UI element references
    private IUISingleLineTextInput _folderInput = null!;
    private IUIButton _cleanUpButton = null!;
    private IUILabel _statusLabel = null!;

    [ImportingConstructor]
    public FolderlyGui(ISettingsProvider settingsProvider, IFileStorage fileStorage)
    {
        _settingsProvider = settingsProvider;
        _fileStorage = fileStorage;
    }

    public UIToolView View =>
        new UIToolView(
            Stack()
                .Vertical()
                .WithChildren(
                    Label().Style(UILabelStyle.BodyStrong).Text("Your own smart file organizer!"),

                    // Folder picker: horizontal stack with text input and browse button
                    Stack()
                        .Horizontal()
                        .WithChildren(
                            (_folderInput = SingleLineTextInput()
                                .Title("Folder to clean up")
                                .Text(_folderPath)
                                .OnTextChanged(OnFolderPathChanged)),
                            Button()
                                .Text("Browse")
                                .Icon("FluentSystemIcons", '\uE708')
                                .OnClick(OnBrowseClicked)
                        ),

                    // Options group
                    SettingGroup()
                        .Title("Options")
                        .Icon("FluentSystemIcons", '\uE670')
                        .WithSettings(
                            Setting()
                                .Title("Delete duplicate files")
                                .Description("Removes duplicate files (by content hash), keeping only one copy")
                                .InteractiveElement(
                                    Switch()
                                        .OffText("Disabled")
                                        .OnText("Enabled")
                                        .OnToggle(OnDeleteDuplicatesToggled)),

                            Setting()
                                .Title("Sort files by type")
                                .Description("Moves files into subfolders like 'Images/', 'Documents/', etc.")
                                .InteractiveElement(
                                    Switch()
                                        .OffText("Disabled")
                                        .OnText("Enabled")
                                        .OnToggle(OnSortByFileTypeToggled)),

                            Setting()
                                .Title("Rename images with date taken")
                                .Description("Uses EXIF date to rename .jpg/.png files (e.g., 2024-03-15_143022.jpg)")
                                .InteractiveElement(
                                    Switch()
                                        .OffText("Disabled")
                                        .OnText("Enabled")
                                        .OnToggle(OnRenameImagesToggled))
                        ),

                    // Execute button - Using AccentAppearance for the primary action
                    (_cleanUpButton = Button()
                        .Text("Clean Up Folder")
                        .Icon("FluentSystemIcons", '\uE71A')
                        .AccentAppearance() // Correct primary button style
                        .OnClick(OnCleanUpClicked)),

                    // Status label
                    (_statusLabel = Label()
                        .Style(UILabelStyle.Caption)
                        .Text("Ready"))
                ));

    private void OnFolderPathChanged(string newPath) => _folderPath = newPath;

    private async void OnBrowseClicked()
    {
        // Correct method for picking a folder
        var folder = await _fileStorage.PickFolderAsync();
        if (!string.IsNullOrEmpty(folder))
        {
            _folderPath = folder;
            _folderInput.Text(folder);
        }
    }

    private void OnDeleteDuplicatesToggled(bool isOn) => _deleteDuplicates = isOn;
    private void OnSortByFileTypeToggled(bool isOn) => _sortByFileType = isOn;
    private void OnRenameImagesToggled(bool isOn) => _renameImagesByDate = isOn;

    private async void OnCleanUpClicked()
    {
        if (_isCleaning) return;

        if (string.IsNullOrWhiteSpace(_folderPath) || !Directory.Exists(_folderPath))
        {
            _statusLabel.Text("Please select a valid folder first.");
            return;
        }

        _isCleaning = true;
        _statusLabel.Text("Cleaning up...");

        try
        {
            await Task.Run(() =>
            {
                if (_deleteDuplicates)
                    DeleteDuplicateFiles(_folderPath);

                if (_sortByFileType)
                    SortFilesByType(_folderPath);

                if (_renameImagesByDate)
                    RenameImagesByDateTaken(_folderPath);
            });

            _statusLabel.Text("Cleanup completed successfully!");
        }
        catch (System.Exception ex)
        {
            _statusLabel.Text($"Error: {ex.Message}");
        }
        finally
        {
            _isCleaning = false;
        }
    }

    // --- Core cleanup logic (unchanged) ---

    private void DeleteDuplicateFiles(string folder)
    {
        var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
        var hashSet = new HashSet<string>();
        var duplicates = new List<string>();

        foreach (var file in files)
        {
            using var stream = File.OpenRead(file);
            var hash = System.Security.Cryptography.SHA256.HashData(stream);
            var hashString = Convert.ToHexString(hash);

            if (!hashSet.Add(hashString))
                duplicates.Add(file);
        }

        foreach (var dup in duplicates)
        {
            File.Delete(dup);
        }
    }

    private void SortFilesByType(string folder)
    {
        var files = Directory.GetFiles(folder);
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant().TrimStart('.');
            string targetFolder = ext switch
            {
                "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp" => "Images",
                "doc" or "docx" or "txt" or "pdf" or "xls" or "xlsx" => "Documents",
                "mp4" or "avi" or "mkv" or "mov" => "Videos",
                "mp3" or "wav" or "flac" => "Audio",
                "zip" or "rar" or "7z" => "Archives",
                "exe" or "msi" => "Executables",
                _ => "Other"
            };

            var destDir = Path.Combine(folder, targetFolder);
            Directory.CreateDirectory(destDir);
            var destPath = Path.Combine(destDir, Path.GetFileName(file));
            if (!File.Exists(destPath))
                File.Move(file, destPath);
        }
    }

    private void RenameImagesByDateTaken(string folder)
    {
        var imageFiles = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
            .Where(f => new[] { ".jpg", ".jpeg", ".png", ".tiff" }.Contains(Path.GetExtension(f).ToLowerInvariant()));

        foreach (var file in imageFiles)
        {
            try
            {
                System.DateTime? dateTaken = GetDateTakenFromImage(file);
                if (dateTaken.HasValue)
                {
                    var newName = dateTaken.Value.ToString("yyyy-MM-dd_HHmmss") + Path.GetExtension(file);
                    var newPath = Path.Combine(Path.GetDirectoryName(file)!, newName);
                    if (!File.Exists(newPath))
                        File.Move(file, newPath);
                }
            }
            catch { /* skip unreadable files */ }
        }
    }

    private System.DateTime? GetDateTakenFromImage(string path)
    {
        var directories = ImageMetadataReader.ReadMetadata(path);
        var subIfdDir = directories.OfType<MetadataExtractor.Formats.Exif.ExifSubIfdDirectory>().FirstOrDefault();
        var dateTime = subIfdDir?.GetDateTime(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTimeOriginal);
        return dateTime;
    }

    public void OnDataReceived(string dataTypeName, object? parsedData) { }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}