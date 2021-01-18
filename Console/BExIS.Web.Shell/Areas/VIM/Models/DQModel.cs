using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BExIS.Dlm.Entities.Data;
using static BExIS.Modules.Vim.UI.Controllers.DQController;

namespace BExIS.Modules.Vim.UI.Models
{
    public class DQModels
    {
        public datasetDescriptionLength datasetDescriptionLength = new datasetDescriptionLength();
        public dataStrDescriptionLength dataStrDescriptionLength = new dataStrDescriptionLength();
        public datasetTotalSize datasetTotalSize = new datasetTotalSize();
        public dataStrUsage dataStrUsage = new dataStrUsage();
        public performersActivity performersActivity = new performersActivity();
        public datasetColNumber datasetColNumber = new datasetColNumber();
        public datasetRowNumber datasetRowNumber = new datasetRowNumber();
        public datasetFileNumber datasetFileNumber = new datasetFileNumber();
        public List<fileInformation> filesInformation = new List<fileInformation>();
        public string type { get; set; }
        public int isPublic { get; set; } //1:true; 0:false;
        //public List<int> ColumnRowNumbers = new List<int>(); //column . row
        public int columnNumber { get; set; }
        public int rowNumber { get; set; }
        public int fileNumber { get; set; }
        public int userNumber { get; set; }
        public string dStrDescription { get; set; }
        public metadataComplition metadataComplition = new metadataComplition();
        public List<performer> performers = new List<performer>(); 
        public List<varVariable> varVariables = new List<varVariable>();
        public List<datasetInformation> datasetsInformation = new List<datasetInformation>();
    }
}