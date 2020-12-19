using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BExIS.Dlm.Entities.Data;

namespace BExIS.Modules.Vim.UI.Models
{
    public class DQModels
    {

        public Dictionary<string, string> datasetInfo { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public Dictionary<string, double> datasetSize { get; set; }
        public string dStrDescription { get; set; }
        public int dStrUsage { get; set; }
        //public Controllers.DQController.varVariable varV {get; set;} 
        public List<Controllers.DQController.varVariable> varVariables = new List<Controllers.DQController.varVariable>();
        public List<Controllers.DQController.performer> performers = new List<Controllers.DQController.performer>();             
    }
}