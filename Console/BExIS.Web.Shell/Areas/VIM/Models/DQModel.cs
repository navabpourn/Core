﻿using System;
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
        //public List<int> ColumnRowNumbers = new List<int>(); //column . row
        public int columnNumber = 0;
        public int rowNumber = 0;
        public string dStrDescription { get; set; }
        public metadataComplition metadataComplition = new metadataComplition();
        public List<performer> performers = new List<performer>(); 
        public List<varVariable> varVariables = new List<varVariable>();
        public List<datasetInformation> datasetsInformation = new List<datasetInformation>();
    }
}