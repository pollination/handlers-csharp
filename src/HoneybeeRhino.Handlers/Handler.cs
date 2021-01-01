using HoneybeeRhino.Entity;
using System;
using System.IO;

namespace HoneybeeRhino
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
    }
}
