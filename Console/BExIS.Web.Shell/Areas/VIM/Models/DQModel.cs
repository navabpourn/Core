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
        public string type { get; set; }
        public List<int> ColumnRowNumbers = new List<int>(); //column . row
        public string dStrDescription { get; set; }
        public Controllers.DQController.metadataComplition metadataComplition = new Controllers.DQController.metadataComplition();
        public List<Controllers.DQController.performer> performers = new List<Controllers.DQController.performer>(); 
        public List<Controllers.DQController.varVariable> varVariables = new List<Controllers.DQController.varVariable>();        
    }
}