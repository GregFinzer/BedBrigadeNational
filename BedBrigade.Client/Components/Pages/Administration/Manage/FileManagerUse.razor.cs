using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using BedBrigade.Common.Logic;
using Serilog;

namespace BedBrigade.Client.Components.Pages.Administration.Manage;

public partial class FileManagerUse : ComponentBase
{
    [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
    [Inject] private ILocationDataService? _svcLocation { get; set; }
    [Inject] private IAuthService? _svcAuth { get; set; }
    [Inject] private IWebHostEnvironment? _env { get; set; }

    [Inject] private NavigationManager _navigation { get; set; }

    private int _maxFileSize;
    
    private string _folderPath;
    private string _mediaFolder;
    private List<string> _allowedExtensions = new List<string>();
    private bool _enableFolderOperations = false;
    private ClaimsPrincipal? Identity { get; set; }
    private string userName = String.Empty;
    private int userLocationId = 0;
    public string userRoute = String.Empty;
    private string userRole = String.Empty;
    private const string PathDivider = "/";

    protected override async Task OnInitializedAsync()
    {
        _maxFileSize = await _svcConfiguration.GetConfigValueAsIntAsync(ConfigSection.Media, "MaxVideoSize");
        _mediaFolder = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "MediaFolder");

        string allowedFileExtensions = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "AllowedFileExtensions");
        string allowedVideoExtensions = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "AllowedVideoExtensions");
        _allowedExtensions.AddRange(allowedFileExtensions.Split(',',StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        _allowedExtensions.AddRange(allowedVideoExtensions.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        _enableFolderOperations = await _svcConfiguration.GetConfigValueAsBoolAsync(ConfigSection.Media, "EnableFolderOperations");

        Identity = _svcAuth.CurrentUser;
        userName = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Defaults.DefaultUserNameAndEmail;
        Log.Information($"{userName} went to the Manage Media Page");

        if (!Identity.Claims.Any())
        {
            string returnUrl = _navigation.ToBaseRelativePath(_navigation.Uri);
            _navigation.NavigateTo($"/login?returnUrl={returnUrl}", true);
            return;
        }

        userLocationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value);
        userRoute = Identity.Claims.FirstOrDefault(c => c.Type == "UserRoute").Value;
        userRoute = userRoute.Replace(PathDivider, "");


        if (userLocationId == (int)LocationNumber.National)
        {
            _folderPath = Path.Combine(_env.WebRootPath, _mediaFolder);
        }
        else
        {
            _folderPath = Path.Combine(_env.WebRootPath, _mediaFolder, userRoute);
        }
    }
}

