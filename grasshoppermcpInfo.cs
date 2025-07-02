using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace grasshoppermcp
{
    public class grasshoppermcpInfo : GH_AssemblyInfo
    {
        public override string Name => "grasshoppermcp";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("cb2c21b8-63e5-4b02-a4b4-86793f1f60f7");

        //Return a string identifying you or your company.
        public override string AuthorName => "sha_opt";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }
}