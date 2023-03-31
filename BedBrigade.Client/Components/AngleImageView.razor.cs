using BedBrigade.Client.Pages.Administration.Manage;
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.IO;

namespace BedBrigade.Client.Components
{
    public partial class AngleImageView
    {

        [Parameter] public string Caption { get; set; } = "Bed Brigade";
        [Parameter] public string Path { get; set; } = "National";

        string FileName { get; set; } = "NoImageFound.jpg";
        string LeftFileName { get; set; }
        string MiddleFileName { get; set; }
        string RightFileName { get; set; }
        List<string> LeftFileNames { get; set; }
        List<string> MiddleFileNames { get; set; }
        List<string> RightFileNames { get; set; }

        protected override Task OnParametersSetAsync()
        {
            const string startPath = "wwwroot/";
            if(!PathExist(Path))
            {
                Path = "National/pages/Error/";
                Caption = "Image Files Do Not Exist";
            }
            List<string> LeftFileNames = GetLeftImages(Path);
            List<string> MiddleFileNames = GetMiddleImages(Path);
            List<string> RightFileNames = GetRightImages(Path);
            LeftFileName = ComputeImageToDisplay(LeftFileNames);
            MiddleFileName = ComputeImageToDisplay(MiddleFileNames);
            RightFileName = ComputeImageToDisplay(RightFileNames);

            return base.OnParametersSetAsync();
        }

        private string ComputeImageToDisplay(List<string> FileNames)
        {         
            var unit = DateTime.Now.Hour * 2 + (DateTime.Now.Minute > 30 ? 1 : 0);
            var imageIndex = unit % FileNames.Count;
            return FileNames[imageIndex].Replace("wwwroot/","");           
        }

        private List<string> GetLeftImages(string path)
        {
            var fileNames = Directory.GetFiles($"wwwroot/media/{path}/Left").ToList();
            return fileNames;
        }

        private List<string> GetMiddleImages(string path)
        {
            var fileNames = Directory.GetFiles($"wwwroot/media/{path}/Middle").ToList();
            return fileNames;
        }

        private List<string> GetRightImages(string path)
        {
            var fileNames = Directory.GetFiles($"wwwroot/media/{path}/Right").ToList();
            return fileNames;
        }

        private bool PathExist(string path)
        {
           if(Directory.Exists($"wwwroot/media/{path}/Left") && Directory.Exists($"wwwroot/media/{path}/Middle") && Directory.Exists($"wwwroot/media/{path}/Right"))
            {
                return true;

            }
            return false;
        }
    }
}
