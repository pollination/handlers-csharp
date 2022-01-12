using System;

namespace Pollination
{
    public class RhinoHandlerChecker: PollinationSDK.HandlerChecker
    {
        private static RhinoHandlerChecker _instance;

        public static RhinoHandlerChecker Instance
        {
            get
            {
                _instance = _instance ?? new RhinoHandlerChecker();
                return _instance;
            }
            set { _instance = value; }
        }

        private RhinoHandlerChecker()
        {
        }

        public override object InvokeCSharpFunction(Type handler, string fullMethodName, object param)
        {
            return base.InvokeCSharpFunction(handler, fullMethodName, param);
        }

        /// <summary>
        /// Specific for Rhino/Grasshopper
        /// </summary>
        /// <param name="module"></param>
        /// <param name="function"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public override object InvokePythonFunction(string module, string function, object param)
        {
            if (param == null) return null;

            var pyRun = Rhino.Runtime.PythonScript.Create();
            pyRun.SetVariable("param", param);
            string pyScript = $"import {module} as h;result=h.{function}(param)";
            pyRun.ExecuteScript(pyScript);

            var PyObject = pyRun.GetVariable("result");

            return PyObject;
        }

    }
}
