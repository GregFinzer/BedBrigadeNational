using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace BedBrigade.Client.Components
{
    public partial class MediaManager : ComponentBase
    {
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private ILocationDataService LocationDataService { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        private int _currentId = 1;
        
        private List<FolderItem> Folders = new List<FolderItem>();
        private List<FileItem> Files = new List<FileItem>();
        private string CurrentFolderPath;
        private string NewFolderName = string.Empty;
        private Modal MyModal;
        private List<FileItem> SelectedFiles = new();
        private string _searchQuery = string.Empty;
        private List<FileItem> AllFiles = new();
        private string _mediaFolderPath = string.Empty;
        private const string ErrorTitle = "Error";
        private List<FileItem> CutOrCopyFiles = new();
        private string CutSourceFolder = string.Empty;
        private string CopySourceFolder = string.Empty;
        private bool ShowCopyPasteButton => CutOrCopyFiles.Any() && CopySourceFolder != CurrentFolderPath && CutSourceFolder != CurrentFolderPath;
        private List<string> _protectedFolders = new List<string>();
        private bool ConvertImages { get; set; } = true;
        [Parameter]
        public string RootFolder { get; set; } = string.Empty;

        private static readonly HashSet<string> _imageConvertibleExts = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        //Default to 250MB
        [Parameter] public int MaxFileSize { get; set; } = 262144000;

        [Parameter]
        public List<string> AllowedExtensions { get; set; } = new List<string>()
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".ico", ".mp4", ".webm", ".avi", ".av1", ".mov", ".pdf"
        };

        [Parameter] public bool EnableFolderOperations { get; set; } = true;
        [Parameter] public string MediaFolderName { get; set; } = "Media";
        private bool IsUploading { get; set; }
        private string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                FilterFiles();
            }
        }

        private string DisplayPath
        {
            get { return _mediaFolderPath + @CurrentFolderPath.Replace(RootFolder, ""); }
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await CommonInit();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing MediaManager component");
                _toastService.Error("Error", "Error initializing MediaManager component");
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            try
            {
                await CommonInit();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error OnParametersSetAsync MediaManager component");
                _toastService.Error("Error", "Error OnParametersSetAsync MediaManager component");
            }
        }

        private async Task CommonInit()
        {
            if (!String.IsNullOrEmpty(RootFolder))
            {
                CurrentFolderPath = RootFolder;
                _mediaFolderPath = GetMediaFolderPath();
                RefreshFolders();
                RefreshFiles(RootFolder);
                var locationResult = await LocationDataService.GetActiveLocations();
                if (locationResult.Success)
                {
                    _protectedFolders = locationResult.Data.Select(l => l.Route.TrimStart('/')).ToList();
                    _protectedFolders.Add(MediaFolderName);
                    _protectedFolders.Add("pages");
                }
            }
        }

        private string GetMediaFolderPath()
        {
            int mediaFolderIndex = RootFolder.IndexOf(Path.DirectorySeparatorChar + MediaFolderName, StringComparison.OrdinalIgnoreCase);

            if (mediaFolderIndex == -1)
            {
                throw new IndexOutOfRangeException($"The MediaFolderName {MediaFolderName} could not be found in the RootFolder.");
            }

            return RootFolder.Substring(mediaFolderIndex);
        }

        private void RefreshFolders()
        {
            Folders.Clear();
            GetFolders(RootFolder, Folders);
            Folders = Folders.OrderBy(f => f.Name).ToList();
        }

        private void RefreshFiles(string folderPath)
        {
            SelectedFiles.Clear();
            Files = GetFiles(folderPath);
            AllFiles = new List<FileItem>(Files);
        }

        private void GetFolders(string path, List<FolderItem> folders)
        {
            string directoryName = Path.GetFileName(path);
            FolderItem folder = new FolderItem
            {
                Id = _currentId++,
                Name = directoryName,
                Path = path
            };
            folders.Add(folder);

            var subDirectories = Directory.GetDirectories(path)
                .OrderBy(directory => Path.GetFileName(directory)) // Sort alphabetically by folder name
                .ToList();

            foreach (string subFolder in subDirectories)
            {
                GetFolders(subFolder, folder.SubFolders);
            }
        }

        private List<FileItem> GetFiles(string folderPath)
        {
            var files = new List<FileItem>();
            foreach (var filePath in Directory.GetFiles(folderPath))
            {
                string extension = Path.GetExtension(filePath).ToLower();
                string fileType = extension switch
                {
                    ".jpg" or ".png" or ".jpeg" or ".gif" or ".webp" or ".svg" or ".ico" => "image",
                    ".mp4" or ".webm" or ".avi" or ".av1" or ".mov" => "video",
                    ".pdf" => "pdf",
                    _ => "other"
                };

                string path = filePath.Replace(RootFolder, _mediaFolderPath).Replace("\\", "/");
                files.Add(new FileItem
                {
                    Name = Path.GetFileName(filePath),
                    Path = path,
                    Type = fileType,
                    Extension = extension,
                    Size = new FileInfo(filePath).Length,
                    LastModified = File.GetLastWriteTime(filePath)
                });
            }

            return files.OrderBy(o => o.Name).ToList();
        }

        private RenderFragment RenderFolder(FolderItem folder) => builder =>
        {
            const string classAttribute = "class";

            int seq = 0;
            builder.OpenElement(seq++, "div");

            // Folder Button
            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, classAttribute, "btn btn-link p-0");
            builder.AddAttribute(seq++, "data-bs-toggle", "collapse");
            builder.AddAttribute(seq++, "data-bs-target", $"#mm-folder-{folder.Id}");
            builder.AddAttribute(seq++, "aria-expanded", "false");
            builder.AddAttribute(seq++, "aria-controls", $"mm-folder-{folder.Id}");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => OnFolderClick(folder)));
            builder.OpenElement(seq++, "i");
            builder.AddAttribute(seq++, classAttribute, "fas fa-folder");
            builder.CloseElement(); //i
            builder.AddContent(seq++, $" {folder.Name}");
            builder.CloseElement(); //button

            // Subfolders
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, classAttribute, "collapse ms-3");
            builder.AddAttribute(seq++, "id", $"mm-folder-{folder.Id}");
            builder.OpenElement(seq++, "ul");
            builder.AddAttribute(seq++, classAttribute, "list-group");
            foreach (var subFolder in folder.SubFolders)
            {
                builder.OpenElement(seq++, "li");
                builder.AddAttribute(seq++, classAttribute, "list-group-item");
                builder.AddContent(seq++, RenderFolder(subFolder));
                builder.CloseElement();
            }

            builder.CloseElement(); // ul
            builder.CloseElement(); // div

            builder.CloseElement(); // div
        };

        private async Task OnFolderClick(FolderItem folder)
        {
            CurrentFolderPath = folder.Path;
            RefreshFiles(CurrentFolderPath);
            StateHasChanged();
        }

        private async Task ShowNewFolderModal()
        {
            NewFolderName = string.Empty;
            NewFolderName = await MyModal.Show(Modal.ModalType.Prompt, Modal.ModalIcon.Question, "New Folder",
                "Please enter the folder name");
            await CreateNewFolder();
        }

        private async Task CreateNewFolder()
        {
            if (string.IsNullOrWhiteSpace(NewFolderName))
            {
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                    "Folder name cannot be empty.");
                return;
            }

            if (ContainsInvalidCharacters(NewFolderName))
            {
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                    "Invalid characters for a folder name.");
                return;
            }

            var newFolderPath = Path.Combine(CurrentFolderPath, NewFolderName);
            try
            {
                Directory.CreateDirectory(newFolderPath);

                // Refresh folders and navigate to the new folder
                RefreshFolders();
                await OnFolderClick(new FolderItem { Path = newFolderPath });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create new folder: {FolderName}", NewFolderName);
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Error, ErrorTitle,
                    $"Failed to create folder: {ex.Message}");
            }
        }

        private bool ContainsInvalidCharacters(string folderName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return folderName.Any(c => invalidChars.Contains(c));
        }

        private async Task ConfirmDeleteFolder()
        {
            var confirmed = await MyModal.Show(Modal.ModalType.Confirm, Modal.ModalIcon.Warning, "Delete Folder",
                $"Are you sure you want to delete the folder '{Path.GetFileName(DisplayPath)}'?");

            if (confirmed)
            {
                await DeleteFolder();
            }
        }

        private async Task DeleteFolder()
        {
            try
            {
                if (_protectedFolders.Any(o => CurrentFolderPath.EndsWith("\\" + o)))
                {
                    await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                        "This is a protected folder and cannot be deleted.");
                    return;
                }

                Directory.Delete(CurrentFolderPath, true); // Recursively deletes the folder
                CurrentFolderPath = Path.GetDirectoryName(CurrentFolderPath) ?? RootFolder;

                // Refresh folder tree and files
                RefreshFolders();
                RefreshFiles(CurrentFolderPath);
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Success, "Folder Deleted",
                    "The folder has been successfully deleted.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete folder: {FolderPath}", CurrentFolderPath);
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Error, ErrorTitle,
                    $"Failed to delete folder: {ex.Message}");
            }

            StateHasChanged();
        }

        private async Task ShowRenameFolderModal()
        {
            var currentFolderName = Path.GetFileName(CurrentFolderPath);
            var newFolderName = await MyModal.Show(Modal.ModalType.Prompt, Modal.ModalIcon.Question, "Rename Folder",
                $"Enter a new name for the folder '{currentFolderName}'.", currentFolderName);

            if (!string.IsNullOrWhiteSpace(newFolderName))
            {
                await RenameFolder(newFolderName);
            }
        }

        private async Task RenameFolder(string newFolderName)
        {
            if (string.IsNullOrWhiteSpace(newFolderName))
            {
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                    "Folder name cannot be empty.");
                return;
            }

            if (ContainsInvalidCharacters(newFolderName))
            {
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                    "Invalid characters for a folder name.");
                return;
            }

            try
            {
                var parentPath = Path.GetDirectoryName(CurrentFolderPath);
                if (parentPath == null)
                {
                    await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Error, ErrorTitle,
                        "Cannot rename this folder.");
                    return;
                }

                var newFolderPath = Path.Combine(parentPath, newFolderName);
                Directory.Move(CurrentFolderPath, newFolderPath);

                // Refresh the folder tree and update the current folder path
                CurrentFolderPath = newFolderPath;
                RefreshFolders();

                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Success, "Folder Renamed",
                    $"The folder has been renamed to '{newFolderName}'.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to rename folder: {CurrentFolderPath} to {NewFolderName}", CurrentFolderPath, newFolderName);
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Error, ErrorTitle,
                    $"Failed to rename folder: {ex.Message}");
            }

            StateHasChanged();
        }

        private void ToggleFileSelection(FileItem file)
        {
            file.IsSelected = !file.IsSelected;

            if (file.IsSelected)
            {
                if (!SelectedFiles.Contains(file))
                {
                    SelectedFiles.Add(file);
                }
            }
            else
            {
                SelectedFiles.Remove(file);
            }
        }

        // UPDATED: HandleFileUpload — replace the inner foreach body where files are written
        private async Task HandleFileUpload(InputFileChangeEventArgs e)
        {
            try
            {
                if (e == null) return;

                IsUploading = true;
                IReadOnlyList<IBrowserFile> inputFiles = e.GetMultipleFiles();

                foreach (var file in inputFiles)
                {
                    var targetPath = Path.Combine(CurrentFolderPath, file.Name);

                    if (!AllowedExtensions.Contains(Path.GetExtension(file.Name)))
                    {
                        await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                            $"File type not allowed for {file.Name}. Valid File Types are:<br />" +
                            String.Join("<br />", AllowedExtensions));
                        return;
                    }

                    if (file.Size > MaxFileSize)
                    {
                        await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                            $"File size exceeds the maximum limit of {MaxFileSize / 1024.0 / 1024.0:F2} MB for for {file.Name}.");
                        return;
                    }

                    using (var targetStream = new FileStream(targetPath, FileMode.Create))
                    {
                        using (var fileStream = file.OpenReadStream(MaxFileSize))
                        {
                            await fileStream.CopyToAsync(targetStream);
                        }
                    }

                    var ext = Path.GetExtension(file.Name) ?? string.Empty;

                    // Is this an image we should process?
                    if (ConvertImages && _imageConvertibleExts.Contains(ext))
                    {
                        // Process -> strip EXIF, resize, save as .webp (if not already).
                        await ProcessAndSaveImageAsync(targetPath);
                        File.Delete(targetPath);
                    }
                }

                // Refresh files after upload
                RefreshFiles(CurrentFolderPath);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to upload files to {CurrentFolderPath}", CurrentFolderPath);
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Error, ErrorTitle,
                    $"Failed to upload: {ex.Message}");
            }
            finally
            {
                IsUploading = false;
                StateHasChanged();
            }
        }


        private async Task<string> ProcessAndSaveImageAsync(string targetPath)
        {
            // Determine source extension and target path
            var srcExt = Path.GetExtension(targetPath) ?? string.Empty;
            var baseName = Path.GetFileNameWithoutExtension(targetPath);
            var folderPath = Path.GetDirectoryName(targetPath) ?? string.Empty;

            // We will save as .webp
            var finalFileName = $"{baseName}.webp";
            var finalPath = Path.Combine(folderPath, finalFileName);

            using (var image = await Image.LoadAsync(targetPath))
            {
                // Strip all EXIF (removes geolocation)
                image.Metadata.ExifProfile = null;

                // Resize to max 1000 on the longer edge (maintain aspect)
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(1000, 1000)
                }));

                // Save to WebP (quality can be adjusted)
                var encoder = new WebpEncoder
                {
                    Quality = 80 // tweak if desired
                };

                using (var outStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await image.SaveAsync(outStream, encoder);
                }
            }

            return finalPath;
        }


        private async Task ShowRenameFileModal()
        {
            var fileToRename = SelectedFiles.First();

            var newFileName = await MyModal.Show(Modal.ModalType.Prompt, Modal.ModalIcon.Question, "Rename File",
                $"Enter a new name for the file '{fileToRename.Name}'.", fileToRename.Name);

            if (!string.IsNullOrWhiteSpace(newFileName))
            {
                await RenameFile(fileToRename, newFileName);
            }
        }

        private async Task RenameFile(FileItem file, string newFileName)
        {
            if (string.IsNullOrWhiteSpace(newFileName))
            {
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                    "File name cannot be empty.");
                return;
            }

            if (ContainsInvalidCharacters(newFileName))
            {
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                    "Invalid characters for a file name.");
                return;
            }

            try
            {
                var newFilePath = Path.Combine(CurrentFolderPath, newFileName);
                if (File.Exists(newFilePath))
                {
                    await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                        "A file with the same name already exists.");
                    return;
                }

                var oldFilePath = Path.Combine(CurrentFolderPath, file.Name);
                File.Move(oldFilePath, newFilePath);

                // Refresh files and update UI
                RefreshFiles(CurrentFolderPath);
                StateHasChanged();

                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Success, "File Renamed",
                    $"The file has been renamed to '{newFileName}'.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to rename file: {OldFilePath} to {NewFileName}", file.Name, newFileName);
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Error, ErrorTitle,
                    $"Failed to rename file: {ex.Message}");
            }
        }

        private async Task ConfirmDeleteFiles()
        {
            var fileList = string.Join("<br/>", SelectedFiles.Select(f => f.Name));
            var confirmed = await MyModal.Show(Modal.ModalType.Confirm, Modal.ModalIcon.Warning, "Delete Files",
                $"Are you sure you want to delete the following files?<br/>{fileList}");

            if (confirmed)
            {
                await DeleteSelectedFiles();
            }
        }

        private async Task DeleteSelectedFiles()
        {
            try
            {
                foreach (var file in SelectedFiles)
                {
                    var filePath = Path.Combine(CurrentFolderPath, file.Name);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }

                RefreshFiles(CurrentFolderPath);
                StateHasChanged();

                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Success, "Files Deleted",
                    "Selected files have been deleted.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete files in {CurrentFolderPath}", CurrentFolderPath);
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Error, ErrorTitle,
                    $"Failed to delete files: {ex.Message}");
            }
        }

        private void RefreshContent()
        {
            try
            {
                RefreshFolders();
                RefreshFiles(CurrentFolderPath);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to refresh content in MediaManager component");
                MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Error, ErrorTitle,
                    $"Failed to refresh content: {ex.Message}");
            }
        }

        private async Task ShowFileInfo()
        {
            if (SelectedFiles.Count != 1)
            {
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                    "Please select exactly one file to view its information.");
                return;
            }

            var fileInfo = SelectedFiles.First();

            var fileDetails = $@"
            <strong>Name:</strong> {fileInfo.Name}<br/>
            <strong>Path:</strong> {fileInfo.Path}<br/>
            <strong>Size:</strong> {fileInfo.Size / 1024.0:F2} KB<br/>
            <strong>Last Modified:</strong> {fileInfo.LastModified}
        ";

            await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Info, "File Information", fileDetails);

        }

        private async Task OpenFileInModal(FileItem file)
        {
            string html = string.Empty;
            switch (file.Type)
            {
                case "image":
                    html = $"<img src=\"{file.Path}\" alt=\"{file.Name}\" class=\"img-fluid\" />";
                    break;
                case "video":
                    html = $"<video src=\"{file.Path}\" controls autoplay></video>";
                    break;
                case "pdf":
                    html = $"<embed src=\"{file.Path}\" type=\"application/pdf\" width=\"100%\" height=\"600px\" />";
                    break;
                default:
                    html = $"<p>File type not supported for preview.</p>";
                    break;
            }

            await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.None, file.Name, html);
        }

        private void FilterFiles()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                Files = new List<FileItem>(AllFiles); // Reset to original files if query is empty
            }
            else
            {
                Files = AllFiles
                    .Where(file => file.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            StateHasChanged();
        }
        private void CutSelectedFiles()
        {
            CutOrCopyFiles = new List<FileItem>(SelectedFiles);
            CutSourceFolder = CurrentFolderPath;
            SelectedFiles.Clear(); 
            StateHasChanged();
        }

        private void CopySelectedFiles()
        {
            CutOrCopyFiles = new List<FileItem>(SelectedFiles);
            CopySourceFolder = CurrentFolderPath;
            SelectedFiles.Clear(); 
            StateHasChanged();
        }

        private async Task PasteFiles()
        {
            var sourceFiles = CutOrCopyFiles;
            var sourceFolder = String.IsNullOrEmpty(CopySourceFolder) ? CutSourceFolder : CopySourceFolder;

            if (sourceFiles.Any() && !string.IsNullOrEmpty(sourceFolder))
            {
                foreach (var file in sourceFiles)
                {
                    var sourcePath = Path.Combine(sourceFolder, file.Name);
                    var targetPath = Path.Combine(CurrentFolderPath, file.Name);

                    if (!File.Exists(sourcePath))
                    {
                        await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                            $"Source file '{file.Name}' does not exist.");
                        continue;
                    }

                    if (File.Exists(targetPath))
                    {
                        await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                            $"File '{file.Name}' already exists in the target folder.");
                        continue;
                    }

                    try
                    {
                        if (String.IsNullOrEmpty(CopySourceFolder))
                        {
                            File.Move(sourcePath, targetPath);
                        }
                        else 
                        {
                            File.Copy(sourcePath, targetPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        string operation = String.IsNullOrEmpty(CopySourceFolder) ? "move" : "copy";
                        Log.Error(ex, "Failed to {Operation} file '{FileName}' from '{SourcePath}' to '{TargetPath}'",
                            operation, file.Name, sourcePath, targetPath);
                        await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Error, ErrorTitle,
                            $"Failed to {operation} file '{file.Name}': {ex.Message}");
                    }
                }

                CutOrCopyFiles.Clear();
                CutSourceFolder = string.Empty;
                CopySourceFolder = string.Empty;
                RefreshFiles(CurrentFolderPath);

                StateHasChanged();
            }
        }
    }
}
