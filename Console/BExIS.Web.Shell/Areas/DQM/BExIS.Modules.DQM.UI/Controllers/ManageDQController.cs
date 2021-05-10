using BExIS.Dlm.Entities.Data;
using BExIS.Dlm.Services.Data;
using BExIS.Modules.DQM.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Vaiona.Web.Extensions;
using Vaiona.Web.Mvc.Models;

namespace BExIS.Modules.DQM.UI.Controllers
{
    public class ManageDQController : Controller
    {
        public class dataset
        {
            public long Id;
            public string title;
            public List<long> versionIds;
        }

        // GET: DQManager
        public ActionResult Index()
        {
            DatasetManager dm = new DatasetManager(); //dataset manager
            ManageDQ manageModel = new ManageDQ();
            //DatasetVersion dsv = new DatasetVersion(); //dataset version manager
            List<long> datasetIds = dm.GetDatasetLatestIds(); //get latest 
            //List<List<long>> matrixId = new List<List<long>>();
            List<dataset> datasets = new List<dataset>();

            foreach (long Id in datasetIds) //for each dataset
            {
                dataset ds = new dataset();
                ds.Id = Id;
                ds.title = dm.GetDatasetLatestVersion(Id).Title;
                List<DatasetVersion> datasetVersions = dm.GetDatasetVersions(Id);
                List<long> versionIds = new List<long>();
                for (int i = 0; i < datasetVersions.Count; ++i)
                {
                    long versionId = datasetVersions[i].Id;
                    versionIds.Add(versionId);
                }
                //matrixId.Add(versions);
                ds.versionIds = versionIds;
                datasets.Add(ds);
            }

            //manageModel.matrixId = matrixId;
            manageModel.datasets = datasets;
            return View(manageModel);
        }

        [HttpPost] // can be HttpGet
        public ActionResult Test(string id)
        {
            bool isValid = true;
            var obj = new
            {
                valid = isValid
            };
            return Json(obj);
        }
    }
}