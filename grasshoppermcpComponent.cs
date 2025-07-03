using System;
using System.IO;
using System.Drawing;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using System.IO.Pipelines;

namespace grasshoppermcp
{
    public class grasshoppermcpComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        private static volatile bool isRunning = false;
        private static int grasshopperPort = 8080;
        private static McpServer _server;
        private readonly object _serverLock = new object();
        public grasshoppermcpComponent()
          : base("grasshoppermcpComponent", "ghmcp",
            "ModelContextProtocol Server Component",
            "MCP", "Server")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            // You can often supply default values when creating parameters.
            // All parameters must have the correct access type. If you want 
            // to import lists or trees of values, modify the ParamAccess flag.
            pManager.AddBooleanParameter("Enabled", "E", "Enable or disable the MCP server", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Port", "P", "Port to listen on", GH_ParamAccess.item, grasshopperPort);

            // If you want to change properties of certain parameters, 
            // you can use the pManager instance to access them by index:
            //pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // Output parameters do not have default values, but they too must have the correct access type.
            pManager.AddTextParameter("Status", "S", "Server status", GH_ParamAccess.item);

            // Sometimes you want to hide a specific parameter from the Rhino preview.
            // You can use the HideParameter() method as a quick way:
            //pManager.HideParameter(0);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool enabled = false;
            int port = grasshopperPort;


            if (!DA.GetData(0, ref enabled)) return;
            if (!DA.GetData(1, ref port)) return;

            grasshopperPort = port;
            var mcpAddress = $"http://localhost:{grasshopperPort}/";


            //HttpListener _httpListener = new();
            //_httpListener.Prefixes.Add(mcpAddress);
            //_httpListener.Start();
            //Pipe clientToServerPipe = new();
            //Pipe serverToClientPipe = new();

            //var builder = new ServiceCollection()
            //    .AddMcpServer()
            //    .WithStreamServerTransport(
            //        clientToServerPipe.Reader.AsStream(), 
            //        serverToClientPipe.Writer.AsStream()); 

            //builder.WithToolsFromAssembly();
            // if (enabled && !isRunning)
            // {
            //     isRunning = true;
            //     DA.SetData(0, $"Running on {mcpAddress}");
            //     _server.Start(mcpAddress);
            // }
            // else if (!enabled && isRunning)
            // {
            //     isRunning = false;
            //     DA.SetData(0, "Stopped");
            //     _server.Stop();
            // }
            // else if (enabled && isRunning)
            // {
            //     DA.SetData(0, $"Running on {mcpAddress}");
            // }
            // else
            // {
            //     DA.SetData(0, "Stopped");
            // }
            lock (_serverLock)
            {
                // _isrunning 应该是你组件的一个成员变量
                bool serverIsActuallyRunning = (_server != null && _server.IsListening);

                if (enabled && !serverIsActuallyRunning)
                {
                    // 需要启动
                    try
                    {
                        // 如果 _server 是 null，需要先创建实例
                        if (_server == null) _server = new McpServer();
                        _server.Start(mcpAddress);
                    }
                    catch (Exception ex)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
                    }
                }
                else if (!enabled && serverIsActuallyRunning)
                {
                    // 需要停止
                    _server?.Stop(); // 使用 ?. 来避免 _server 本身为 null 的情况
                }

                // 更新组件状态信息
                this.Message = (_server != null && _server.IsListening) ? "Running" : "Stopped";
            }


        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("607c17a0-4ae0-4cd7-9123-66a190a4863b");
    }
}
