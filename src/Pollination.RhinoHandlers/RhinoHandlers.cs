using Core;
using System;
using System.Collections.Generic;
using System.IO;
using PollinationSDK;
using PollinationSDK.Wrapper;
using System.Linq;

// project name has to be the format of NameSpace.Handlers. 
// For example: if the name space is "HoneybeeSchema", then project name should be HoneybeeSchema.Handlers.
namespace Pollination
{
    public static class RhinoHandlers
    {
        /// <summary>
        /// Translate in-model SimulationParameter to json and return the saved file path
        /// </summary>
        /// <returns>Json file path</returns>
        public static string RhinoSimulationParameterToJSON(object param)
        {
            var model = Core.ModelEntity.CurrentModel;
            var hbObj = model.GetSimulationParameter();

            if (hbObj == null) throw new ArgumentNullException("Input model");
            var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".hbjson");
           
            File.WriteAllText(temp, hbObj.ToJson());

            if (!File.Exists(temp)) throw new ArgumentException($"Failed to save the SimulationParameter to {temp}");
            return temp;
        }

        /// <summary>
        /// Translate Honeybee Model to HBJson and return the saved file path
        /// </summary>
        /// <returns>File path</returns>
        public static string RhinoHBModelToJSON(object param)
        {
            var model = Core.ModelEntity.CurrentModel;
            var hbObj = model.GetHBModel();

            if (hbObj == null) throw new ArgumentNullException("Input model");
            var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".hbjson");
            using (var outputFile = new StreamWriter(temp))
            {
                var json = hbObj.ToJson();
                outputFile.Write(json);
            }

            if (!File.Exists(temp)) throw new ArgumentException($"Failed to save the model to {temp}");
            return temp;
        }

        /// <summary>
        /// Translate Honeybee SimulationParameter to Json and return the saved file path
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string HBSimulationParameterToJSON(object param)
        {
            if (param == null) throw new ArgumentNullException("Input model: null is not valid");

            if (param is string s)
                return s;

            var sP = param as HoneybeeSchema.SimulationParameter;
            if (sP == null) throw new ArgumentNullException("Input is not a SimulationParameter object");


            var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
            File.WriteAllText(temp, sP.ToJson());

            if (!File.Exists(temp)) throw new ArgumentException($"Failed to save the SimulationParameter to {temp}");
            return temp;
        }

        /// <summary>
        /// Translate Honeybee Model to HBJson and return the saved file path
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static string HBModelToJSON(object param)
        {
            if (param == null) throw new ArgumentNullException("Input model: null is not valid");

            if (param is string s)
                return s;

            var model = param as HoneybeeSchema.Model;
            if (model == null) throw new ArgumentNullException("Input is not a Honeybee Model");


            var temp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".hbjson");
            using (var outputFile = new StreamWriter(temp))
            {
                var json = model.ToJson();
                outputFile.Write(json);
            }

            if (!File.Exists(temp)) throw new ArgumentException($"Failed to save the model to {temp}");
            return temp;
        }


        public static async void LoadMeshBasedResultsToRhino(params object[] results)
        {
            var modelAsset = results.OfType<RunInputAsset>().FirstOrDefault(_ => _.PreloadedPath is HoneybeeSchema.Model);
            var resultAsset = results.OfType<RunOutputAsset>().FirstOrDefault(); // two rooms result


            var model = modelAsset.PreloadedPath as HoneybeeSchema.Model;
            var res = resultAsset.PreloadedPath as IList<object>;

            // load to Rhino
            var grids = model.Properties.Radiance.SensorGrids;
            var grid_res = grids.Zip(res, (grid, values) => new { grid, values });

            var doc = Rhino.RhinoDoc.ActiveDoc;

            var mergedMesh = new Rhino.Geometry.Mesh();
            var resultNumbers = new List<double>();
            foreach (var grid in grid_res)
            {
                var values = grid.values as IList<object>;
                // try to convert int or double, etc
                var numbers = values.Select(_ => System.Convert.ToDouble(_)).ToList();
                var mesh = grid.grid.Mesh.ToRHMesh();

                mergedMesh.Append(mesh);
                resultNumbers.AddRange(numbers);
            }

            // get RunID
            var runID = modelAsset.RunSource?.Split('/')?.LastOrDefault();
            if (string.IsNullOrEmpty(runID))
                runID = Guid.NewGuid().ToString();

            runID = runID.Substring(0, 5);
            var min = resultNumbers.Min();
            var max = resultNumbers.Max();
            var legend = new Honeybee.UI.LegendParameter(min, max, 10);
            var re = new Core.Objects.AnalyticalMeshObject(mergedMesh, resultNumbers, legend);
            re.AddToRhino(doc, layerName: $"RESULT-{runID}", resultAsset.Name);
            doc.Views.Redraw();
        }

        //public static void OpenFileOrFolderResult(params object[] results)
        //{
        //    if (results == null || !results.Any()) return;

        //    var result = results.FirstOrDefault();
        //    if (!result.IsSaved())
        //        return;

        //    var path = result.LocalPath;
           
        //    try
        //    {
        //        if (System.IO.Path.HasExtension(path)) // file type
        //            Share.Utility.OpenFile(path);
        //        else //directory
        //            Share.Utility.OpenFolder(path);

        //    }
        //    catch (Exception e)
        //    {
        //        Eto.Forms.MessageBox.Show(e.Message);
        //        //throw;
        //    }
        //}


        //public static async void LoadMeshBasedHourlyResultsToRhino(List<RunAssetBase> results)
        //{
        //    var modelAsset = results.First(_ => _ is RunInputAsset);
        //    var resultAsset = results.First(_ => _ is RunOutputAsset);


        //    var model = modelAsset.PreloadedPath as HoneybeeSchema.Model;
        //    var res = resultAsset.PreloadedPath as IList<object>;

        //    var hourRes = res[4000] as IList<object>;

        //    // load to Rhino
        //    var grids = model.Properties.Radiance.SensorGrids;
        //    var grid_res = grids.Zip(hourRes, (grid, values) => new { grid, values });

        //    var doc = Rhino.RhinoDoc.ActiveDoc;

        //    var mergedMesh = new Rhino.Geometry.Mesh();
        //    var resultNumbers = new List<double>();
        //    foreach (var grid in grid_res)
        //    {
        //        var values = grid.values as IList<object>;
        //        var numbers = values.OfType<double>().ToList();
        //        var mesh = grid.grid.Mesh.ToRHMesh();

        //        mergedMesh.Append(mesh);
        //        resultNumbers.AddRange(numbers);
        //    }

        //    // get RunID
        //    var runID = modelAsset.CloudRunSource?.Split('/')?.LastOrDefault();
        //    if (string.IsNullOrEmpty(runID))
        //        runID = Guid.NewGuid().ToString();

        //    runID = runID.Substring(0, 5);

        //    var re = new Share.Objects.AnalyticalMeshObject(mergedMesh, resultNumbers);
        //    re.AddToRhino(doc, layerName: $"RESULT-{runID}");
        //    doc.Views.Redraw();
        //}

    }


}
