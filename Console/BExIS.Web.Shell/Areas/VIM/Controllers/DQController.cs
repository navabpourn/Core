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
            //--------

            DatasetManager dm = new DatasetManager();
            DataStructureManager dsm = new DataStructureManager();
            EntityPermissionManager entityPermissionManager = new EntityPermissionManager();

            // var entityPermissionManager = new EntityPermissionManager();
            try
            {
                if (dm.IsDatasetCheckedIn(datasetID))
                {
                    // get latest or other datasetversion
                    var dsv = dm.GetDatasetVersion(versionId);
                    string title = dsv.Title;
                    string description = dsv.Description;
                    datasetInfo.Add("title", title);
                    datasetInfo.Add("description", description);
                    string descRatio = textRatio(description, 255).ToString();
                    datasetInfo.Add("descRatio", descRatio);

                    string type = "file";
                    if (dsv.Dataset.DataStructure.Self is StructuredDataStructure)
                    {
                        type = "tabular";
                    }
                    datasetInfo.Add("type", type);

                    if (type == "file")
                    {
                        string dataStr = "ss";
                        datasetInfo.Add("dataStr", dataStr);
                    }

                    if (type == "tabular")
                    {
                        string dataStr = "ss";
                        datasetInfo.Add("dataStr", dataStr);
                    }

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