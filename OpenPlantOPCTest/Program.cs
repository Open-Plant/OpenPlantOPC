using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenPlant;

namespace OpenPlantOPC
{
    class Program
    {
        static void Main(string[] args)
        {
            Global.Product = new ProductDetails("OpenPlantOPC", typeof(OPCBackEndConfig), "Open-Plant\\OPOPC");
            bool SendResult = (new EmbeddedResourceFileSender()).SendFiles("OpenPlantOPC", "SendToProgramDataDirectory", Global.Product.ProgramDataDirectory, "*", false, Assembly.GetExecutingAssembly());
            if (!SendResult) throw new CustomException("FATAL ERROR: Unable to access Directory '" + Global.Product.ProgramDataDirectory);            
            OPCBackEnd oPCBackEnd = new OPCBackEnd();            
        }
    }
}
