using System.Text;
using BedBrigade.Common.Models;
using Bogus.DataSets;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace BedBrigade.Client.Components
{
    public partial class MediaManager : ComponentBase
    {
        [Inject] private IJSRuntime JS { get; set; }
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

        [Parameter]
        public string RootFolder { get; set; } = string.Empty;



        //Default to 250MB
        [Parameter] public int MaxFileSize { get; set; } = 262144000;

        [Parameter]
        public List<string> AllowedExtensions { get; set; } = new List<string>()
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp",
            ".svg",
            ".ico",
            ".mp4",
            ".webm",
            ".avi",
            ".av1",
            ".mov",
            ".pdf"
        };

        [Parameter] public bool EnableFolderOperations { get; set; } = true;
        [Parameter] public string MediaFolderName { get; set; } = "Media";

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

        protected override void OnInitialized()
        {
            CommonInit();
        }

        protected override void OnParametersSet()
        {
            CommonInit();
        }

        private void CommonInit()
        {
            if (!String.IsNullOrEmpty(RootFolder))
            {
                CurrentFolderPath = RootFolder;
                _mediaFolderPath = GetMediaFolderPath();
                RefreshFolders();
                RefreshFiles(RootFolder);
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
            builder.AddAttribute(seq++, "data-bs-target", $"#folder-{folder.Id}");
            builder.AddAttribute(seq++, "aria-expanded", "false");
            builder.AddAttribute(seq++, "aria-controls", $"folder-{folder.Id}");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => OnFolderClick(folder)));
            builder.OpenElement(seq++, "i");
            builder.AddAttribute(seq++, classAttribute, "fas fa-folder");
            builder.CloseElement(); //i
            builder.AddContent(seq++, $" {folder.Name}");
            builder.CloseElement(); //button

            // Subfolders
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, classAttribute, "collapse ms-3");
            builder.AddAttribute(seq++, "id", $"folder-{folder.Id}");
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
                if (CurrentFolderPath == RootFolder)
                {
                    await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                        "The root folder cannot be deleted.");
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

        private async Task HandleFileUpload(InputFileChangeEventArgs e)
        {
            try
            {
                if (e != null)
                {
                    var inputFiles = e.GetMultipleFiles();

                    foreach (var file in inputFiles)
                    {
                        var targetPath = Path.Combine(CurrentFolderPath, file.Name);

                        if (!AllowedExtensions.Contains(Path.GetExtension(file.Name)))
                        {
                            await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                                $"File type not allowed for {file.Name}. Valid File Types are:<br />" + String.Join("<br />",AllowedExtensions));
                            return;
                        }

                        if (file.Size > MaxFileSize)
                        {
                            await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Warning, ErrorTitle,
                                $"File size exceeds the maximum limit of {MaxFileSize / 1024.0 / 1024.0:F2} MB for for {file.Name}.");
                            return;
                        }

                        MemoryStream stream = new MemoryStream();
                        await file.OpenReadStream(file.Size).CopyToAsync(stream);
                        byte[] fileBytes = stream.ToArray();

                        await File.WriteAllBytesAsync(targetPath, fileBytes);
                    }

                    // Refresh files after upload
                    RefreshFiles(CurrentFolderPath);
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Error, ErrorTitle,
                    $"Failed to upload: {ex.Message}");
            }

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

                File.Move(file.Path.Replace("/" + _mediaFolderPath, RootFolder).Replace("/", "\\"),
                    newFilePath);

                // Refresh files and update UI
                RefreshFiles(CurrentFolderPath);
                StateHasChanged();

                await MyModal.Show(Modal.ModalType.Alert, Modal.ModalIcon.Success, "File Renamed",
                    $"The file has been renamed to '{newFileName}'.");
            }
            catch (Exception ex)
            {
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
                    var filePath = file.Path.Replace("/" + _mediaFolderPath, RootFolder).Replace("/", "\\");
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
    }
}
