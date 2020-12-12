using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BExIS.Dlm.Entities.Data;

namespace BExIS.Modules.Vim.UI.Models
{
    public class DQModels
    {
        //public class varVariable
        //{
        //    public string varLabel;
        //    public string varType;
        //    public int varUsage;
        //}

        public Dictionary<string, string> datasetInfo { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string dStrDescription { get; set; }
        public int dStrUsage { get; set; }
        //public List<string[]> variables = new List<string[]>();
        public Controllers.DQController.varVariable varV {get; set;} 
        public List<Controllers.DQController.varVariable> varVariables = new List<Controllers.DQController.varVariable>();
        public List<Controllers.DQController.performer> performers = new List<Controllers.DQController.performer>();        
        //public List<string> performers { get; set; }        
    }
}