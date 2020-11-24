using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BExIS.Dlm.Entities.Data;

namespace BExIS.Modules.Vim.UI.Models
{
    public class DQModels
    {
        public class variable
        {
            public string varLabel { get; set; }
            public string varType { get; set; }
            public int varUsage { get; set; }
        }

        public Dictionary<string, string> datasetInfo { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string dStrDescription { get; set; }
        public int dStrUsage { get; set; }
        //public List<string[]> variables = new List<string[]>();
        public List<variable> variables { get; set; }
        public List<string> performers { get; set; }        
    }
}