using System.Collections.Generic;
using PollinationSDK;
using PollinationSDK.Wrapper;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System;

namespace Pollination
{
    public static class HandlerHelper
    {

        public static async Task LoadResult(this Core.Runner.JobResultPackage resultPackage, RunInfo runInfo, RunOutputAsset outputAsset, Action<string> reportMessage = default)
        {
            reportMessage("Loading.");

            // get job
            var res = resultPackage;

            if (resultPackage.IsCloudJob)
            {
                var projApi = new PollinationSDK.Api.ProjectsApi();
                var proj = await projApi.GetProjectAsync(res.CloudProjectOwner, res.CloudProjectName);
                var job = new ScheduledJobInfo(proj, res.JobID);
                reportMessage("Loading..");

                if (!IsJobFinished(job))
                    throw new ArgumentException($"This job status is [{job.CloudJob.Status.Status}], please check back later!");

                // get run assets
                runInfo = runInfo ?? job.GetRunInfo(0);
                reportMessage("Loading...");
            }
            else
            {
                var jobFolder = resultPackage.SavedLocalPath;
                if (!System.IO.Directory.Exists(jobFolder))
                    throw new ArgumentException($"Failed to find the simulation folder: {jobFolder}");

                //Local run
                runInfo = runInfo ?? RunInfo.LoadFromLocalFolder(jobFolder);
            }
            
            var assets = new List<RunAssetBase>() { outputAsset };

            // check if this output is model-based
            var handlers = outputAsset.Handlers;
            if (handlers != null && outputAsset.IsLinkedAsset)
            {
                // add model input to asset list
                var inputAsset = runInfo.GetInputAssets().First(_ => _.Name == "model");
                assets.Add(inputAsset);
            }

            var results = new List<RunAssetBase>();
            if (resultPackage.IsCloudJob)
            {
                // check if needs to download
                var folder = resultPackage.SavedLocalPath;

                // downloaded 
                results = await runInfo.DownloadRunAssetsAsync(assets, folder, reportMessage, useCached: true);
            }
            else
            {
                //Local run
                results = runInfo.LoadLocalRunAssets(assets);
            }
  

            // preprocess the downloaded files
            reportMessage("Pre-processing.");
            var preloadedResults = PreloadLinkedOutput(results);


            // Load to Rhino 
            var outputToLoad = preloadedResults.OfType<RunOutputAsset>().First();
          
            if (handlers != null && handlers.Any())
            {
                var outputAssetHandler = handlers.LastOrDefault();
                reportMessage("Loading results");

                var name = outputAssetHandler.Function;
                var actionMethod = typeof(RhinoHandlers).GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                var paramArg = preloadedResults.ToArray<object>();
                if (handlers.Count == 1) // only 
                    paramArg = new object[] { preloadedResults.OfType<RunOutputAsset>().FirstOrDefault() };
                actionMethod?.Invoke(null, new object[] { paramArg });
                //RhinoHandlers.LoadMeshBasedResultsToRhino(paramArg);
                reportMessage("Loaded");
            }
            else
            {
                // open downloaded folder
                Core.Utility.OpenFileOrFolderPath(outputToLoad.LocalPath);
                reportMessage($"Opened: {outputToLoad.LocalPath}");
            }
           
        }


        //private static List<RunAssetBase> DownloadCloudResult(RunInfo runInfo, List<RunAssetBase> assets , string saveFolder, bool useCache)
        //{
        //    var modelInputName = "model";
        //    var resultOutputName = "results";

        //    //var runInfo = runInfo.GetRunInfo(0); // get the first run for now
        //    var inputAsset = runInfo.GetInputAssets().First(_ => _.Name == modelInputName);
        //    var outputAsset = runInfo.GetOutputAssets("rhino").First(_ => _.Name == resultOutputName);

        //    var assets = new List<RunAssetBase>();
        //    assets.Add(inputAsset);
        //    assets.Add(outputAsset);

        //    var task = runInfo.DownloadRunAssetsAsync(assets, saveFolder, useCached: useCache);
        //    Task.Run(async () => await task);
        //    var results = task.Result;
        //    return results;
        //}

        private static List<RunAssetBase> PreloadLinkedOutput(List<RunAssetBase> rawResults)
        {
            var checkedAssets = new List<RunAssetBase>();
            foreach (var item in rawResults)
            {

                var dup = item.Duplicate();
                if (dup is RunOutputAsset outputAsset)
                {
                    var handlers = outputAsset.Handlers;
                    if (!outputAsset.IsLinkedAsset || handlers == null || handlers.Count == 1) // no need to preload, as there is only one handler available
                    {
                        checkedAssets.Add(dup);
                        continue; 
                    }
                  
                    // preload result
                    var path = outputAsset.LocalPath;
                    outputAsset.PreloadedPath = outputAsset.PreloadLinkedOutputWithHandler(path, RhinoHandlerChecker.Instance);

                }
                else
                {
                    // inputs

                    // Load input model
                    if (item.LocalPath.ToLower().EndsWith(".hbjson"))
                    {
                        var json = System.IO.File.ReadAllText(item.LocalPath);
                        dup.PreloadedPath = HoneybeeSchema.Model.FromJson(json);
                    }
                
                }

                checkedAssets.Add(dup);
            
            }

            return checkedAssets;
        }


        private static bool IsJobFinished(ScheduledJobInfo jobInfo)
        {
            var api = new PollinationSDK.Api.JobsApi();
            var proj = jobInfo.Project;
            var job = api.GetJob(proj.Owner.Name, proj.Name, jobInfo.JobID);

            return job.Status.FinishedAt > job.Status.StartedAt;
        }

        //public static List<RunAssetBase> DownloadCloudAssets(this Share.Runner.ResultPackage resultPackage, ScheduledJobInfo jobInfo, string saveFolder, bool useCache)
        //{
        //    if (string.IsNullOrEmpty(resultPackage.DownloadAction))
        //        throw new ArgumentNullException("No download action was found");

        //    var name = resultPackage.DownloadAction;
        //    var actionMethod = typeof(Handlers).GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        //    var downloaded = actionMethod?.Invoke(null, new object[] { jobInfo, saveFolder, useCache });
        //    return downloaded as List<RunAssetBase>;
        //}


        //public static void GetLoadAction(this Share.Runner.ResultPackage resultPackage, List<RunAssetBase> results)
        //{
        //    if (string.IsNullOrEmpty(resultPackage.LoadAction))
        //        throw new ArgumentNullException("No load action was found");

        //    var name = resultPackage.LoadAction;
        //    var actionMethod = typeof(RhinoHandlers).GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        //    actionMethod?.Invoke(null, new object[] { results });
        //}

        //private static ScheduledJobInfo GetJob(this Share.Runner.ResultPackage resultPackage)
        //{
        //    var projApi = new PollinationSDK.Api.ProjectsApi();
        //    var proj = projApi.GetProject(resultPackage.CloudProjectOwner, resultPackage.CloudProjectName);

        //    var job = new ScheduledJobInfo(proj, resultPackage.JobID);
        //    return job;
        //}



    }


}
