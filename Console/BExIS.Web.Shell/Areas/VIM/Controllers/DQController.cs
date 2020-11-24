using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Services.Data;
using BExIS.Modules.Vim.UI.Models;
using BExIS.Modules.Vim.UI.Helper;
using BExIS.Security.Services.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using BExIS.Dlm.Services.DataStructure;
using BExIS.Dlm.Entities.DataStructure;

namespace BExIS.Modules.Vim.UI.Controllers
{
    public class DQController : Controller
    {
        // GET: DQ
        public ActionResult ShowDQ(long datasetID, long versionId)
        {
            DQModels dqModel = new DQModels();
            Dictionary<string, string> datasetInfo = new Dictionary<string, string>();
            List<string> performers = new List<string>();
            
        //--------

        DatasetManager dm = new DatasetManager();
            DataStructureManager dsm = new DataStructureManager();
            EntityPermissionManager entityPermissionManager = new EntityPermissionManager();

            // var entityPermissionManager = new EntityPermissionManager();
            try
            {
                if (dm.IsDatasetCheckedIn(datasetID))
                {
                    // get all dataset versions
                    var dsvs = dm.GetDatasetVersions(datasetID);                    
                    foreach (var d in dsvs){
                        if (d.Id <= versionId)
                        {
                            string performer = d.ModificationInfo.Performer;
                            if (performer != null && !performers.Contains(performer))
                            {
                                performers.Add(performer);
                            }
                        }
                    }
                    dqModel.performers = performers;
                    //var performers = dsvs.Select(v => v.CreationInfo.Performer).ToList();

                    // get datasetversion
                    var dsv = dm.GetDatasetVersion(versionId);
                    string title = dsv.Title;
                    dqModel.title = title;
                    string description = dsv.Description;
                    dqModel.description = description;

                    var metadata = dsv.Metadata;

                    string type = "file";
                    //if (dsv.Dataset.DataStructure.Self is StructuredDataStructure)
                    //{
                    //    type = "tabular";
                    //}

                    string dStrDescription = dsv.Dataset.DataStructure.Description;
                    dqModel.dStrDescription = dStrDescription;

                    DataStructure ds = dsm.AllTypesDataStructureRepo.Get(dsv.Dataset.DataStructure.Id);
                    if (ds.Self.GetType() == typeof(StructuredDataStructure))
                    {
                        type = "tabular";
                        StructuredDataStructure sds = dsm.StructuredDataStructureRepo.Get(dsv.Dataset.DataStructure.Id);
                        var dataStrUsage = sds.Datasets;
                        dqModel.dStrUsage = dataStrUsage.Count();
                        
                        var variables = sds.Variables;
                        
                        //vs = null;
                        //int i = 1;
                        foreach(var variable in variables)
                        {
                            string varLabel = variable.Label;                            
                            var varType = variable.GetType();
                            int varUsage = variable.DataAttribute.UsagesAsVariable.Count();
                            //dqModel.variables.Add(Var);
                        }

                        //List<string> variables = 
                        string fake = "ss";
                    }
                    else
                    {

                    }
                    dqModel.type = type;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                dm.Dispose();
                dsm.Dispose();
                entityPermissionManager.Dispose();
            }
            
            //var datasetversions = dm.GetDatasetVersions(datasetID);
            //var n = datasetversions.Select(v => v.CreationInfo.Performer).ToList();

            dqModel.datasetInfo = datasetInfo;
            dqModel.performers = performers;
            return PartialView(dqModel);
        }

        private int textRatio(String text, int totalLength)
        {
            int ratio;
            try
            {
                ratio = (text.Length * 100) / totalLength;
            }
            catch { ratio = 99999; }

            //return (ratio.toFixed(0));
            return (ratio);
        }
    }
}