using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.SpeakIt;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.JSInterop;
using Syncfusion.Blazor.Kanban.Internal;
using Syncfusion.Blazor.RichTextEditor;
using System.Diagnostics;
using static BedBrigade.Common.Logic.BlogHelper;

namespace BedBrigade.Client.Components
{
    public partial class BlogCards : ComponentBase
    {
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ITranslateLogic _translateLogic { get; set; }

        // constant data
        private readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        // Parameters

        [Parameter] public Location? CurrentLocation { get; set; }  // requested Location     
      
        [Parameter] public string? ContentTypeName { get; set; } // News, Stories

      
        // all variables are retired
        [Parameter] public bool IsPaging { get; set; } = true;
        [Parameter] public int Columns { get; set; } = 3;
        [Parameter] public int Rows { get; set; } = 2;
        [Parameter] public int MaxContentSize { get; set; } = 150;

        [Parameter] public List<BlogData> BlogItemList { get; set; } = new();

        private BlogData? ActiveCard { get; set; } = null;
        private BlogData? CurrentCard { get; set; } = null;

        // Pagination

        private int _currentPage = 0;
        private Dictionary<BlogData, ElementReference> cardRefMap = new();
        private List<ElementReference> cardRefs = new();
        private List<ElementReference> selectedCardRefs = new();

        private int _pageSize => (IsPaging && BlogItemList.Count > (Columns * Rows)) ? Columns * Rows : BlogItemList.Count;
             
        private bool ComputedPaging => IsPaging && BlogItemList.Count > _pageSize;             

        private IEnumerable<BlogData> CurrentPageData => ComputedPaging ? BlogItemList.Skip(_currentPage * _pageSize).Take(_pageSize) : BlogItemList; 

        private int TotalPages => ComputedPaging ? (int)Math.Ceiling((double)BlogItemList.Count / _pageSize) : 1; // work

        private bool IsFirstPage => _currentPage == 0; // work
        private bool IsLastPage => _currentPage == TotalPages - 1; //work

        private bool showDetails = false;

    
        protected override void OnParametersSet()
        {          
            ResetCardReferences();
        } // Param

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Map data to corresponding references
            if (firstRender || showDetails == false)
            {
                cardRefMap.Clear();
                var currentPageList = CurrentPageData.ToList();
                for (int i = 0; i < currentPageList.Count; i++)
                {
                    if (i < cardRefs.Count)
                    {
                        cardRefMap[currentPageList[i]] = cardRefs[i];
                    }
                }

            }
        }//OnAfterRender

        private void ResetCardReferences()
        {
            if (ComputedPaging)
            {
                cardRefs = Enumerable.Repeat(new ElementReference(), _pageSize).ToList();
            }
            else
            {
                cardRefs = Enumerable.Repeat(new ElementReference(), BlogItemList.Count).ToList();
            }
        }

      
        // Pagination methods
        private void PreviousPage()
        {
            if (_currentPage > 0)
            {
                _currentPage--;
            }
        }

        private void NextPage()
        {
            if (_currentPage < TotalPages - 1)
            {
                _currentPage++;
            }
        }

        private void OpenDetail(int intContentId) //BlogData card
        {
            CurrentCard = BlogItemList.FirstOrDefault(b => b.ContentId == intContentId);

            if (CurrentCard != null)
            {
               // Debug.WriteLine($"Opening details for: {CurrentCard.Title}");

                ActiveCard = CurrentCard;
                //ActiveCard = card;
               // Debug.WriteLine("Selected Card: " + ActiveCard.ContentId.ToString());
              //  Debug.WriteLine("CurrentPage: " + _currentPage.ToString());
                showDetails = true;
            }
            _ = ScrollToTop();
        }

        private void CloseDetail(BlogData? closedCard)
        {
            if (closedCard != null)
            {
                ActiveCard = closedCard;
                
                // Debug.WriteLine("Returned Card: " + ActiveCard.ContentId.ToString());

                int activeIndex = BlogItemList.IndexOf(closedCard);

                if (TotalPages > 1)
                {
                    // Calculate the correct page for the ActiveCard
                    _currentPage = (activeIndex / _pageSize);
                    _currentPage = Math.Clamp(_currentPage, 0, TotalPages - 1); // Prevent invalid pages

                    //  Debug.WriteLine("CurrentPage: " + _currentPage.ToString());
                }
            }

            //ActiveCard = null; // Clear ActiveCard after use
            showDetails = false;            
            StateHasChanged();
        }

        private void NavigateDetail(string direction)
        {

           // Debug.WriteLine($"BlogCards - NavigateDetail: {direction}");
          //  Debug.WriteLine($"BlogCards - Current Active Card: {ActiveCard?.Title}");

            if (ActiveCard == null) return;

            try
            {

                int currentIndex = BlogItemList.IndexOf(ActiveCard);

                if (direction == "Next" && currentIndex < BlogItemList.Count - 1)
                {
                    ActiveCard = BlogItemList[currentIndex + 1];
                }
                else if (direction == "Previous" && currentIndex > 0)
                {
                    ActiveCard = BlogItemList[currentIndex - 1];
                }

                // Update the ActiveCard reference
               // Debug.WriteLine($"BlogCards - New Active Card: {ActiveCard?.Title}");

                if (TotalPages > 1)
                {
                    // Sync CurrentPage to the ActiveCard's position
                    _currentPage = (BlogItemList.IndexOf(ActiveCard) / _pageSize) + 1;

                    // Ensure CurrentPage does not exceed TotalPages
                    _currentPage = Math.Clamp(_currentPage, 1, TotalPages);
                }


                StateHasChanged();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BlogCards - Error in NavigateDetail: {ex.Message}");
            }
        }


        public async Task ScrollToTop()
        {
            await JSRuntime.InvokeVoidAsync("scrollToTop");
        }

    } // partial class
    }// namespace
