using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace SpatialPrinting
{
    public class SpatialPrintingInfo : GH_AssemblyInfo
    {
        public override string Name => "SpatialPrinting";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("6028c5bf-50b0-4ee6-addf-84bfc779a8f2");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}