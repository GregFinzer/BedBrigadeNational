using BedBrigade.Client.Services;
using BedBrigade.Data.Services;
using BedBrigade.Common.Logic;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Enums;
using BedBrigade.Common.EnumModels;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KellermanSoftware.NetEmailValidation;
using System.Data;
using BedBrigade.Data;
using System.Data.Entity.Infrastructure;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.IO.Compression;
using System.Data.SqlClient;
using BedBrigade.Common.Models;
using BedBrigade.Client.Components.Pages.Administration.Manage;
using System.Collections.Generic;
using Microsoft.JSInterop.Infrastructure;
using BedBrigade.Common.Constants;
using Syncfusion.Blazor.Inputs;
using System.Linq;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using static BedBrigade.Common.Logic.BlogHelper;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Routing;
using Syncfusion.Blazor.Grids;
using System.Text.RegularExpressions;
using System.ComponentModel;
using Syncfusion.Blazor.RichTextEditor;
using Microsoft.AspNetCore.Components.Forms;
using Bogus.DataSets;
using Syncfusion.Blazor.DropDowns;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.InPlaceEditor.Internal;
using static System.Net.WebRequestMethods;
using System;
using System.Collections;
using System.Security.Claims;
using BedBrigade.SpeakIt;



namespace BedBrigade.Client.Components
{
    public partial class BlogGrid: ComponentBase
    {

        [Inject] private IContentDataService? _svcContent { get; set; }
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] IWebHostEnvironment? WebhostEnvironment { get; set; }
        [Inject] private ToastService? _toastService { get; set; }
        [Inject] private ILanguageContainerService? _lc { get; set; }
        [Inject] private ITranslateLogic? _translateLogic { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IJSRuntime? JSRuntime { get; set; }

        [Parameter] public string? ContentTypeName { get; set; }

        private const string PageGridMode = "Grid";

        private ClaimsPrincipal? Identity { get; set; }
        protected List<Content>? lstContent { get; set; }

        private BedBrigade.Common.Enums.ContentType CurrentContentType { get; set; }

        public List<BlogData>? lstBlogData { get; set; }
        // Filtered list for Grid
        private List<BlogData> FilteredBlogList = new List<BlogData>();
        public List<Location>? Locations { get; private set; }
                       
        private SfGrid<BlogData>? MyGrid;
      
        private BlogData? CurrentBlog;
               
        private string MainImageUrl { set; get; } = string.Empty;
        private int MaxContentSize = 150;
       
        private string? ModalImageUrl;     
        private bool ShowEditModal = false;
        private bool ShowModal = false;
        private bool IsConfirmationModal = false;
                                   

        protected SfToast? ToastObj { get; set; }
        protected string? ToastTitle { get; set; }
        protected string? ToastContent { get; set; }
        protected int ToastTimeout { get; set; } = 3000;
               
        private int SelectedLocationId = 1;
        
        private string WebRootPath => WebhostEnvironment.WebRootPath;
        private Location userLocation { set; get; } = new Location();

        private bool isNational = false;   
        public string PageMode = "Grid"; // other values: View, Edit

        private bool isEditing = true;

        protected override async Task OnInitializedAsync()
        {
            CurrentContentType = (BedBrigade.Common.Enums.ContentType)Enum.Parse(typeof(BedBrigade.Common.Enums.ContentType), ContentTypeName);

            await LoadUserData();
            await LoadGridData();           
            ApplyFilter();

        }//OnInit

        private async Task LoadUserData()
        {
            var locResult = await _svcLocation.GetAllAsync();
            if (locResult != null && locResult.Success)
            {
                Locations = locResult.Data.ToList();
            }

            Identity = _svcAuth.CurrentUser;
            if (Identity.IsInRole(RoleNames.NationalAdmin))
            {
                isNational = true;
                SelectedLocationId = 1;
            }
            else
            {
                SelectedLocationId = await _svcUser.GetUserLocationId();

                var userLocationResult = await _svcLocation.GetByIdAsync(SelectedLocationId);
                if (userLocationResult.Success && userLocationResult.Data != null)
                {
                    userLocation = userLocationResult.Data;
                    Debug.WriteLine($"User Location Name: {userLocation.Name}");

                }
            }
        }// Load User Data

     

        private async Task LoadGridData()
        {         
                            

            var contentResult = await _svcContent.GetAllAsync();
            if (contentResult != null && contentResult.Success)
            {
                lstContent = contentResult.Data.ToList();
                if (lstContent != null && lstContent.Count > 0)               
                {
                    lstContent = lstContent.Where(c => c.ContentType.ToString() == ContentTypeName).OrderByDescending(c => c.UpdateDate).ToList();
                    lstBlogData = BlogHelper.GetBlogDataList(lstContent, Locations);                                 
                }
                else
                {
                    lstBlogData = new List<BlogData>();
                }
                
            }         

        } // Load Content             

        private string StripHtml(string input, int maxLength)
        {

            if (input != null && input.Length > 0)
            {
                var plainText = Regex.Replace(input, "<.*?>", ""); // Remove HTML tags
                return plainText.Length > maxLength ? plainText.Substring(0, maxLength) + "..." : plainText;
            }
            else { return ""; }

        }// Strip Html

        public void RowUpdating(RowUpdatingEventArgs<BlogData> args)
        {
            //args.Data.Freight = Convert.ToInt32(args.Data.Freight);
        }

        public void ActionBeginHandler(ActionEventArgs<BlogData> args)
        {
            Debug.WriteLine($"Action begin: {args.RequestType}");

            var ModifiedBlogItem = args.Data; // Store the item to delete or save

            switch (args.RequestType)
            {
                case Syncfusion.Blazor.Grids.Action.Add: // Add new blog item

                    // cancel default editing
                    args.Cancel = true;
                    Debug.WriteLine("Add New Blog Item");
                    
                    CurrentBlog = new BlogData();
                    CurrentBlog.ContentId = 0;
                    CurrentBlog.LocationId = userLocation.LocationId;
                    CurrentBlog.LocationName = userLocation.Name;
                    CurrentBlog.LocationRoute = userLocation.Route;
                    CurrentBlog.MainImageUrl = string.Empty;
                    CurrentBlog.OptImagesUrl = new List<string>();
                    CurrentBlog.BlogFolder = BlogHelper.GetBlogLocationFolder(userLocation.Route, userLocation.Name);
                    CurrentBlog.ContentType = CurrentContentType;
                    CurrentBlog.Title = $"Enter New {ContentTypeName} Item Title...";
                    CurrentBlog.ContentHtml = "Enter Blog Content Here...";

                    PageMode = "Edit"; // display edit component
                    OpenCustomModal();
                    break;

                case Syncfusion.Blazor.Grids.Action.BeginEdit:
                    Debug.WriteLine($"Begin Edit Blog Item : {ModifiedBlogItem.ContentId} - {ModifiedBlogItem.Title}");
                    // cancel default editing
                    args.Cancel = true;
                    PageMode="Edit"; // display edit component
                    OpenCustomModal();
                    break;
              
                case Syncfusion.Blazor.Grids.Action.Delete: // delete current blog item
                    Debug.WriteLine("Grid Record will be deleted");
                    _= DeleteBlogAsync(ModifiedBlogItem);
                    break;             

            }// Request Type

        } // Edit Form Actions

        public void ViewBlog(BlogData BlogItem)
        {
            CurrentBlog = BlogItem;
            PageMode = "View";
            OpenCustomModal();
        }


        public void ActionCompleteHandler(ActionEventArgs<BlogData> args)
        {
            Debug.WriteLine("OnActionCompleteHandler Raised");

            Debug.WriteLine("OnActionCompleteHandler RequestType:" + args.RequestType);
        }//ActionCompleteHandler


        public async Task DeleteBlogAsync(BlogData? blogDelete)
        {           
            if(blogDelete == null){ return; }
            Debug.WriteLine($"Request to delete Blog Item: {blogDelete.ContentId} - {blogDelete.Title}");

            if (blogDelete.BlogFolder != null)
            {

                var blogFolder = Path.Combine(WebRootPath, blogDelete.BlogFolder);

                //Debug.WriteLine($"Request to delete Blog folder: {blogFolder}");
                if (Directory.Exists(blogFolder))
                {
                    try
                    {
                        Directory.Delete(blogFolder, true); // Delete the folder and all files
                        Debug.WriteLine($"Deleted Blog folder: {blogFolder}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting Blog folder: {blogFolder} - {ex.Message}");
                    }
                }
                else
                {
                    Debug.WriteLine($"Blog folder not found: {blogFolder}");
                }
            } // Delete Blog Folder

            // Delete Blog  Record
            try
            {// call delete service
                var updateResult = await _svcContent.DeleteAsync(blogDelete.ContentId);
                if (updateResult.Success)
                {
                    Debug.WriteLine($"Blog {blogDelete.ContentId} deleted.");
                    // clear uploaded file list
                   
                    _toastService.Success($"{ContentTypeName} #{blogDelete.ContentId}", "Blog Content deleted");
                    FilteredBlogList.Remove(blogDelete);
                    await MyGrid.CloseEditAsync();
                    await MyGrid.Refresh();
                    StateHasChanged();
                }
                else
                {
                    _toastService.Error("Error", $"Could not delete {ContentTypeName} content");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error on delete: {ex.Message}");
            }

        } // Delete Blog       
              


        public void GetSelectedRecords(RowSelectEventArgs<BlogData> args)
        {
            
            CurrentBlog = args.Data;              
            //Debug.WriteLine($"Selected Blog: {CurrentBlog.Title}");
        }
           
       
        private void ApplyFilter()
        {
            FilteredBlogList = lstBlogData
                .Where(b => b.LocationId == SelectedLocationId)
                .ToList();
                StateHasChanged();
            // Set Current Use Location


        }

        private void OnLocationChange(ChangeEventArgs<int, Location> args)
        {
            // Debug.WriteLine($"Selected Location Id: {args.Value}");

            if (args.Value > 0)
            {
                SelectedLocationId = args.Value;
                userLocation = args.ItemData;
                ApplyFilter();
            }      
            


        } // Location Filter Changed

       
        
        private async Task OnEditSave(BlogData updatedItem)
        {
            // Update record in list
            var index = FilteredBlogList.FindIndex(b => b.ContentId == updatedItem.ContentId);
            if (index >= 0)
            {
                FilteredBlogList[index] = updatedItem;
            }
            // refresh Record in Grid?
            await MyGrid.SetRowDataAsync(updatedItem.ContentId, updatedItem);
            _ = MyGrid.Refresh();
            PageMode = PageGridMode;
            CloseCustomModal();
            StateHasChanged(); // Refresh the grid
        }

        private void OnEditCancel()
        {
            PageMode = PageGridMode;
            CloseCustomModal();
            StateHasChanged();
        }

        private void CloseView(BlogData? closedCard)
        {
            PageMode = PageGridMode;
            CloseCustomModal();
            StateHasChanged();
        }

        private void NavigateDetail(string direction)
        { // Place holder 
        }
        private void CloseCustomModal()
        {
            ShowEditModal = false;          
        }
        private void OpenCustomModal()
        {
            ShowEditModal = true;          
        }
        private void OnBackgroundClick()
        {
            // Prevent closing if editing is active
            if (!isEditing)
            {
                //await CloseCustomModal();
            }
        }

    } // BlogGrid class
}// namespace
