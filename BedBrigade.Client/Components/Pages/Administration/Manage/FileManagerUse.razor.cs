using BedBrigade.Common.Enums;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;


namespace BedBrigade.Client.Components.Pages.Administration.Manage;

public partial class FileManagerUse : ComponentBase
{
    [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
    [Inject] private IAuthService? _svcAuth { get; set; }
    [Inject] private IWebHostEnvironment? _env { get; set; }
    [Inject] private ToastService _toastService { get; set; }
    private int _maxFileSize;
    
    private string _folderPath;
    private string _mediaFolder;
    private List<string> _allowedExtensions = [];
    private bool _enableFolderOperations;
    public string userRoute = String.Empty;
    private const string PathDivider = "/";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            Log.Information($"{_svcAuth.UserName} went to the Manage Media Page");
            _maxFileSize = await _svcConfiguration.GetConfigValueAsIntAsync(ConfigSection.Media, "MaxVideoSize");
            _mediaFolder = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "MediaFolder");

            string allowedFileExtensions = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "AllowedFileExtensions");
            string allowedVideoExtensions = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "AllowedVideoExtensions");
            _allowedExtensions.AddRange(allowedFileExtensions.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            _allowedExtensions.AddRange(allowedVideoExtensions.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            _enableFolderOperations = await _svcConfiguration.GetConfigValueAsBoolAsync(ConfigSection.Media, "EnableFolderOperations");
            userRoute = (_svcAuth.UserRoute ?? string.Empty).Replace(PathDivider, string.Empty);

            if (_svcAuth.IsNationalAdmin)
            {
                _folderPath = Path.Combine(_env.WebRootPath, _mediaFolder);
            }
            else
            {
                _folderPath = Path.Combine(_env.WebRootPath, _mediaFolder, userRoute);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing FileManagerUse component");
            _toastService.Error("Error", $"An error occurred while initializing the page: {ex.Message}");
        }
    }
}

