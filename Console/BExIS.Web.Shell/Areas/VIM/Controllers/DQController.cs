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

namespace BExIS.Modules.Vim.UI.Controllers
{
    public class DQController : Controller
    {
        // GET: DQ
        public ActionResult Index()
        {
            DQModels dqModel = new DQModels();
            Dictionary<string, string> datasetInfo = new Dictionary<string, string>();

            string title;
            string description;


            long myId = 6;

            //--------

            DatasetManager dm = new DatasetManager();
            var entityPermissionManager = new EntityPermissionManager();

            List<Dataset> datasets = dm.DatasetRepo.Query().OrderBy(p => p.Id).ToList();
            List<long> datasetIds = datasets.Select(p => p.Id).ToList();

            foreach (var id in datasetIds)
            {
                if (id == myId)
                {
                    Dataset dataset = dm.GetDataset(id);

                    List<DatasetVersion> versions = dm.DatasetVersionRepo.Query(p => p.Dataset.Id == id).OrderBy(p => p.Id).ToList();

                    List<long> datasetVersionId = versions.Select(p => p.Id).ToList();

                    title = dataset.Versions.Last().Title;
                    description = dataset.Versions.Last().Description;
                    datasetInfo.Add("title", title);
                    datasetInfo.Add("description", description);
                }

            }
            dqModel.datasetInfo = datasetInfo;

            return View(dqModel);
        }
    }
}