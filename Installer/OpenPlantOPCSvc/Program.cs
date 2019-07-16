using OpenPlantOPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace OpenPlant
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }

    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        OPCBackEnd oPCBackEnd;
        protected override void OnStart(string[] args)
        {       
            new Thread(() => 
            {
                Global.Product = new ProductDetails("OPOPC", typeof(OPCBackEndConfig), "Open-Plant\\OPOPC");
                bool SendResult = (new EmbeddedResourceFileSender()).SendFiles("OpenPlantOPC", "SendToProgramDataDirectory", Global.Product.ProgramDataDirectory, "*", false, Assembly.GetExecutingAssembly());
                if (!SendResult) throw new CustomException("FATAL ERROR: Unable to access Directory '" + Global.Product.ProgramDataDirectory);
                oPCBackEnd = new OPCBackEnd();
            }).Start();
        }

        protected override void OnStop()
        {
            oPCBackEnd.Stop();
        }
    }
}
