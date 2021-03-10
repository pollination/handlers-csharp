using Share.Entity;
using System;
using System.IO;

// project name has to be the format of NameSpace.Handlers. 
// For example: if the name space is "HoneybeeSchema", then project name should be HoneybeeSchema.Handlers.
namespace Pollination
{
    public static class Handlers
    {
        /// <summary>
        /// Translate Honeybee Model to HBJson and return the saved file path
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string RhinoHBModelToJSON(object param)
        {
            var model = ModelEntityTable.Instance.CurrentModelEntity;
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
        /// Translate Honeybee Model to HBJson and return the saved file path
        /// </summary>
        /// <param name="model"></param>
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
    }

    
}
